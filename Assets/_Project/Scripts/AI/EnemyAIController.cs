using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Buildings;
using TinyTactics.Combat;
using TinyTactics.Core;
using TinyTactics.Economy;
using TinyTactics.Units;

namespace TinyTactics.AI
{
    public enum AIState
    {
        EconomyFocus,
        MilitaryProduction,
        Attacking
    }

    /// <summary>
    /// Bộ điều khiển AI cho phe Red (PvE) theo mô hình Finite State Machine (FSM).
    /// Chạy định kỳ mỗi 1.0s để tự vận hành kinh tế, huấn luyện quân đội và tổ chức đợt tấn công Castle Blue.
    /// </summary>
    public class EnemyAIController : MonoBehaviour
    {
        [Header("Faction & Core References")]
        [SerializeField] private Faction _aiFaction = Faction.Red;
        [SerializeField] private CastleBuilding _enemyCastle;
        [SerializeField] private CastleBuilding _playerCastle;
        [SerializeField] private List<BarracksBuilding> _enemyBarracks = new List<BarracksBuilding>();

        [Tooltip("Điểm tập kết quân của AI trước khi xuất kích")]
        [SerializeField] private Transform _rallyPoint;

        [Header("Unit Data")]
        [SerializeField] private UnitDataSO _pawnData;
        [SerializeField] private UnitDataSO _warriorData;
        [SerializeField] private UnitDataSO _archerData;
        [SerializeField] private UnitDataSO _lancerData;

        [Header("AI Building Settings")]
        [SerializeField] private BuildingData _houseData;
        [SerializeField] private BuildingData _barracksData;
        [SerializeField] private BuildingData _towerData;

        [Tooltip("Số Barracks tối đa AI được phép xây")]
        [SerializeField, Min(1)] private int _maxBarracks = 2;

        [Tooltip("Số lượng dân dự phòng trước khi AI xây House")]
        [SerializeField, Min(1)] private int _populationBuffer = 1;

        [Tooltip("Thời gian chờ trước khi thử xây lại")]
        [SerializeField, Min(1f)] private float _buildingRetryInterval = 5f;

        [Header("FSM Settings")]
        [Tooltip("Chu kỳ ra quyết định của AI (giây)")]
        [SerializeField, Min(0.2f)] private float _decisionInterval = 1.0f;

        [Tooltip("Số lượng nông dân mục tiêu của phe Red")]
        [SerializeField, Min(1)] private int _targetWorkerCount = 5;

        [Header("Dynamic Worker Settings")]
        [SerializeField, Min(1)] private int _minWorkerCount = 4;

        [SerializeField, Min(1)] private int _maxWorkerCount = 12;

        [SerializeField, Min(0.1f)] private float _workerGrowthMultiplier = 8f;

        [Tooltip("Số lượng quân tối thiểu để bắt đầu đợt tấn công đầu tiên")]
        [SerializeField, Min(1)] private int _attackWaveSize = 6;

        [Tooltip("Số lượng quân tăng thêm sau mỗi đợt tấn công")]
        [SerializeField, Min(0)] private int _waveSizeIncrement = 2;

        [Tooltip("Bán kính quét mỏ tài nguyên xung quanh căn cứ Red")]
        [SerializeField, Min(5f)] private float _resourceSearchRadius = 50f;

        [Header("Economy Resource Targets")]
        [Tooltip("Lượng Meat AI muốn duy trì")]
        [SerializeField, Min(1)] private int _targetMeatStock = 200;

        [Tooltip("Lượng Wood AI muốn duy trì")]
        [SerializeField, Min(1)] private int _targetWoodStock = 300;

        [Tooltip("Lượng Gold AI muốn duy trì")]
        [SerializeField, Min(1)] private int _targetGoldStock = 150;

        [Tooltip("Mức ảnh hưởng của số Worker đang khai thác đến quyết định phân bổ")]
        [SerializeField, Range(0.01f, 1f)] private float _workerBalanceWeight = 0.15f;

        [Header("Runtime Info - Read Only")]
        [SerializeField] private AIState _currentState = AIState.EconomyFocus;
        [SerializeField] private int _aliveWorkersCount;
        [SerializeField] private int _readySoldiersCount;

