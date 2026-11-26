
using UnityEngine;
using System;
using System.Collections.Generic;
using TinyTactics.Core;
using TinyTactics.Units;
using TinyTactics.Economy;
using TinyTactics.Factions;
using TinyTactics.UI;

namespace TinyTactics.Buildings
{
    [RequireComponent(typeof(CastleDepositArea))]
    public class CastleBuilding : BuildingBase
    {
        [Header("Castle Population")]
        [SerializeField] private int castlePopulationProvided = 5;

        [Header("Resource Drop-Off Settings")]
        [SerializeField] private float dropOffRadius = 3f;
        [SerializeField] private Transform dropOffPoint;

        [Header("Pawn Spawn Settings")]
        [SerializeField] private UnitDataSO pawnData;
        [SerializeField] private float spawnSpacing = 0.6f;
        [SerializeField, Min(0.1f)] private float pawnSpawnDelay = 5f;
        [SerializeField, Min(1)] private int maxQueueSize = 5;

        [Tooltip("Bật nếu đây là Castle có sẵn từ đầu trận.")]
        [SerializeField] private bool spawnStartingPawns = false;

        [Tooltip("Số Pawn được sinh khi bắt đầu trận.")]
        [SerializeField, Min(0)] private int startingPawnCount = 4;

        [Header("Pawn Faction Animation")]
        [SerializeField] private RuntimeAnimatorController pawnBlueController;
        [SerializeField] private RuntimeAnimatorController pawnRedController;

        private CastleDepositArea depositArea;
        private bool populationGranted;
        private bool startingPawnsSpawned;
        private bool isSpawningPawn;
        private float pawnSpawnTimer;
        private readonly List<UnitDataSO> pawnQueue =
            new List<UnitDataSO>();

        public event Action<float> OnPawnSpawnProgressChanged;
        public event Action<IReadOnlyList<UnitDataSO>> OnPawnQueueUpdated;
        public static event Action<CastleBuilding, GameObject> OnAnyPawnSpawned;
        public IReadOnlyList<UnitDataSO> PawnQueue => pawnQueue;
        public int CurrentPawnQueueCount => pawnQueue.Count;
        public int MaxPawnQueueSize => maxQueueSize;
        public UnitDataSO CurrentPawnQueueUnit =>
            pawnQueue.Count > 0 ? pawnQueue[0] : null;
        public bool IsSpawningPawn => isSpawningPawn;
        public float PawnSpawnDelay => pawnSpawnDelay;
        public float PawnSpawnProgress =>
            !isSpawningPawn || pawnSpawnDelay <= 0f
                ? 0f
                : Mathf.Clamp01(1f - (pawnSpawnTimer / pawnSpawnDelay));
        public float PawnSpawnRemainingTime =>
            isSpawningPawn ? Mathf.Max(0f, pawnSpawnTimer) : 0f;

        public float DropOffRadius => dropOffRadius;

        protected override void Awake()
        {
            buildingName = "Castle";

            if (maxHP <= 500)
                maxHP = 2500;

            buildingGridSize = new Vector2Int(3, 3);
            cameraShakeDuration = 0.5f;
            cameraShakeMagnitude = 0.35f;

            base.Awake();

            depositArea = GetComponent<CastleDepositArea>();

            if (depositArea == null)
                depositArea = gameObject.AddComponent<CastleDepositArea>();
        }

        protected override void Start()
        {
            base.Start();

            // Cấp Population trước khi đăng ký các Pawn khởi đầu.
            GrantCastlePopulation();

            if (spawnStartingPawns)
                SpawnStartingPawns();
        }

        private void Update()
        {
            if (!isSpawningPawn || isDestroyed)
                return;

            pawnSpawnTimer -= Time.deltaTime;
            OnPawnSpawnProgressChanged?.Invoke(PawnSpawnProgress);

            if (pawnSpawnTimer <= 0f)
                CompletePawnSpawn();
        }

