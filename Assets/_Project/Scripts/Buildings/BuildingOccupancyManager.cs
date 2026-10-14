using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Pathfinding;

namespace TinyTactics.Buildings
{
    /// <summary>
    /// Quản lý các ô Grid bị công trình chiếm.
    /// Đánh dấu ô không thể đi qua và giải phóng khi phá nhà.
    /// </summary>
    public class BuildingOccupancyManager : MonoBehaviour
    {
        public static BuildingOccupancyManager Instance { get; private set; }

        // Lưu các ô đã chiếm theo ID của từng công trình.
        private readonly Dictionary<int, List<Vector2Int>>
            occupiedBuildings = new Dictionary<int, List<Vector2Int>>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Đăng ký các ô Grid cho công trình.
        ///
        /// anchorWorldPosition: vị trí nằm trong ô dưới-trái
        /// của footprint.
        ///
        /// building: GameObject công trình đã được tạo.
        /// </summary>
        public bool TryOccupy(
            GameObject building,
            BuildingData data,
            Vector3 anchorWorldPosition)
        {
            if (building == null || data == null)
                return false;

            PathfindingGrid grid = PathfindingGrid.Instance;

            if (grid == null)
                return false;

            int buildingId = building.GetInstanceID();

            // Tránh đăng ký trùng một công trình.
            if (occupiedBuildings.ContainsKey(buildingId))
                return false;

            // Kiểm tra footprint trước khi thay đổi Grid.
            if (!BuildingPlacementValidator.TryValidatePlacement(
                    data,
                    anchorWorldPosition,
                    out _,
                    out _))
            {
                return false;
            }

            grid.GetGridCoordinates(
                anchorWorldPosition,
                out int startX,
                out int startY);

            Vector2Int footprint = data.Footprint;

            List<Vector2Int> cells =
                new List<Vector2Int>(footprint.x * footprint.y);

            // Đăng ký toàn bộ footprint.
            for (int x = 0; x < footprint.x; x++)
            {
                for (int y = 0; y < footprint.y; y++)
                {
                    int gridX = startX + x;
                    int gridY = startY + y;

                    PathNode node = grid.GetNode(gridX, gridY);

                    // Validator đã đảm bảo node tồn tại.
                    cells.Add(new Vector2Int(gridX, gridY));

                    // Chặn ô để hệ thống tìm đường tránh nhà.
                    grid.UpdateNodeWalkable(
                        node.worldPosition,
                        false);
                }
            }

            occupiedBuildings.Add(buildingId, cells);

            return true;
        }

        /// <summary>
        /// Giải phóng các ô khi công trình bị phá hủy.
        /// Phải gọi trước khi Destroy GameObject.
        /// </summary>
        public bool Release(GameObject building)
        {
            if (building == null)
                return false;

            int buildingId = building.GetInstanceID();

            if (!occupiedBuildings.TryGetValue(
                    buildingId,
                    out List<Vector2Int> cells))
            {
                return false;
            }

            PathfindingGrid grid = PathfindingGrid.Instance;

            if (grid == null)
                return false;

            foreach (Vector2Int cell in cells)
            {
                PathNode node = grid.GetNode(cell.x, cell.y);

                if (node != null)
                {
                    grid.UpdateNodeWalkable(
                        node.worldPosition,
                        true);
                }
            }

            occupiedBuildings.Remove(buildingId);

            return true;
        }

        /// <summary>
        /// Kiểm tra công trình đã được đăng ký hay chưa.
        /// </summary>
        public bool IsRegistered(GameObject building)
        {
            return building != null &&
                occupiedBuildings.ContainsKey(
                    building.GetInstanceID());
        }
    }
}