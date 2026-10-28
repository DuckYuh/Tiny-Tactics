using System;
using System.Collections;
using UnityEngine;
using TinyTactics.Pathfinding;
using TinyTactics.CameraSystem;

namespace TinyTactics.Buildings
{
    /// <summary>
    /// Lớp trừu tượng cơ sở cho mọi công trình trong game (Lâu đài, Nhà dân, Trại lính, Tháp canh,...).
    /// Quản lý máu (HP), kích thước ô lưới (Grid Size), điểm tập kết (Rally Point), nhận sát thương và giải phóng ô lưới khi bị phá hủy.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class BuildingBase : MonoBehaviour
    {
        [Header("Building Info")]
        [Tooltip("Tên định danh của công trình")]
        [SerializeField] protected string buildingName = "Building";

        [Tooltip("Phe sở hữu công trình (Blue: Player, Red: Enemy)")]
        [SerializeField] protected string factionId = "Blue";

        [Header("Health & Durability")]
        [Tooltip("Lượng máu tối đa")]
        [SerializeField] protected int maxHP = 500;

        [Tooltip("Lượng máu hiện tại")]
        [SerializeField] protected int currentHP;

        [Header("Grid Size")]
        [Tooltip("Kích thước công trình trên ma trận Pathfinding (x: chiều rộng, y: chiều cao)")]
        [SerializeField] protected Vector2Int buildingGridSize = new Vector2Int(2, 2);

        [Header("Rally Point")]
        [Tooltip("Điểm tập kết quân sau khi sinh ra từ công trình này")]
        [SerializeField] protected Transform rallyPoint;

        [Header("Destruction Effects")]
        [Tooltip("Thời gian rung Camera khi công trình sập")]
        [SerializeField] protected float cameraShakeDuration = 0.25f;

        [Tooltip("Biên độ rung Camera")]
        [SerializeField] protected float cameraShakeMagnitude = 0.15f;

        [Tooltip("Prefab hiệu ứng sập nhà / khói bụi đổ nát (nếu có)")]
        [SerializeField] protected GameObject destructionEffectPrefab;

        protected bool isDestroyed = false;
        protected bool isGridReleased = false;

        public string BuildingName => buildingName;
        public string FactionId => factionId;
        public int MaxHP => maxHP;
        public int CurrentHP => currentHP;
        public Vector2Int BuildingGridSize => buildingGridSize;
        public Transform RallyPoint => rallyPoint;
        public bool IsDestroyed => isDestroyed;
        public bool IsPlayerFaction => string.Equals(factionId, "Blue", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Sự kiện phát ra khi HP thay đổi (currentHP, maxHP) để cập nhật thanh máu UI.
        /// </summary>
        public event Action<int, int> OnHealthChanged;

        /// <summary>
        /// Sự kiện toàn cục phát ra khi bất kỳ công trình nào bị phá hủy.
        /// </summary>
        public static event Action<BuildingBase> OnAnyBuildingDestroyed;

        protected virtual void Awake()
        {
            if (currentHP <= 0)
            {
                currentHP = maxHP;
            }
        }

        protected virtual void Start()
        {
            SyncWithBuildingInstance();
        }

        /// <summary>
        /// Đồng bộ thông tin từ BuildingInstance và BuildingData nếu công trình được sinh ra từ BuildingPlacementService.
        /// </summary>
        protected virtual void SyncWithBuildingInstance()
        {
            BuildingInstance instance = GetComponent<BuildingInstance>();
            if (instance != null)
            {
                if (!string.IsNullOrEmpty(instance.FactionId))
                {
                    factionId = instance.FactionId;
                }

                if (instance.Data != null)
                {
                    if (string.IsNullOrEmpty(buildingName) || buildingName == "Building")
                    {
                        buildingName = instance.Data.BuildingName;
                    }

                    if (buildingGridSize == Vector2Int.zero || buildingGridSize == Vector2Int.one)
                    {
                        buildingGridSize = instance.Data.Footprint;
                    }
                }
            }
        }

        /// <summary>
        /// Nhận sát thương khi bị kẻ địch tấn công.
        /// </summary>
        /// <param name="damage">Lượng sát thương nhận vào.</param>
        public virtual void TakeDamage(int damage)
        {
            if (isDestroyed || damage <= 0) return;

            currentHP = Mathf.Clamp(currentHP - damage, 0, maxHP);
            Debug.Log($"[{buildingName} ({factionId})] Nhận {damage} sát thương. HP còn lại: {currentHP}/{maxHP}");

            OnHealthChanged?.Invoke(currentHP, maxHP);

            if (currentHP <= 0)
            {
                OnDestroyed();
            }
        }

        /// <summary>
        /// Hồi phục máu cho công trình (qua sửa chữa hoặc phép thuật).
        /// </summary>
        public virtual void Heal(int amount)
        {
            if (isDestroyed || amount <= 0) return;

            currentHP = Mathf.Clamp(currentHP + amount, 0, maxHP);
            OnHealthChanged?.Invoke(currentHP, maxHP);
        }

        /// <summary>
        /// Trả về vị trí tập kết quân (Rally Point).
        /// Nếu chưa gắn Transform thì tính toán vị trí nằm ngay mép dưới công trình.
        /// </summary>
        public virtual Vector3 GetRallyPointPosition()
        {
            if (rallyPoint != null)
            {
                return rallyPoint.position;
            }

            // Điểm tập kết mặc định: Phía trước (bên dưới) công trình
            return transform.position + new Vector3(0f, -buildingGridSize.y * 0.5f - 0.5f, 0f);
        }

        /// <summary>
        /// Thiết lập điểm tập kết mới (dành cho người chơi nhấp chuột phải chỉ định điểm rally).
        /// </summary>
        public virtual void SetRallyPoint(Vector3 newWorldPosition)
        {
            if (rallyPoint == null)
            {
                GameObject rallyObj = new GameObject($"{buildingName}_RallyPoint");
                rallyObj.transform.SetParent(transform);
                rallyPoint = rallyObj.transform;
            }

            rallyPoint.position = newWorldPosition;
            Debug.Log($"[{buildingName}] Đặt điểm tập kết mới tại {newWorldPosition}");
        }

        /// <summary>
        /// Xử lý logic khi công trình bị phá hủy (hết máu).
        /// Giải phóng ô lưới Pathfinding, kích hoạt hiệu ứng sập nhà và rung camera.
        /// </summary>
        protected virtual void OnDestroyed()
        {
            if (isDestroyed) return;
            isDestroyed = true;

            Debug.Log($"<color=orange>[BuildingBase] Công trình <b>{buildingName}</b> ({factionId}) đã bị phá hủy!</color>");

            OnAnyBuildingDestroyed?.Invoke(this);

            // 1. Mở lại ô lưới di chuyển trên PathfindingGrid
            ReleaseGridOccupancy();

            // 2. Kích hoạt hiệu ứng đổ nát và rung Camera
            TriggerCollapseEffect();

            // 3. Vô hiệu hóa va chạm và ẩn visual
            DisableBuildingInteractions();

            // 4. Hủy GameObject sau một khoảng thời gian ngắn để hiệu ứng hoàn tất
            Destroy(gameObject, 0.1f);
        }

        /// <summary>
        /// Mở lại các ô lưới mà công trình đã chiếm dụng để Unit có thể đi xuyên qua.
        /// </summary>
        public virtual void ReleaseGridOccupancy()
        {
            if (isGridReleased) return;
            isGridReleased = true;

            // Cách 1: Giải phóng thông qua BuildingOccupancyManager nếu đang sử dụng
            if (BuildingOccupancyManager.Instance != null)
            {
                BuildingOccupancyManager.Instance.Release(gameObject);
            }

            // Cách 2: Cập nhật trực tiếp trên PathfindingGrid để đảm bảo an toàn tuyệt đối
            if (PathfindingGrid.Instance != null)
            {
                ReleaseGridNodesDirectly();
            }
        }

        /// <summary>
        /// Cập nhật trực tiếp các node trên PathfindingGrid thành isWalkable = true.
        /// </summary>
        private void ReleaseGridNodesDirectly()
        {
            PathfindingGrid grid = PathfindingGrid.Instance;
            if (grid == null) return;

            float cellSize = grid.CellSize;

            // Tính toán vị trí ô dưới cùng bên trái của Footprint
            Vector3 lowerLeft = transform.position - new Vector3(
                (buildingGridSize.x - 1) * cellSize * 0.5f,
                (buildingGridSize.y - 1) * cellSize * 0.5f,
                0f);

            grid.GetGridCoordinates(lowerLeft, out int startX, out int startY);

            for (int x = 0; x < buildingGridSize.x; x++)
            {
                for (int y = 0; y < buildingGridSize.y; y++)
                {
                    PathNode node = grid.GetNode(startX + x, startY + y);
                    if (node != null)
                    {
                        grid.UpdateNodeWalkable(node.worldPosition, true);
                    }
                }
            }
        }

        /// <summary>
        /// Kích hoạt hiệu ứng hình ảnh sập nhà và rung màn hình.
        /// </summary>
        protected virtual void TriggerCollapseEffect()
        {
            // Sinh prefab hiệu ứng đổ nát nếu có
            if (destructionEffectPrefab != null)
            {
                Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
            }

            // Rung màn hình Camera
            TriggerCameraShake();
        }

        /// <summary>
        /// Kích hoạt rung camera khi công trình sụp đổ.
        /// </summary>
        protected virtual void TriggerCameraShake()
        {
            RTSCameraController camController = FindObjectOfType<RTSCameraController>();
            if (camController != null)
            {
                camController.Shake(cameraShakeDuration, cameraShakeMagnitude);
            }
        }

        /// <summary>
        /// Tắt các collider để không còn nhận click hoặc cản đường.
        /// </summary>
        protected virtual void DisableBuildingInteractions()
        {
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            // Ẩn selection visual nếu có
            BuildingSelectionVisual selectionVisual = GetComponent<BuildingSelectionVisual>();
            if (selectionVisual != null)
            {
                selectionVisual.SetHighlighted(false);
            }
        }

        protected virtual void OnDestroy()
        {
            // Đảm bảo ô lưới luôn được giải phóng kể cả khi bị Destroy bằng Demolish
            ReleaseGridOccupancy();
        }
    }
}
