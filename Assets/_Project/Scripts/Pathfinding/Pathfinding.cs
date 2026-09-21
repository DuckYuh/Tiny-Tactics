using System.Collections.Generic;
using UnityEngine;

namespace TinyTactics.Pathfinding
{
    /// <summary>
    /// Chứa logic cốt lõi của thuật toán A* (A-Star) tìm đường đi ngắn nhất tránh vật cản trên 8 hướng.
    /// </summary>
    public class Pathfinding
    {
        // Hệ số chi phí dựa trên công thức Octile Distance
        private const int MOVE_STRAIGHT_COST = 10;
        private const int MOVE_DIAGONAL_COST = 14;

        /// <summary>
        /// Hàm trả về mảng danh sách các toạ độ Waypoint nếu tìm được đường, ngược lại trả về null.
        /// </summary>
        
        private static int globalRunId = 0;

        public List<Vector3> FindPath(Vector3 startWorldPos, Vector3 targetWorldPos)
        {
            PathfindingGrid grid = PathfindingGrid.Instance;
            PathNode startNode = grid.GetNodeFromWorldPosition(startWorldPos);
            PathNode targetNode = grid.GetNodeFromWorldPosition(targetWorldPos);

            if (startNode == null || targetNode == null)
            {
                return null;
            }

            // Nếu đích đến rơi vào vật cản, tự động tìm ô trống gần nhất thay thế
            if (!targetNode.isWalkable)
            {
                targetNode = grid.GetNearestWalkableNode(targetNode);
                if (targetNode == null)
                {
                    return null; // Bị bọc kín hoàn toàn không có ô trống nào xung quanh
                }
            }

            // Nếu đơn vị đã đứng ngay tại ô đích
            if (startNode == targetNode)
            {
                return new List<Vector3> { targetNode.worldPosition };
            }

            // Tăng runId, không cần for loop qua gridWidth * gridHeight nữa!
            globalRunId++;
            int currentRunId = globalRunId;

            List<PathNode> openList = new List<PathNode> { startNode };
            HashSet<PathNode> closedSet = new HashSet<PathNode>();

            startNode.ResetIfNewRun(currentRunId);
            startNode.gCost = 0;
            startNode.hCost = CalculateDistanceCost(startNode, targetNode);

            while (openList.Count > 0)
            {
                PathNode currentNode = GetLowestFCostNode(openList);

                if (currentNode == targetNode)
                {
                    return RetracePath(startNode, targetNode);
                }

                openList.Remove(currentNode);
                closedSet.Add(currentNode);

                foreach (PathNode neighborNode in GetNeighborList(currentNode))
                {
                    if (closedSet.Contains(neighborNode))
                        continue;

                    if (!neighborNode.isWalkable)
                    {
                        closedSet.Add(neighborNode);
                        continue;
                    }

                    if (IsDiagonal(currentNode, neighborNode))
                    {
                        if (!CanMoveDiagonally(currentNode, neighborNode))
                        {
                            continue;
                        }
                    }

                    // Reset thông số cho neighbor nếu đây là lần đầu node được chạm tới ở lượt này
                    neighborNode.ResetIfNewRun(currentRunId);

                    int tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNode, neighborNode);
                    if (tentativeGCost < neighborNode.gCost)
                    {
                        neighborNode.parent = currentNode;
                        neighborNode.gCost = tentativeGCost;
                        neighborNode.hCost = CalculateDistanceCost(neighborNode, targetNode);

                        if (!openList.Contains(neighborNode))
                        {
                            openList.Add(neighborNode);
                        }
                    }
                }
            }

            return null;
        }

        private List<PathNode> GetNeighborList(PathNode currentNode)
        {
            List<PathNode> neighborList = new List<PathNode>();
            PathfindingGrid grid = PathfindingGrid.Instance;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue; // Bỏ qua ô hiện tại ở tâm
                    
                    PathNode neighborNode = grid.GetNode(currentNode.x + x, currentNode.y + y);
                    if (neighborNode != null)
                    {
                        neighborList.Add(neighborNode);
                    }
                }
            }

            return neighborList;
        }

        // Kiểm tra xem nước đi sang ô hàng xóm có phải là đi chéo không
        private bool IsDiagonal(PathNode current, PathNode neighbor)
        {
            return current.x != neighbor.x && current.y != neighbor.y;
        }

        // Logic chống cắt góc: Nếu đi chéo, yêu cầu 2 ô kề cạnh thẳng phải không bị block
        private bool CanMoveDiagonally(PathNode current, PathNode neighbor)
        {
            PathfindingGrid grid = PathfindingGrid.Instance;
            PathNode adjacent1 = grid.GetNode(current.x, neighbor.y);
            PathNode adjacent2 = grid.GetNode(neighbor.x, current.y);

            bool isAdj1Walkable = adjacent1 != null && adjacent1.isWalkable;
            bool isAdj2Walkable = adjacent2 != null && adjacent2.isWalkable;

            return isAdj1Walkable && isAdj2Walkable;
        }

        // Dò ngược lại danh sách đường đi bằng thuộc tính parent
        private List<Vector3> RetracePath(PathNode startNode, PathNode endNode)
        {
            List<Vector3> path = new List<Vector3>();
            PathNode currentNode = endNode;

            while (currentNode != startNode)
            {
                // node.worldPosition đã là tâm của Grid Cell từ khi khởi tạo trong PathfindingGrid
                path.Add(currentNode.worldPosition);
                
                currentNode = currentNode.parent;
            }
            
            // Đảo ngược mảng vì chúng ta đang lội từ target về start
            path.Reverse();
            return path;
        }

        // Công thức Heuristic Octile Distance cho Grid 8 hướng
        private int CalculateDistanceCost(PathNode a, PathNode b)
        {
            int xDistance = Mathf.Abs(a.x - b.x);
            int yDistance = Mathf.Abs(a.y - b.y);
            int remaining = Mathf.Abs(xDistance - yDistance);
            
            return MOVE_DIAGONAL_COST * Mathf.Min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
        }

        // Tìm kiếm tuyến tính cơ bản để lấy Node rẻ nhất. 
        // LƯU Ý: Với game có nhiều quân, cần thay thế hàm này bằng cấu trúc Binary Min-Heap (Priority Queue) để đạt độ phức tạp O(logN).
        private PathNode GetLowestFCostNode(List<PathNode> pathNodeList)
        {
            PathNode lowestFCostNode = pathNodeList[0];
            for (int i = 1; i < pathNodeList.Count; i++)
            {
                if (pathNodeList[i].fCost < lowestFCostNode.fCost || 
                   (pathNodeList[i].fCost == lowestFCostNode.fCost && pathNodeList[i].hCost < lowestFCostNode.hCost))
                {
                    lowestFCostNode = pathNodeList[i];
                }
            }
            return lowestFCostNode;
        }
    }
}
