
using UnityEngine;
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

                // Thêm và cấu hình FactionVisual tại runtime.
                ConfigurePawnFaction(pawn);

                // Chỉ quản lý Population của phe người chơi Blue.
                if (IsPlayerFaction &&
                    PopulationManager.Instance != null)
                {
                    PopulationManager.Instance.RegisterUnit(
                        pawnData.PopulationCost
                    );
                }

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

            if (IsPlayerFaction &&
                PopulationManager.Instance != null)
            {
                PopulationManager.Instance.AddMaxPopulation(
                    castlePopulationProvided
                );

                populationGranted = true;

                Debug.Log(
                    $"[CastleBuilding] Đã cấp +" +
                    $"{castlePopulationProvided} Population.",
                    this
                );
            }
        }

        private void RevokeCastlePopulation()
        {
            if (!populationGranted)
                return;

            if (IsPlayerFaction &&
                PopulationManager.Instance != null)
            {
                PopulationManager.Instance.RemoveMaxPopulation(
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
            RevokeCastlePopulation();

            if (!isDestroyed && IsPlayerFaction)
                GameManager.TriggerPlayerDefeated();

            base.OnDestroy();
        }

        #endregion

        #region Pawn Manual Spawn

        /// <summary>
        /// Kiểm tra điều kiện sinh Pawn từ Castle.
        /// </summary>
        public bool CanSpawnPawn()
        {
            if (isDestroyed || !IsPlayerFaction || pawnData == null)
                return false;

            ResourceManager resources = ResourceManager.Instance;
            PopulationManager population = PopulationManager.Instance;

            if (resources == null || population == null)
                return false;

            return pawnData.CanAfford(resources) &&
                   population.CanSpawnUnit(pawnData.PopulationCost);
        }

        /// <summary>
        /// Sinh một Pawn từ Castle của người chơi.
        /// </summary>
        public bool TrySpawnPawn()
        {
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

            Vector3 spawnPosition = GetRallyPointPosition();

            spawnPosition += new Vector3(
                Random.Range(-spawnSpacing, spawnSpacing),
                Random.Range(-spawnSpacing, spawnSpacing),
                0f
            );

            GameObject pawn = Instantiate(
                pawnData.UnitPrefab,
                spawnPosition,
                Quaternion.identity
            );

            if (pawn == null)
                return false;

            // Dùng chung logic cấu hình visual với Pawn khởi đầu.
            ConfigurePawnFaction(pawn);

            // Trừ tài nguyên.
            bool paid = resources.ConsumeResource(
                ResourceType.Meat,
                pawnData.MeatCost
            );

            if (!paid)
            {
                Destroy(pawn);
                return false;
            }

            if (pawnData.WoodCost > 0)
            {
                resources.ConsumeResource(
                    ResourceType.Wood,
                    pawnData.WoodCost
                );
            }

            if (pawnData.GoldCost > 0)
            {
                resources.ConsumeResource(
                    ResourceType.Gold,
                    pawnData.GoldCost
                );
            }

            // Đăng ký Population.
            population.RegisterUnit(pawnData.PopulationCost);

            Debug.Log(
                $"[CastleBuilding] Spawn Pawn thành công. " +
                $"Population: {population.CurrentPopulation}/" +
                $"{population.MaxPopulation}",
                this
            );

            return true;
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
