using UnityEngine;

namespace TinyTactics.Buildings
{
    /// <summary>
    /// Công trình Nhà dân (House) — Công trình dân sự tăng giới hạn dân số.
    /// - Khi xây dựng xong: Gọi PopulationManager.Instance.AddMaxPopulation(5).
    /// - Khi bị phá hủy: Lập tức gọi PopulationManager.Instance.RemoveMaxPopulation(5) và giải phóng ô Grid.
    /// </summary>
    public class HouseBuilding : BuildingBase
    {
        [Header("House Population")]
        [Tooltip("Lượng sức chứa dân số cộng thêm cho người chơi")]
        [SerializeField] private int populationBonus = 5;

        private bool _populationAdded = false;

        protected override void Awake()
        {
            // Thiết lập giá trị mặc định cho Nhà dân
            buildingName = "House";
            if (maxHP <= 500) maxHP = 600;
            buildingGridSize = new Vector2Int(2, 2);
            cameraShakeDuration = 0.2f;
            cameraShakeMagnitude = 0.15f;

            base.Awake();
        }

        protected override void Start()
        {
            base.Start();

            // Khi hoàn thành xây dựng -> Tăng sức chứa tối đa cho người chơi
            AddHousePopulation();
        }

        /// <summary>
        /// Cộng thêm +5 Max Population khi nhà dân hoàn thành.
        /// </summary>
        private void AddHousePopulation()
        {
            if (_populationAdded) return;

            if (IsPlayerFaction && PopulationManager.Instance != null)
            {
                PopulationManager.Instance.AddMaxPopulation(populationBonus);
                _populationAdded = true;
                Debug.Log($"<color=green>[HouseBuilding] Nhà dân hoàn thành: Cấp thêm +{populationBonus} Sức chứa tối đa.</color>");
            }
        }

        /// <summary>
        /// Khi Nhà dân bị phá hủy (hết máu hoặc bị phá), trừ ngay 5 Max Population và giải phóng ô Grid.
        /// </summary>
        protected override void OnDestroyed()
        {
            if (isDestroyed) return;

            // 1. Trừ ngay 5 Max Population nếu thuộc phe người chơi
            RemoveHousePopulation();

            // 2. Gọi logic cơ sở: Giải phóng ô Grid, hiệu ứng sập nhà và rung camera
            base.OnDestroyed();
        }

        /// <summary>
        /// Trừ đi sức chứa tối đa khi nhà dân bị phá hủy.
        /// </summary>
        private void RemoveHousePopulation()
        {
            if (!_populationAdded) return;

            if (IsPlayerFaction && PopulationManager.Instance != null)
            {
                PopulationManager.Instance.RemoveMaxPopulation(populationBonus);
                _populationAdded = false;
                Debug.Log($"<color=yellow>[HouseBuilding] Nhà dân bị phá hủy: Đã trừ -{populationBonus} Sức chứa tối đa.</color>");
            }
        }

        protected override void OnDestroy()
        {
            // Đảm bảo nếu bị Destroy thông qua Demolish hoặc hủy trực tiếp, dân số vẫn được trừ chính xác
            RemoveHousePopulation();

            base.OnDestroy();
        }
    }
}