        private readonly List<WorkerFSM> _workers = new List<WorkerFSM>();
        private readonly List<UnitCombatController> _soldiers = new List<UnitCombatController>();
        private readonly List<UnitCombatController> _attackingSquad = new List<UnitCombatController>();

        // Danh sách công trình thuộc phe AI.
        private readonly List<HouseBuilding> _enemyHouses = new List<HouseBuilding>();

        // Thời điểm sớm nhất AI được phép thử xây công trình tiếp theo.
        private float _nextBuildingAttemptTime;

        private Coroutine _aiLoopCoroutine;
        private WaitForSeconds _waitInterval;
        private int _productionRotationIndex;

        public AIState CurrentState => _currentState;

        private void OnEnable()
        {
            CastleBuilding.OnAnyPawnSpawned += HandlePawnSpawned;
            BarracksBuilding.OnAnyUnitTrained += HandleSoldierTrained;
        }

        private void Start()
        {
            AutoDiscoverReferencesIfMissing();
            ApplyRallyPointToBuildings();
            ScanExistingUnits();

            _waitInterval = new WaitForSeconds(_decisionInterval);
            _aiLoopCoroutine = StartCoroutine(AIDecisionLoop());
        }

        private void OnDisable()
        {
            CastleBuilding.OnAnyPawnSpawned -= HandlePawnSpawned;
            BarracksBuilding.OnAnyUnitTrained -= HandleSoldierTrained;

            if (_aiLoopCoroutine != null)
            {
                StopCoroutine(_aiLoopCoroutine);
                _aiLoopCoroutine = null;
            }
        }

        private IEnumerator AIDecisionLoop()
        {
            // Chờ 1 nhịp nhỏ đầu trận để Castle/Start hoàn tất sinh Pawn khởi đầu.
            yield return null;
            ScanExistingUnits();

            while (enabled)
            {
                if (IsMatchEnded())
                {
                    yield break;
                }

                ExecuteDecisionTick();
                yield return _waitInterval;
            }
        }

        private bool IsMatchEnded()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
                return true;

            if (_enemyCastle == null || _enemyCastle.IsDestroyed)
                return true;

            if (_playerCastle == null || _playerCastle.IsDestroyed)
                return true;

            return false;
        }

        /// <summary>
        /// Tính số Worker mục tiêu dựa trên mức thiếu hụt tài nguyên.
        /// Thiếu tài nguyên càng nhiều thì AI càng cần thêm Worker.
        /// </summary>
        private int CalculateDynamicWorkerTarget()
        {
            ResourceManager resourceManager = ResourceManager.Instance;

            if (resourceManager == null)
                return _targetWorkerCount;

            float meatShortage = CalculateShortageRatio(
                resourceManager.GetResourceAmount(_aiFaction, ResourceType.Meat),
                _targetMeatStock);

            float woodShortage = CalculateShortageRatio(
                resourceManager.GetResourceAmount(_aiFaction, ResourceType.Wood),
                _targetWoodStock);

            float goldShortage = CalculateShortageRatio(
                resourceManager.GetResourceAmount(_aiFaction, ResourceType.Gold),
                _targetGoldStock);

            // Lấy mức thiếu hụt lớn nhất để tránh tăng Worker quá nhanh
            // chỉ vì một loại tài nguyên đang thiếu.
            float maxShortage = Mathf.Max(
                meatShortage,
                woodShortage,
                goldShortage);

            int additionalWorkers = Mathf.RoundToInt(
                maxShortage * _workerGrowthMultiplier);

            return Mathf.Clamp(
                _targetWorkerCount + additionalWorkers,
                _minWorkerCount,
                _maxWorkerCount);
        }

        /// <summary>
        /// Trả về tỷ lệ thiếu hụt từ 0 đến 1.
        /// </summary>
        private float CalculateShortageRatio(int currentStock, int targetStock)
        {
            if (targetStock <= 0)
                return 0f;

            return 1f - Mathf.Clamp01(
                (float)currentStock / targetStock);
        }

