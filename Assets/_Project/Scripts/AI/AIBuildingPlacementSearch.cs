
using UnityEngine;
using TinyTactics.Pathfinding;
using TinyTactics.Buildings;

namespace TinyTactics.AI
{
    /// <summary>
    /// Tìm vị trí hợp lệ để AI đặt công trình.
    /// Sử dụng Spiral Search trên PathfindingGrid.
    /// Không Instantiate hoặc tiêu hao tài nguyên.
    /// </summary>
    public static class AIBuildingPlacementSearch
    {
        /// <summary>
        /// Tìm vị trí đặt công trình gần căn cứ nhất.
        ///
        /// castlePosition: vị trí Castle của AI.
        /// buildingData: dữ liệu công trình cần đặt.
        /// maxSearchRadius: bán kính tìm kiếm tính theo ô Grid.
        /// anchorPosition: tọa độ góc dưới-trái của footprint.
        /// </summary>
        public static bool TryFindPlacement(
            Vector3 castlePosition,
            BuildingData buildingData,
            out Vector3 anchorPosition,
            int maxSearchRadius = 12)
        {
            anchorPosition = Vector3.zero;

            if (buildingData == null ||
                !buildingData.IsValid() ||
                maxSearchRadius <= 0)
            {
                return false;
            }

            PathfindingGrid grid = PathfindingGrid.Instance;

            if (grid == null)
                return false;

            // Chuyển vị trí Castle thành tọa độ Grid.
            grid.GetGridCoordinates(
                castlePosition,
                out int centerX,
                out int centerY);

            // Quét từng vòng vuông từ trong ra ngoài.
            for (int radius = 1;
                 radius <= maxSearchRadius;
                 radius++)
            {
                // Cạnh dưới và cạnh trên.
                for (int x = -radius; x <= radius; x++)
                {
                    if (TryCandidate(
                            centerX + x,
                            centerY - radius,
                            grid,
                            buildingData,
                            out anchorPosition))
                    {
                        return true;
                    }

                    if (TryCandidate(
                            centerX + x,
                            centerY + radius,
                            grid,
                            buildingData,
                            out anchorPosition))
                    {
                        return true;
                    }
                }

                // Cạnh trái và cạnh phải.
                // Bỏ qua hai góc vì đã kiểm tra ở trên.
                for (int y = -radius + 1;
                     y < radius;
                     y++)
                {
                    if (TryCandidate(
                            centerX - radius,
                            centerY + y,
                            grid,
                            buildingData,
                            out anchorPosition))
                    {
                        return true;
                    }

                    if (TryCandidate(
                            centerX + radius,
                            centerY + y,
                            grid,
                            buildingData,
                            out anchorPosition))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Kiểm tra một ô Grid có thể làm anchor xây dựng hay không.
        /// </summary>
        private static bool TryCandidate(
            int gridX,
            int gridY,
            PathfindingGrid grid,
            BuildingData buildingData,
            out Vector3 anchorPosition)
        {
            // GetWorldPosition trả về góc dưới-trái của ô.
            anchorPosition = grid.GetWorldPosition(
                gridX,
                gridY);

            return BuildingPlacementValidator.TryValidatePlacement(
                buildingData,
                anchorPosition,
                out _,
                out _);
        }
    }
}