        /// <summary>
        /// Gán gameplay faction cho unit ngay sau khi Instantiate.
        /// </summary>
        private bool ConfigurePawnIdentity(GameObject pawn)
        {
            if (pawn == null)
                return false;

            UnitFaction identity = pawn.GetComponent<UnitFaction>();

            if (identity == null)
            {
                Debug.LogError(
                    $"[CastleBuilding] {pawn.name} thiếu UnitFaction trên prefab!",
                    pawn
                );

                Destroy(pawn);
                return false;
            }

            identity.SetFaction(TeamFaction);
            return true;
        }

        #region Starting Pawn Spawn

        /// <summary>
        /// Sinh Pawn khởi đầu, không tiêu tốn tài nguyên.
        /// </summary>
        private void SpawnStartingPawns()
        {
            if (startingPawnsSpawned || isDestroyed)
                return;

            if (pawnData == null || pawnData.UnitPrefab == null)
            {
                Debug.LogError(
                    $"[CastleBuilding] {FactionId}: Chưa cấu hình Pawn Data hoặc Unit Prefab.",
                    this
                );
                return;
            }

            startingPawnsSpawned = true;

            Vector3 center = GetRallyPointPosition();

            Vector3[] offsets =
            {
                new Vector3(-spawnSpacing, -spawnSpacing, 0f),
                new Vector3(spawnSpacing, -spawnSpacing, 0f),
                new Vector3(-spawnSpacing, spawnSpacing, 0f),
                new Vector3(spawnSpacing, spawnSpacing, 0f)
            };

            int spawnedCount = 0;

            for (int i = 0; i < startingPawnCount; i++)
            {
                Vector3 spawnPosition =
                    center + offsets[i % offsets.Length];

                GameObject pawn = Instantiate(
                    pawnData.UnitPrefab,
                    spawnPosition,
                    Quaternion.identity
                );

                if (pawn == null)
                    continue;

                // Gán gameplay faction trước khi cấu hình visual.
                if (!ConfigurePawnIdentity(pawn))
                    continue;

                // Cấu hình Animator và Minimap.
                ConfigurePawnFaction(pawn);

                // Đăng ký dân số theo đúng phe của Castle.
                if (PopulationManager.Instance != null)
                {
                    PopulationManager.Instance.RegisterUnit(
                        TeamFaction,
                        pawnData.PopulationCost
                    );
                }

                OnAnyPawnSpawned?.Invoke(this, pawn);

                spawnedCount++;
            }

            Debug.Log(
                $"[CastleBuilding] {FactionId} đã sinh " +
                $"{spawnedCount}/{startingPawnCount} Pawn khởi đầu.",
                this
            );
        }

        #endregion

        #region Pawn Faction Visual

        /// <summary>
        /// Thêm FactionVisual nếu chưa tồn tại,
        /// cấu hình Animator Controller và đồng bộ Minimap.
        /// Không cần thêm FactionVisual vào prefab.
        /// </summary>
        private void ConfigurePawnFaction(GameObject pawn)
        {
            if (pawn == null)
                return;

            // Lấy hoặc thêm FactionVisual.
            FactionVisual factionVisual =
                pawn.GetComponent<FactionVisual>();

            if (factionVisual == null)
                factionVisual = pawn.AddComponent<FactionVisual>();

            // Cấu hình Animator Controller theo phe của Castle.
            bool configured = factionVisual.Configure(
                pawnBlueController,
                pawnRedController,
                FactionId
            );

            if (!configured)
            {
                Debug.LogWarning(
                    $"[CastleBuilding] Không thể cấu hình FactionVisual " +
                    $"cho {pawn.name}, phe {FactionId}. " +
                    "Kiểm tra Animator Controller trong Inspector.",
                    pawn
                );
            }

            // Đồng bộ phe trên Minimap.
            MinimapTarget minimapTarget =
                pawn.GetComponent<MinimapTarget>();

            if (minimapTarget != null)
            {
                minimapTarget.SetFaction(FactionId);
            }
            else
            {
                Debug.LogWarning(
                    $"[CastleBuilding] {pawn.name} chưa có MinimapTarget.",
                    pawn
                );
            }
        }

        #endregion

        #region Population

        private void GrantCastlePopulation()
        {
            if (populationGranted)
                return;

            if (PopulationManager.Instance != null)
            {
                PopulationManager.Instance.AddMaxPopulation(
                    TeamFaction,
                    castlePopulationProvided
                );

                populationGranted = true;

                Debug.Log(
                    $"[CastleBuilding] {FactionId} đã cấp +" +
                    $"{castlePopulationProvided} Population.",
                    this
                );
            }
        }

