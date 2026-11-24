using UnityEngine;
using TinyTactics.FogOfWar;

namespace TinyTactics.Buildings
{
    /// <summary>
    /// Công trình Tháp canh (Tower) — Kế thừa BuildingBase.
    /// Có lượng máu cao và tích hợp VisionProvider cung cấp tầm nhìn rộng xuyên qua sương mù.
    /// </summary>
    [RequireComponent(typeof(VisionProvider))]
    public class TowerBuilding : BuildingBase
    {
        [Header("Tower Vision")]
        [Tooltip("Bán kính tầm nhìn của Tháp canh (mặc định 11f để phát hiện địch từ xa)")]
        [SerializeField, Min(1f)] private float _visionRadius = 11f;

        private VisionProvider _visionProvider;

        protected override void Awake()
        {
            buildingName = "Tower";
            if (maxHP <= 500) maxHP = 1000;
            buildingGridSize = new Vector2Int(2, 2);
            cameraShakeDuration = 0.25f;
            cameraShakeMagnitude = 0.15f;

            base.Awake();

            _visionProvider = GetComponent<VisionProvider>();
            if (_visionProvider == null)
            {
                _visionProvider = gameObject.AddComponent<VisionProvider>();
            }
        }

        protected override void Start()
        {
            base.Start();
            SyncTowerVision();
        }

        /// <summary>
        /// Đồng bộ phe sở hữu và bán kính tầm nhìn cho VisionProvider sau khi SyncWithBuildingInstance().
        /// </summary>
        private void SyncTowerVision()
        {
            if (_visionProvider == null)
                _visionProvider = GetComponent<VisionProvider>();

            if (_visionProvider != null)
            {
                _visionProvider.SetVision(TeamFaction, _visionRadius);
            }
        }
    }
}
