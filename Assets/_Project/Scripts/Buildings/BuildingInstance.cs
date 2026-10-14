using UnityEngine;

namespace TinyTactics.Buildings
{
    /// <summary>
    /// Quản lý thông tin và vòng đời của một công trình.
    /// Được khởi tạo bởi BuildingPlacementService.
    /// </summary>
    [DisallowMultipleComponent]
    public class BuildingInstance : MonoBehaviour
    {
        [Header("Runtime Information")]
        [SerializeField] private BuildingData buildingData;
        [SerializeField] private string factionId;

        private bool isInitialized;

        public BuildingData Data => buildingData;
        public string FactionId => factionId;
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// Khởi tạo công trình sau khi đăng ký Grid thành công.
        /// </summary>
        public void Initialize(
            BuildingData data,
            string ownerFactionId)
        {
            if (isInitialized)
            {
                Debug.LogWarning(
                    "[BuildingInstance] Công trình đã được khởi tạo.",
                    this);
                return;
            }

            if (data == null ||
                string.IsNullOrWhiteSpace(ownerFactionId))
            {
                Debug.LogError(
                    "[BuildingInstance] Dữ liệu khởi tạo không hợp lệ.",
                    this);
                return;
            }

            buildingData = data;
            factionId = ownerFactionId;
            isInitialized = true;
        }

        /// <summary>
        /// Tự giải phóng footprint khi công trình bị hủy.
        /// </summary>
        private void OnDestroy()
        {
            if (!isInitialized)
                return;

            BuildingOccupancyManager occupancy =
                BuildingOccupancyManager.Instance;

            if (occupancy == null)
                return;

            occupancy.Release(gameObject);
            isInitialized = false;
        }
    }
}