        private void RevokeCastlePopulation()
        {
            if (!populationGranted)
                return;

            if (PopulationManager.Instance != null)
            {
                PopulationManager.Instance.RemoveMaxPopulation(
                    TeamFaction,
                    castlePopulationProvided
                );

                populationGranted = false;
            }
        }

        #endregion

        #region Resource Drop-Off

        public Vector3 GetDropOffPosition()
        {
            if (dropOffPoint != null)
                return dropOffPoint.position;

            if (depositArea != null)
                return depositArea.GetDropOffPosition();

            return transform.position;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            WorkerFSM worker = other.GetComponent<WorkerFSM>();

            if (worker != null)
                OnWorkerArrivedAtCastle(worker);
        }

        public void OnWorkerArrivedAtCastle(WorkerFSM worker)
        {
            if (worker == null)
                return;

            // WorkerFSM tự xử lý việc nộp tài nguyên.
        }

        #endregion

        #region Castle Destruction

        protected override void OnDestroyed()
        {
            if (isDestroyed)
                return;

            if (isSpawningPawn)
                RefundPendingPawn();

            RevokeCastlePopulation();

            if (IsPlayerFaction)
            {
                Debug.Log(
                    "<color=red><b>[CastleBuilding] " +
                    "LÂU ĐÀI NGƯỜI CHƠI BỊ TIÊU DIỆT! THUA CUỘC!</b></color>"
                );

                GameManager.TriggerPlayerDefeated();
            }
            else
            {
                Debug.Log(
                    "<color=green><b>[CastleBuilding] " +
                    "LÂU ĐÀI ĐỐI PHƯƠNG BỊ TIÊU DIỆT! CHIẾN THẮNG!</b></color>"
                );

                GameManager.TriggerPlayerVictory();
            }

            base.OnDestroyed();
        }

        protected override void OnDestroy()
        {
            if (isSpawningPawn)
                RefundPendingPawn();

            RevokeCastlePopulation();

            if (!isDestroyed && IsPlayerFaction)
                GameManager.TriggerPlayerDefeated();

            base.OnDestroy();
        }

        #endregion

        #region Pawn Manual Spawn

        /// <summary>
        /// Kiểm tra điều kiện sinh Pawn từ Castle theo đúng phe sở hữu.
        /// </summary>
        public bool CanSpawnPawn()
        {
            if (isDestroyed || pawnData == null)
                return false;

            ResourceManager resources = ResourceManager.Instance;
            PopulationManager population = PopulationManager.Instance;

            if (resources == null || population == null)
                return false;

            return pawnData.CanAfford(resources, TeamFaction) &&
                   population.CanSpawnUnit(TeamFaction, pawnData.PopulationCost);
        }

        /// <summary>
        /// Sinh một Pawn từ Castle (dùng cho cả Player Blue và AI Red).
        /// </summary>
        public bool TrySpawnPawn()
        {
            if (isDestroyed)
            {
                Debug.LogWarning(
                    "[CastleBuilding] Không thể thêm Pawn: Castle đã bị phá hủy.",
                    this
                );
                return false;
            }

            if (pawnQueue.Count >= maxQueueSize)
            {
                Debug.LogWarning(
                    $"[CastleBuilding] Hàng đợi Pawn đã đầy ({maxQueueSize}/{maxQueueSize}).",
                    this
                );
                return false;
            }

            if (!CanSpawnPawn())
            {
                Debug.LogWarning(
                    "[CastleBuilding] Không đủ điều kiện Spawn Pawn.",
                    this
                );

                return false;
            }

            ResourceManager resources = ResourceManager.Instance;
            PopulationManager population = PopulationManager.Instance;

            bool paid = resources.ConsumeResource(
                TeamFaction,
                ResourceType.Meat,
                pawnData.MeatCost
            );

            if (!paid)
                return false;

            if (pawnData.WoodCost > 0)
                resources.ConsumeResource(TeamFaction, ResourceType.Wood, pawnData.WoodCost);

            if (pawnData.GoldCost > 0)
                resources.ConsumeResource(TeamFaction, ResourceType.Gold, pawnData.GoldCost);

            population.RegisterUnit(TeamFaction, pawnData.PopulationCost);

            pawnQueue.Add(pawnData);

            if (pawnQueue.Count == 1)
            {
                isSpawningPawn = true;
                pawnSpawnTimer = pawnSpawnDelay;
                OnPawnSpawnProgressChanged?.Invoke(0f);
            }

            OnPawnQueueUpdated?.Invoke(pawnQueue);

            Debug.Log(
                $"[CastleBuilding] Đã thêm Pawn vào hàng đợi. " +
                $"Vị trí: {pawnQueue.Count}/{maxQueueSize}.",
                this
            );

            return true;
        }

