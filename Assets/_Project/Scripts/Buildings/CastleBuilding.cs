using UnityEngine;
using TinyTactics.Core;
using TinyTactics.Units;
using TinyTactics.Economy;

namespace TinyTactics.Buildings
{
    /// <summary>
    /// Công trình Lâu đài (Castle) — Căn cứ đầu não của mỗi phe.
    /// - Cấp sẵn +5 Sức chứa tối đa (Max Population) cho phe Blue lúc khởi đầu.
    /// - Là điểm nộp tài nguyên (Drop-off point) cho Nông dân (Pawn / Worker).
    /// - Là điều kiện THẮNG/THUA: Nếu Lâu đài của Player bị hủy -> Kích hoạt Thua cuộc (GameManager.OnPlayerDefeated).
    /// </summary>
    [RequireComponent(typeof(CastleDepositArea))]
    public class CastleBuilding : BuildingBase
    {
        [Header("Castle Population")]
        [Tooltip("Số lượng dân số tối đa mà Lâu đài cấp sẵn khi bắt đầu trận")]
        [SerializeField] private int castlePopulationProvided = 5;

        [Header("Resource Drop-Off Settings")]
        [Tooltip("Bán kính tương tác nộp tài nguyên cho Nông dân")]
        [SerializeField] private float dropOffRadius = 3.0f;

        [Tooltip("Vị trí điểm nộp tài nguyên cụ thể (nếu để trống sẽ lấy tâm công trình)")]
        [SerializeField] private Transform dropOffPoint;

        private CastleDepositArea _depositArea;
        private bool _populationGranted = false;

        public float DropOffRadius => dropOffRadius;

        protected override void Awake()
        {
            // Thiết lập giá trị mặc định cho Lâu đài nếu chưa tùy chỉnh trong Inspector
            buildingName = "Castle";
            if (maxHP <= 500) maxHP = 2500;
            buildingGridSize = new Vector2Int(3, 3);
            cameraShakeDuration = 0.5f;
            cameraShakeMagnitude = 0.35f;

            base.Awake();

            // Đảm bảo luôn có CastleDepositArea để WorkerFSM tìm kiếm
            _depositArea = GetComponent<CastleDepositArea>();
            if (_depositArea == null)
            {
                _depositArea = gameObject.AddComponent<CastleDepositArea>();
            }
        }

        protected override void Start()
        {
            base.Start();

            // Khởi tạo: Cấp sẵn +5 Sức chứa tối đa cho Player Blue
            GrantCastlePopulation();
        }

        /// <summary>
        /// Cộng thêm +5 Max Population cho người chơi.
        /// </summary>
        private void GrantCastlePopulation()
        {
            if (_populationGranted) return;

            if (IsPlayerFaction && PopulationManager.Instance != null)
            {
                PopulationManager.Instance.AddMaxPopulation(castlePopulationProvided);
                _populationGranted = true;
                Debug.Log($"<color=cyan>[CastleBuilding] Lâu đài khởi tạo: Đã cấp +{castlePopulationProvided} Sức chứa tối đa.</color>");
            }
        }

        /// <summary>
        /// Trả về tọa độ điểm nộp tài nguyên cho Nông dân.
        /// </summary>
        public Vector3 GetDropOffPosition()
        {
            if (dropOffPoint != null)
            {
                return dropOffPoint.position;
            }

            if (_depositArea != null)
            {
                return _depositArea.GetDropOffPosition();
            }

            return transform.position;
        }

        /// <summary>
        /// Tích hợp nhận diện Nông dân (Pawn) đi vào vùng nộp tài nguyên qua Trigger Collider2D.
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            WorkerFSM worker = other.GetComponent<WorkerFSM>();
            if (worker != null)
            {
                // Nông dân tiến vào vùng Castle, hỗ trợ xử lý nếu cần kích hoạt nhận tài nguyên sớm
                OnWorkerArrivedAtCastle(worker);
            }
        }

        /// <summary>
        /// Xử lý khi Nông dân mang tài nguyên về tới Lâu đài.
        /// </summary>
        public void OnWorkerArrivedAtCastle(WorkerFSM worker)
        {
            if (worker == null) return;
            // WorkerFSM đã có logic tự động nộp tài nguyên vào ResourceManager khi đến gần
        }

        /// <summary>
        /// Xử lý khi Lâu đài bị tiêu diệt (HP <= 0 hoặc bị phá).
        /// Kích hoạt Thua cuộc (Defeat) nếu là Lâu đài Player, hoặc Chiến thắng (Victory) nếu là Lâu đài Địch.
        /// </summary>
        protected override void OnDestroyed()
        {
            if (isDestroyed) return;

            RevokeCastlePopulation();

            // Điều kiện Thắng / Thua sống còn
            if (IsPlayerFaction)
            {
                Debug.Log("<color=red><b>[CastleBuilding] LÂU ĐÀI CỦA NGƯỜI CHƠI BỊ TIÊU DIỆT! THUA CUỘC!</b></color>");
                GameManager.TriggerPlayerDefeated();
            }
            else
            {
                Debug.Log("<color=green><b>[CastleBuilding] LÂU ĐÀI CỦA ĐỐI PHƯƠNG BỊ TIÊU DIỆT! CHIẾN THẮNG!</b></color>");
                GameManager.TriggerPlayerVictory();
            }

            base.OnDestroyed();
        }

        /// <summary>
        /// Trừ lại sức chứa khi Lâu đài bị phá hủy.
        /// </summary>
        private void RevokeCastlePopulation()
        {
            if (!_populationGranted) return;

            if (IsPlayerFaction && PopulationManager.Instance != null)
            {
                PopulationManager.Instance.RemoveMaxPopulation(castlePopulationProvided);
                _populationGranted = false;
            }
        }

        protected override void OnDestroy()
        {
            RevokeCastlePopulation();

            // Nếu Lâu đài Player bị xóa khỏi scene trước khi hết máu (ví dụ do Demolish)
            if (!isDestroyed && IsPlayerFaction)
            {
                GameManager.TriggerPlayerDefeated();
            }

            base.OnDestroy();
        }

        private void OnDrawGizmosSelected()
        {
            // Vẽ bán kính điểm nộp tài nguyên trong Editor
            Gizmos.color = Color.cyan;
            Vector3 center = dropOffPoint != null ? dropOffPoint.position : transform.position;
            Gizmos.DrawWireSphere(center, dropOffRadius);
        }
    }
}
