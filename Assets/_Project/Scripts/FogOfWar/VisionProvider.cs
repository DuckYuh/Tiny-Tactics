using UnityEngine;
using TinyTactics.Buildings;
using TinyTactics.Combat;
using TinyTactics.Factions;

namespace TinyTactics.FogOfWar
{
    /// <summary>
    /// Cung cấp tầm nhìn mở Sương mù (Fog of War) cho Unit và Công trình (như Tower).
    /// Tự động đồng bộ bán kính tầm nhìn từ UnitDataSO và phe sở hữu từ HealthSystem/FactionVisual/BuildingBase.
    /// </summary>
    public class VisionProvider : MonoBehaviour
    {
        [Header("Vision Settings")]
        [Tooltip("Bán kính tầm nhìn (Unit tự đồng bộ từ UnitDataSO.VisionRange; Tower mặc định 11f)")]
        [SerializeField, Min(0f)] private float _visionRadius = 6f;

        [Tooltip("Phe mặc định nếu đối tượng chưa có HealthSystem/FactionVisual/BuildingBase")]
        [SerializeField] private Faction _ownerFaction = Faction.Blue;

        [Tooltip("Tự động lấy VisionRange từ UnitDataSO nếu gắn trên Unit")]
        [SerializeField] private bool _syncFromUnitData = true;

        private HealthSystem _healthSystem;
        private FactionVisual _factionVisual;
        private BuildingBase _buildingBase;
        private BuildingInstance _buildingInstance;
        private bool _hasExplicitOverride;

        public float VisionRadius
        {
            get => _visionRadius;
            set => _visionRadius = Mathf.Max(0f, value);
        }

        public Faction OwnerFaction
        {
            get
            {
                if (_hasExplicitOverride)
                    return _ownerFaction;

                if (_healthSystem != null)
                    return _healthSystem.TeamFaction;

                if (_factionVisual != null && !string.IsNullOrWhiteSpace(_factionVisual.FactionId))
                    return FactionUtility.FromString(_factionVisual.FactionId);

                if (_buildingInstance != null && _buildingInstance.IsInitialized)
                    return FactionUtility.FromString(_buildingInstance.FactionId);

                if (_buildingBase != null)
                    return _buildingBase.TeamFaction;

                return _ownerFaction;
            }
        }

        public bool IsAliveAndActive
        {
            get
            {
                if (!isActiveAndEnabled)
                    return false;

                if (_healthSystem != null && _healthSystem.IsDead)
                    return false;

                if (_buildingBase != null && _buildingBase.IsDestroyed)
                    return false;

                return true;
            }
        }

        private void Awake()
        {
            CacheOwnerComponents();
        }

        private void Start()
        {
            SyncVisionRadiusFromUnitData();
        }

        private void OnEnable()
        {
            CacheOwnerComponents();
            SyncVisionRadiusFromUnitData();
            FogOfWarManager.RegisterVisionProvider(this);
        }

        private void OnDisable()
        {
            FogOfWarManager.UnregisterVisionProvider(this);
        }

        /// <summary>
        /// Gán trực tiếp phe và bán kính tầm nhìn tại runtime (dùng cho TowerBuilding hoặc AI).
        /// </summary>
        public void SetVision(Faction faction, float radius)
        {
            _ownerFaction = faction;
            _hasExplicitOverride = true;
            _visionRadius = Mathf.Max(0f, radius);
        }

        private void CacheOwnerComponents()
        {
            if (_healthSystem == null)
                _healthSystem = GetComponent<HealthSystem>();

            if (_factionVisual == null)
                _factionVisual = GetComponent<FactionVisual>();

            if (_buildingBase == null)
                _buildingBase = GetComponent<BuildingBase>();

            if (_buildingInstance == null)
                _buildingInstance = GetComponent<BuildingInstance>();
        }

        private void SyncVisionRadiusFromUnitData()
        {
            if (!_syncFromUnitData || _healthSystem == null || _healthSystem.UnitData == null)
                return;

            if (_healthSystem.UnitData.VisionRange > 0f)
            {
                _visionRadius = _healthSystem.UnitData.VisionRange;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.85f, 1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, _visionRadius);
        }
#endif
    }
}
