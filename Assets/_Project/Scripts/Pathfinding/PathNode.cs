using UnityEngine;

namespace TinyTactics.Pathfinding
{
    public class PathNode
    {
        public int x;
        public int y;
        public Vector3 worldPosition;
        public bool isWalkable;

        public int gCost;
        public int hCost;
        public PathNode parent;

        // Đánh dấu node này đã được cập nhật ở lượt tìm đường nào (tối ưu tránh duyệt reset toàn map)
        public int lastRunId = -1;

        public int fCost => gCost + hCost;

        public PathNode(int x, int y, Vector3 worldPosition, bool isWalkable)
        {
            this.x = x;
            this.y = y;
            this.worldPosition = worldPosition;
            this.isWalkable = isWalkable;
        }

        public void ResetIfNewRun(int currentRunId)
        {
            if (lastRunId != currentRunId)
            {
                gCost = int.MaxValue;
                hCost = 0;
                parent = null;
                lastRunId = currentRunId;
            }
        }
    }
}