        private void ExecuteDecisionTick()
        {
            CleanupDeadUnits();
            _aliveWorkersCount = _workers.Count;
            _readySoldiersCount = CountIdleStagingSoldiers();

            // Luôn duy trì kinh tế (đẻ nông dân nếu thiếu + điều phối nông dân Idle đi khai thác).
            MaintainEconomy();
            MaintainBuildings();

            switch (_currentState)
            {
                case AIState.EconomyFocus:
                    {
                        int dynamicTarget = CalculateDynamicWorkerTarget();

                        if (_workers.Count >= dynamicTarget)
                        {
                            _currentState = AIState.MilitaryProduction;
                        }

                        break;
                    }

                case AIState.MilitaryProduction:
                    TrainMilitaryUnits();

                    if (CountIdleStagingSoldiers() >= _attackWaveSize)
                    {
                        LaunchAttackWave();
                    }
                    break;

                case AIState.Attacking:
                    MaintainAttackingWave();
                    break;
            }
        }

        /// <summary>
        /// Duy trì hệ thống công trình của AI.
        /// Ưu tiên mở rộng sức chứa dân số trước khi xây thêm Barracks.
        /// </summary>
        private void MaintainBuildings()
        {
            if (_enemyCastle == null || _enemyCastle.IsDestroyed)
                return;

            if (Time.time < _nextBuildingAttemptTime)
                return;

            PopulationManager population = PopulationManager.Instance;

            if (population == null)
                return;

            int currentPopulation = population.GetCurrentPopulation(_aiFaction);
            int maxPopulation = population.GetMaxPopulation(_aiFaction);

            // Ưu tiên House khi dân số sắp chạm giới hạn.
            bool needsHouse =
                maxPopulation > 0 &&
                currentPopulation + _populationBuffer >= maxPopulation;

            if (needsHouse)
            {
                TryBuildBuilding(_houseData);
                return;
            }

            // Chỉ xây thêm Barracks khi chưa đạt giới hạn.
            int barracksCount = CountOwnedBarracks();

            if (barracksCount < _maxBarracks)
            {
                TryBuildBuilding(_barracksData);
            }
        }

