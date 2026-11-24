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

        [Header("FSM Settings")]
        [Tooltip("Chu kỳ ra quyết định của AI (giây)")]
        [SerializeField, Min(0.2f)] private float _decisionInterval = 1.0f;

        [Tooltip("Số lượng nông dân mục tiêu của phe Red")]
        [SerializeField, Min(1)] private int _targetWorkerCount = 5;

        [Tooltip("Số lượng quân tối thiểu để bắt đầu đợt tấn công đầu tiên")]
        [SerializeField, Min(1)] private int _attackWaveSize = 6;

        [Tooltip("Số lượng quân tăng thêm sau mỗi đợt tấn công")]
        [SerializeField, Min(0)] private int _waveSizeIncrement = 2;

        [Tooltip("Bán kính quét mỏ tài nguyên xung quanh căn cứ Red")]
        [SerializeField, Min(5f)] private float _resourceSearchRadius = 50f;

        [Header("Runtime Info - Read Only")]
        [SerializeField] private AIState _currentState = AIState.EconomyFocus;
        [SerializeField] private int _aliveWorkersCount;
        [SerializeField] private int _readySoldiersCount;

        private readonly List<WorkerFSM> _workers = new List<WorkerFSM>();
        private readonly List<UnitCombatController> _soldiers = new List<UnitCombatController>();
        private readonly List<UnitCombatController> _attackingSquad = new List<UnitCombatController>();

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

        private void ExecuteDecisionTick()
        {
            CleanupDeadUnits();
            _aliveWorkersCount = _workers.Count;
            _readySoldiersCount = CountIdleStagingSoldiers();

            // Luôn duy trì kinh tế (đẻ nông dân nếu thiếu + điều phối nông dân Idle đi khai thác).
            MaintainEconomy();

            switch (_currentState)
            {
                case AIState.EconomyFocus:
                    if (_workers.Count >= Mathf.Min(3, _targetWorkerCount) || CanAffordAnyMilitaryUnit())
                    {
                        _currentState = AIState.MilitaryProduction;
                    }
                    break;

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

        private void MaintainEconomy()
        {
            if (_enemyCastle != null && !_enemyCastle.IsDestroyed && _pawnData != null)
            {
                int totalWorkers = _workers.Count + _enemyCastle.CurrentPawnQueueCount;
                if (totalWorkers < _targetWorkerCount && _enemyCastle.CanSpawnPawn())
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
            ResourceType preferredType = PickBalancedResourceType();
            ResourceDeposit deposit = MapResourceManager.Instance.GetClosestResource(
                basePos,
                preferredType,
                _resourceSearchRadius);

            if (deposit != null)
                return deposit;

            // Nếu loại ưu tiên đã hết quanh căn cứ, fallback sang các loại còn lại.
            ResourceType[] fallbackOrder = { ResourceType.Meat, ResourceType.Wood, ResourceType.Gold };
            for (int i = 0; i < fallbackOrder.Length; i++)
            {
                if (fallbackOrder[i] == preferredType)
                    continue;

                deposit = MapResourceManager.Instance.GetClosestResource(
                    basePos,
                    fallbackOrder[i],
                    _resourceSearchRadius);

                if (deposit != null)
                    return deposit;
            }

            return null;
        }

        private ResourceType PickBalancedResourceType()
        {
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

            ResourceManager res = ResourceManager.Instance;
            int meatStock = res != null ? res.GetResourceAmount(_aiFaction, ResourceType.Meat) : 0;
            int woodStock = res != null ? res.GetResourceAmount(_aiFaction, ResourceType.Wood) : 0;
            int goldStock = res != null ? res.GetResourceAmount(_aiFaction, ResourceType.Gold) : 0;

            // Ưu tiên tài nguyên có ít nông dân khai thác và trữ lượng đang thấp.
            float meatScore = meatWorkers * 100f + meatStock * 0.1f;
            float woodScore = woodWorkers * 100f + woodStock * 0.1f;
            float goldScore = goldWorkers * 100f + goldStock * 0.1f;

            if (meatScore <= woodScore && meatScore <= goldScore)
                return ResourceType.Meat;

            if (woodScore <= goldScore)
                return ResourceType.Wood;

            return ResourceType.Gold;
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
                UnitCombatController soldier = _soldiers[i];
                if (!IsSoldierAlive(soldier))
                    continue;

                _attackingSquad.Add(soldier);
                soldier.AttackMoveTo(targetPos);
            }

            if (_attackingSquad.Count > 0)
            {
                _currentState = AIState.Attacking;
                Debug.Log($"[EnemyAI] Phe {_aiFaction} phát động đợt tấn công với {_attackingSquad.Count} quân!");
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

                if (!_attackingSquad.Contains(soldier))
                {
                    count++;
                }
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