        private void CompletePawnSpawn()
        {
            isSpawningPawn = false;
            pawnSpawnTimer = 0f;

            Vector3 spawnPosition = GetRallyPointPosition();

            spawnPosition += new Vector3(
                UnityEngine.Random.Range(-spawnSpacing, spawnSpacing),
                UnityEngine.Random.Range(-spawnSpacing, spawnSpacing),
                0f
            );

            GameObject pawn = Instantiate(
                pawnData.UnitPrefab,
                spawnPosition,
                Quaternion.identity
            );

            if (pawn != null && ConfigurePawnIdentity(pawn))
            {
                ConfigurePawnFaction(pawn);

                OnAnyPawnSpawned?.Invoke(this, pawn);

                PopulationManager population = PopulationManager.Instance;
                if (population != null)
                {
                    Debug.Log(
                        $"[CastleBuilding] Spawn Pawn thành công. " +
                        $"Population: {population.CurrentPopulation}/" +
                        $"{population.MaxPopulation}",
                        this
                    );
                }

                if (pawnQueue.Count > 0)
                    pawnQueue.RemoveAt(0);

                OnPawnQueueUpdated?.Invoke(pawnQueue);
            }
            else
            {
                RefundPawnAtIndex(0);
            }

            if (pawnQueue.Count > 0)
            {
                isSpawningPawn = true;
                pawnSpawnTimer = pawnSpawnDelay;
            }

            OnPawnSpawnProgressChanged?.Invoke(0f);
        }

        public void CancelPawnQueue(int index)
        {
            RefundPawnAtIndex(index);
        }

        private void RefundPawnAtIndex(int index)
        {
            if (index < 0 || index >= pawnQueue.Count)
                return;

            UnitDataSO cancelledUnit = pawnQueue[index];
            pawnQueue.RemoveAt(index);

            if (cancelledUnit != null)
            {
                ResourceManager resources = ResourceManager.Instance;
                PopulationManager population = PopulationManager.Instance;

                if (resources != null)
                {
                    resources.AddResource(TeamFaction, ResourceType.Meat, cancelledUnit.MeatCost);
                    resources.AddResource(TeamFaction, ResourceType.Wood, cancelledUnit.WoodCost);
                    resources.AddResource(TeamFaction, ResourceType.Gold, cancelledUnit.GoldCost);
                }

                population?.UnregisterUnit(TeamFaction, cancelledUnit.PopulationCost);
            }

            if (index == 0)
            {
                if (pawnQueue.Count > 0)
                {
                    isSpawningPawn = true;
                    pawnSpawnTimer = pawnSpawnDelay;
                    OnPawnSpawnProgressChanged?.Invoke(0f);
                }
                else
                {
                    isSpawningPawn = false;
                    pawnSpawnTimer = 0f;
                    OnPawnSpawnProgressChanged?.Invoke(0f);
                }
            }

            OnPawnQueueUpdated?.Invoke(pawnQueue);
        }

        private void ClearAndRefundPawnQueue()
        {
            while (pawnQueue.Count > 0)
                RefundPawnAtIndex(0);
        }

        private void RefundPendingPawn()
        {
            ClearAndRefundPawnQueue();
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;

            Vector3 center = dropOffPoint != null
                ? dropOffPoint.position
                : transform.position;

            Gizmos.DrawWireSphere(center, dropOffRadius);
        }
    }
}