        /// <summary>
        /// Đếm số Barracks còn tồn tại thuộc phe AI.
        /// </summary>
        private int CountOwnedBarracks()
        {
            int count = 0;

            for (int i = _enemyBarracks.Count - 1; i >= 0; i--)
            {
                BarracksBuilding barracks = _enemyBarracks[i];

                if (barracks == null || barracks.IsDestroyed)
                {
                    _enemyBarracks.RemoveAt(i);
                    continue;
                }

                if (barracks.TeamFaction == _aiFaction)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Tìm vị trí hợp lệ và đặt công trình bằng BuildingPlacementService.
        /// Service chịu trách nhiệm kiểm tra vị trí và trừ tài nguyên.
        /// </summary>
        private bool TryBuildBuilding(BuildingData buildingData)
        {
            if (buildingData == null ||
                ResourceManager.Instance == null ||
                _enemyCastle == null)
            {
                return false;
            }

            ResourceManager resourceManager = ResourceManager.Instance;

            // Không tìm vị trí nếu AI chưa đủ gỗ.
            int availableWood = resourceManager.GetResourceAmount(
                _aiFaction,
                ResourceType.Wood);

            if (availableWood < buildingData.WoodCost)
                return false;

            // Bắt đầu cooldown ngay khi thử xây, kể cả khi thất bại.
            _nextBuildingAttemptTime = Time.time + _buildingRetryInterval;

            // Tìm vị trí đặt gần Castle nhất.
            if (!AIBuildingPlacementSearch.TryFindPlacement(
                    _enemyCastle.transform.position,
                    buildingData,
                    out Vector3 anchorPosition))
            {
                Debug.LogWarning(
                    $"[EnemyAI] Không tìm được vị trí xây {buildingData.BuildingName}.");

                return false;
            }

            // Dùng chung service với Player.
            // Chỉ trừ gỗ của phe Red.
            bool placed = BuildingPlacementService.TryPlace(
                buildingData,
                _aiFaction.ToString(),
                anchorPosition,
                woodCost => resourceManager.ConsumeResource(
                    _aiFaction,
                    ResourceType.Wood,
                    woodCost),
                out GameObject buildingInstance);

            if (!placed || buildingInstance == null)
            {
                Debug.LogWarning(
                    $"[EnemyAI] Xây {buildingData.BuildingName} thất bại.");

                return false;
            }

            // Đăng ký công trình vừa xây để AI quản lý tiếp.
            if (buildingData == _houseData)
            {
                HouseBuilding house = buildingInstance.GetComponent<HouseBuilding>();

                if (house != null && !_enemyHouses.Contains(house))
                {
                    _enemyHouses.Add(house);
                }
            }
            else if (buildingData == _barracksData)
            {
                BarracksBuilding barracks =
                    buildingInstance.GetComponent<BarracksBuilding>();

                if (barracks != null && !_enemyBarracks.Contains(barracks))
                {
                    _enemyBarracks.Add(barracks);

                    if (_rallyPoint != null)
                    {
                        barracks.SetRallyPoint(_rallyPoint.position);
                    }
                }
            }

            Debug.Log(
                $"[EnemyAI] Phe {_aiFaction} đã xây {buildingData.BuildingName}.");

            return true;
        }

        private void MaintainEconomy()
        {
            if (_enemyCastle != null &&
                !_enemyCastle.IsDestroyed &&
                _pawnData != null)
            {
                int dynamicTarget = CalculateDynamicWorkerTarget();

                int totalWorkers =
                    _workers.Count + _enemyCastle.CurrentPawnQueueCount;

                bool hasQueueSpace =
                    _enemyCastle.CurrentPawnQueueCount <
                    _enemyCastle.MaxPawnQueueSize;

                if (totalWorkers < dynamicTarget &&
                    hasQueueSpace &&
                    _enemyCastle.CanSpawnPawn())
                {
                    _enemyCastle.TrySpawnPawn();
                }
            }

            AssignIdleWorkers();
        }

        private void AssignIdleWorkers()
        {
            if (MapResourceManager.Instance == null || _enemyCastle == null)
                return;

            Vector3 basePos = _enemyCastle.transform.position;

            for (int i = 0; i < _workers.Count; i++)
            {
                WorkerFSM worker = _workers[i];
                if (worker == null || worker.CurrentState != WorkerState.Idle)
                    continue;

                ResourceDeposit deposit = FindBalancedResourceDeposit(basePos);
                if (deposit != null)
                {
                    worker.AssignToResource(deposit);
                }
            }
        }

        private ResourceDeposit FindBalancedResourceDeposit(Vector3 basePos)
        {
            if (MapResourceManager.Instance == null)
                return null;

            ResourceType preferredType = PickBalancedResourceType();

            // Ưu tiên loại tài nguyên AI đang cần.
            ResourceDeposit deposit = FindLeastOccupiedDeposit(
                basePos,
                preferredType);

            if (deposit != null)
                return deposit;

            // Nếu không còn mỏ thuộc loại ưu tiên, thử các loại còn lại.
            ResourceType[] fallbackOrder =
            {
        ResourceType.Meat,
        ResourceType.Wood,
        ResourceType.Gold
    };

            for (int i = 0; i < fallbackOrder.Length; i++)
            {
                if (fallbackOrder[i] == preferredType)
                    continue;

                deposit = FindLeastOccupiedDeposit(
                    basePos,
                    fallbackOrder[i]);

                if (deposit != null)
                    return deposit;
            }

            return null;
        }

        /// <summary>
        /// Chọn mỏ có ít Worker được phân công nhất.
        /// Nếu số Worker bằng nhau, ưu tiên mỏ gần căn cứ hơn.
        /// </summary>
        private ResourceDeposit FindLeastOccupiedDeposit(
            Vector3 basePos,
            ResourceType resourceType)
        {
            IReadOnlyList<ResourceDeposit> deposits =
                MapResourceManager.Instance.AllResources;

            ResourceDeposit bestDeposit = null;
            int lowestWorkerCount = int.MaxValue;
            float shortestDistanceSqr = _resourceSearchRadius * _resourceSearchRadius;

            for (int i = 0; i < deposits.Count; i++)
            {
                ResourceDeposit deposit = deposits[i];

                if (deposit == null ||
                    deposit.currentAmount <= 0 ||
                    deposit.resourceType != resourceType)
                {
                    continue;
                }

                float distanceSqr =
                    (deposit.transform.position - basePos).sqrMagnitude;

                if (distanceSqr > _resourceSearchRadius * _resourceSearchRadius)
                    continue;

                int assignedWorkers = CountWorkersAssignedToDeposit(deposit);

                bool isBetter =
                    assignedWorkers < lowestWorkerCount ||
                    (assignedWorkers == lowestWorkerCount &&
                     distanceSqr < shortestDistanceSqr);

                if (!isBetter)
                    continue;

                bestDeposit = deposit;
                lowestWorkerCount = assignedWorkers;
                shortestDistanceSqr = distanceSqr;
            }

            return bestDeposit;
        }

        /// <summary>
        /// Đếm số Worker đang giữ mỏ này làm mục tiêu.
        /// Bao gồm cả Worker đang di chuyển, khai thác và quay về Castle.
        /// </summary>
        private int CountWorkersAssignedToDeposit(ResourceDeposit deposit)
        {
            int count = 0;

            for (int i = 0; i < _workers.Count; i++)
            {
                WorkerFSM worker = _workers[i];

                if (worker == null)
                    continue;

                if (worker.TargetResource == deposit)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Chọn loại tài nguyên cần khai thác dựa trên:
        /// - Tỷ lệ thiếu hụt so với lượng dự trữ mục tiêu.
        /// - Số Worker hiện đang được phân công khai thác loại đó.
        /// </summary>
        private ResourceType PickBalancedResourceType()
        {
            ResourceManager resourceManager = ResourceManager.Instance;

            if (resourceManager == null)
                return ResourceType.Wood;

            int meatWorkers = 0;
            int woodWorkers = 0;
            int goldWorkers = 0;

            for (int i = 0; i < _workers.Count; i++)
            {
                WorkerFSM worker = _workers[i];

                if (worker == null || worker.CurrentState == WorkerState.Idle)
                    continue;

                switch (worker.CurrentResourceType)
                {
                    case ResourceType.Meat:
                        meatWorkers++;
                        break;

                    case ResourceType.Wood:
                        woodWorkers++;
                        break;

                    case ResourceType.Gold:
                        goldWorkers++;
                        break;
                }
            }

            int meatStock = resourceManager.GetResourceAmount(
                _aiFaction, ResourceType.Meat);

            int woodStock = resourceManager.GetResourceAmount(
                _aiFaction, ResourceType.Wood);

            int goldStock = resourceManager.GetResourceAmount(
                _aiFaction, ResourceType.Gold);

            // Điểm càng cao thì loại tài nguyên càng cần được ưu tiên.
            float meatScore = CalculateResourcePriority(
                meatStock, _targetMeatStock, meatWorkers);

            float woodScore = CalculateResourcePriority(
                woodStock, _targetWoodStock, woodWorkers);

            float goldScore = CalculateResourcePriority(
                goldStock, _targetGoldStock, goldWorkers);

            if (woodScore >= meatScore && woodScore >= goldScore)
                return ResourceType.Wood;

            if (meatScore >= goldScore)
                return ResourceType.Meat;

            return ResourceType.Gold;
        }

        /// <summary>
        /// Tính độ ưu tiên khai thác.
        /// Kho càng thiếu thì điểm càng cao.
        /// Nhiều Worker cùng khai thác thì điểm giảm.
        /// </summary>
        private float CalculateResourcePriority(
            int currentStock,
            int targetStock,
            int assignedWorkers)
        {
            float stockRatio = Mathf.Clamp01(
                (float)currentStock / Mathf.Max(1, targetStock));

            float shortageScore = 1f - stockRatio;
            float workerPenalty = assignedWorkers * _workerBalanceWeight;

            return shortageScore - workerPenalty;
        }

        private void TrainMilitaryUnits()
        {
            UnitDataSO[] roster = { _warriorData, _archerData, _lancerData };
            for (int attempt = 0; attempt < roster.Length; attempt++)
            {
                int index = (_productionRotationIndex + attempt) % roster.Length;
                UnitDataSO candidate = roster[index];

                if (candidate == null)
                    continue;

                if (TryQueueAtAvailableBarracks(candidate))
                {
                    _productionRotationIndex = (index + 1) % roster.Length;
                    return;
                }
            }
        }

        private bool TryQueueAtAvailableBarracks(UnitDataSO unitData)
        {
            ResourceManager res = ResourceManager.Instance;
            PopulationManager pop = PopulationManager.Instance;

            if (res == null || !unitData.CanAfford(res, _aiFaction))
                return false;

            if (pop != null && !pop.CanSpawnUnit(_aiFaction, unitData.PopulationCost))
                return false;

            for (int i = 0; i < _enemyBarracks.Count; i++)
            {
                BarracksBuilding barracks = _enemyBarracks[i];
                if (barracks == null || barracks.IsDestroyed)
                    continue;

                if (barracks.CurrentQueueCount >= barracks.MaxQueueSize)
                    continue;

                if (!BarracksSupportsUnit(barracks, unitData))
                    continue;

                if (barracks.TryQueueUnit(unitData))
                    return true;
            }

            return false;
        }

        private static bool BarracksSupportsUnit(BarracksBuilding barracks, UnitDataSO unitData)
        {
            IReadOnlyList<UnitDataSO> trainable = barracks.TrainableUnits;
            if (trainable == null)
                return false;

            for (int i = 0; i < trainable.Count; i++)
            {
                if (trainable[i] == unitData)
                    return true;
            }

            return false;
        }

        private bool CanAffordAnyMilitaryUnit()
        {
            ResourceManager res = ResourceManager.Instance;
            if (res == null)
                return false;

            return (_warriorData != null && _warriorData.CanAfford(res, _aiFaction)) ||
                   (_archerData != null && _archerData.CanAfford(res, _aiFaction)) ||
                   (_lancerData != null && _lancerData.CanAfford(res, _aiFaction));
        }

        private void LaunchAttackWave()
        {
            if (_playerCastle == null || _playerCastle.IsDestroyed)
                return;

            _attackingSquad.Clear();

            Vector3 targetPos = _playerCastle.transform.position;

            for (int i = 0; i < _soldiers.Count; i++)
            {
                if (_attackingSquad.Count >= _attackWaveSize)
                    break;

                UnitCombatController soldier = _soldiers[i];

                if (!IsSoldierAlive(soldier))
                    continue;

                if (_attackingSquad.Contains(soldier))
                    continue;

                if (soldier.OrderMode != CombatOrderMode.IdleDefend)
                    continue;

                _attackingSquad.Add(soldier);
                soldier.AttackMoveTo(targetPos);
            }

            if (_attackingSquad.Count >= _attackWaveSize)
            {
                _currentState = AIState.Attacking;

                Debug.Log(
                    $"[EnemyAI] Phe {_aiFaction} phát động đợt tấn công " +
                    $"với {_attackingSquad.Count} quân!"
                );
            }
        }

        private void MaintainAttackingWave()
        {
            int aliveCount = 0;
            Vector3 targetPos = _playerCastle != null ? _playerCastle.transform.position : Vector3.zero;

            for (int i = _attackingSquad.Count - 1; i >= 0; i--)
            {
                UnitCombatController soldier = _attackingSquad[i];
                if (!IsSoldierAlive(soldier))
                {
                    _attackingSquad.RemoveAt(i);
                    continue;
                }

                aliveCount++;

                // Nếu lính vẫn ở trạng thái Idle giữa đường thì tiếp tục đẩy về Castle Blue.
                if (soldier.OrderMode == CombatOrderMode.IdleDefend && _playerCastle != null)
                {
                    soldier.AttackMoveTo(targetPos);
                }
            }

            if (aliveCount == 0)
            {
                _attackWaveSize += _waveSizeIncrement;
                _currentState = AIState.EconomyFocus;
                Debug.Log($"[EnemyAI] Đợt tấn công kết thúc. Quay lại củng cố kinh tế, ngưỡng đợt sau: {_attackWaveSize} quân.");
            }
        }

        private int CountIdleStagingSoldiers()
        {
            int count = 0;

            for (int i = 0; i < _soldiers.Count; i++)
            {
                UnitCombatController soldier = _soldiers[i];

                if (!IsSoldierAlive(soldier))
                    continue;

                if (_attackingSquad.Contains(soldier))
                    continue;

                if (soldier.OrderMode != CombatOrderMode.IdleDefend)
                    continue;

                count++;
            }

            return count;
        }

        private void HandlePawnSpawned(CastleBuilding castle, GameObject pawn)
        {
            if (castle == null || pawn == null || castle.TeamFaction != _aiFaction)
                return;

            WorkerFSM worker = pawn.GetComponent<WorkerFSM>();
            if (worker != null && !_workers.Contains(worker))
            {
                _workers.Add(worker);
            }
        }

        private void HandleSoldierTrained(BarracksBuilding barracks, GameObject spawnedUnit)
        {
            if (barracks == null || spawnedUnit == null || barracks.TeamFaction != _aiFaction)
                return;

            UnitCombatController combat = spawnedUnit.GetComponent<UnitCombatController>();
            if (combat != null && !_soldiers.Contains(combat))
            {
                _soldiers.Add(combat);
            }
        }

        private void ScanExistingUnits()
        {
            _workers.Clear();
            _soldiers.Clear();

            WorkerFSM[] allWorkers = FindObjectsOfType<WorkerFSM>();
            for (int i = 0; i < allWorkers.Length; i++)
            {
                WorkerFSM w = allWorkers[i];
                if (w != null && w.WorkerFaction == _aiFaction)
                {
                    _workers.Add(w);
                }
            }

            UnitCombatController[] allSoldiers = FindObjectsOfType<UnitCombatController>();
            for (int i = 0; i < allSoldiers.Length; i++)
            {
                UnitCombatController s = allSoldiers[i];
                if (!IsSoldierAlive(s))
                    continue;

                HealthSystem hs = s.GetComponent<HealthSystem>();
                if (hs != null && hs.TeamFaction == _aiFaction)
                {
                    _soldiers.Add(s);
                }
            }
        }

        private void CleanupDeadUnits()
        {
            for (int i = _workers.Count - 1; i >= 0; i--)
            {
                WorkerFSM worker = _workers[i];
                if (worker == null)
                {
                    _workers.RemoveAt(i);
                    continue;
                }

                HealthSystem hs = worker.GetComponent<HealthSystem>();
                if (hs != null && hs.IsDead)
                {
                    _workers.RemoveAt(i);
                }
            }

            for (int i = _soldiers.Count - 1; i >= 0; i--)
            {
                if (!IsSoldierAlive(_soldiers[i]))
                {
                    _soldiers.RemoveAt(i);
                }
            }
        }

        private static bool IsSoldierAlive(UnitCombatController soldier)
        {
            if (soldier == null)
                return false;

            HealthSystem hs = soldier.GetComponent<HealthSystem>();
            return hs == null || !hs.IsDead;
        }

        private void AutoDiscoverReferencesIfMissing()
        {
            CastleBuilding[] castles = FindObjectsOfType<CastleBuilding>();
            for (int i = 0; i < castles.Length; i++)
            {
                if (castles[i] == null)
                    continue;

                if (castles[i].TeamFaction == _aiFaction && _enemyCastle == null)
                    _enemyCastle = castles[i];
                else if (castles[i].TeamFaction == Faction.Blue && _playerCastle == null)
                    _playerCastle = castles[i];
            }

            if (_enemyBarracks.Count == 0)
            {
                BarracksBuilding[] allBarracks = FindObjectsOfType<BarracksBuilding>();
                for (int i = 0; i < allBarracks.Length; i++)
                {
                    if (allBarracks[i] != null && allBarracks[i].TeamFaction == _aiFaction)
                    {
                        _enemyBarracks.Add(allBarracks[i]);
                    }
                }
            }

            // Tìm các House thuộc phe AI đã được đặt sẵn trong Scene.
            _enemyHouses.Clear();

            HouseBuilding[] allHouses = FindObjectsOfType<HouseBuilding>();

            for (int i = 0; i < allHouses.Length; i++)
            {
                HouseBuilding house = allHouses[i];

                if (house != null &&
                    !house.IsDestroyed &&
                    house.TeamFaction == _aiFaction)
                {
                    _enemyHouses.Add(house);
                }
            }
        }

        private void ApplyRallyPointToBuildings()
        {
            if (_rallyPoint == null)
                return;

            for (int i = 0; i < _enemyBarracks.Count; i++)
            {
                if (_enemyBarracks[i] != null)
                {
                    _enemyBarracks[i].SetRallyPoint(_rallyPoint.position);
                }
            }
        }
    }
}
