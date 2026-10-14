using UnityEngine;
using TinyTactics.Pathfinding;

namespace TinyTactics.Buildings
{
    /// <summary>
    /// Kiểm tra vị trí xây công trình.
    ///
    /// Điều kiện:
    /// - BuildingData hợp lệ.
    /// - Footprint nằm trong Grid.
    /// - Không có ô Grid bị chặn.
    /// - Không có Unit trong footprint.
    ///
    /// Không thay đổi Grid hoặc tài nguyên.
    /// </summary>
    public static class BuildingPlacementValidator
    {
        // Tên Layer dành cho tất cả Unit của Blue và Red.
        private const string UnitLayerName = "Unit";

        // Cache LayerMask, tránh tra cứu tên Layer mỗi frame.
        private static readonly int UnitLayerMask =
            LayerMask.GetMask(UnitLayerName);

        /// <summary>
        /// Kiểm tra vị trí đặt nhà.
        ///
        /// anchorWorldPosition nằm trong ô dưới-trái của footprint.
        /// snappedPosition trả về tâm của công trình.
        /// </summary>
        public static bool TryValidatePlacement(
            BuildingData data,
            Vector3 anchorWorldPosition,
            out Vector3 snappedPosition,
            out string reason)
        {
            snappedPosition = anchorWorldPosition;
            reason = string.Empty;

            if (data == null || !data.IsValid())
            {
                reason = "BuildingData không hợp lệ.";
                return false;
            }

            PathfindingGrid grid = PathfindingGrid.Instance;

            if (grid == null)
            {
                reason = "Không tìm thấy PathfindingGrid.";
                return false;
            }

            if (UnitLayerMask == 0)
            {
                reason = "Chưa cấu hình Layer Unit.";
                return false;
            }

            grid.GetGridCoordinates(
                anchorWorldPosition,
                out int startX,
                out int startY);

            Vector2Int footprint = data.Footprint;

            // Bước 1: Kiểm tra toàn bộ các ô Grid.
            for (int x = 0; x < footprint.x; x++)
            {
                for (int y = 0; y < footprint.y; y++)
                {
                    PathNode node = grid.GetNode(
                        startX + x,
                        startY + y);

                    if (node == null)
                    {
                        reason = "Công trình nằm ngoài Grid.";
                        return false;
                    }

                    if (!node.isWalkable)
                    {
                        reason = "Vị trí bị vật cản hoặc nhà chiếm.";
                        return false;
                    }
                }
            }

            // Bước 2: Tính tâm footprint theo Grid.
            PathNode firstNode = grid.GetNode(startX, startY);

            float cellSize = grid.CellSize;

            snappedPosition = firstNode.worldPosition +
                new Vector3(
                    (footprint.x - 1) * cellSize * 0.5f,
                    (footprint.y - 1) * cellSize * 0.5f,
                    0f);

            // Bước 3: Tính kích thước vùng xây dựng.
            Vector2 footprintSize = new Vector2(
                footprint.x * cellSize,
                footprint.y * cellSize);

            // Thu nhỏ rất nhẹ để tránh trường hợp Collider
            // chỉ chạm đúng cạnh bên ngoài footprint.
            const float edgeInset = 0.02f;

            footprintSize.x = Mathf.Max(
                0.001f,
                footprintSize.x - edgeInset);

            footprintSize.y = Mathf.Max(
                0.001f,
                footprintSize.y - edgeInset);

            // Bước 4: Kiểm tra có Unit trong footprint không.
            Collider2D blockingUnit = Physics2D.OverlapBox(
                new Vector2(
                    snappedPosition.x,
                    snappedPosition.y),
                footprintSize,
                0f,
                UnitLayerMask);

            if (blockingUnit != null)
            {
                reason = "Có Unit đang đứng trong khu vực xây.";
                return false;
            }

            reason = "Vị trí hợp lệ.";
            return true;
        }
    }
}