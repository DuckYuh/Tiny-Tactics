using UnityEngine;

namespace TinyTactics.Input
{
    /// <summary>
    /// Quản lý trạng thái Input đặc biệt của người chơi.
    /// Chưa cần MonoBehaviour hoặc GameObject trong Scene.
    /// </summary>
    public static class RTSInputMode
    {
        private static bool isBuildingPlacementActive;
        private static int lastExitFrame = -1;

        public static bool IsBuildingPlacementActive =>
            isBuildingPlacementActive;

        /// <summary>
        /// Chặn Input điều khiển quân trong chế độ xây.
        /// Giữ chặn đến hết frame hủy xây để tránh
        /// click phải vừa hủy preview vừa ra lệnh di chuyển.
        /// </summary>
        public static bool IsWorldInputBlocked =>
            isBuildingPlacementActive ||
            Time.frameCount == lastExitFrame;

        public static void EnterBuildingPlacement()
        {
            isBuildingPlacementActive = true;
        }

        public static void ExitBuildingPlacement()
        {
            if (!isBuildingPlacementActive)
                return;

            isBuildingPlacementActive = false;
            lastExitFrame = Time.frameCount;
        }
    }
}