using UnityEngine;
using TinyTactics.Buildings;
using TinyTactics.Combat;
using TinyTactics.Factions;
using TinyTactics.UI;

namespace TinyTactics.FogOfWar
{
    /// <summary>
    /// Ẩn/hiện hình ảnh (SpriteRenderer, Canvas thanh máu và MinimapTarget tùy chọn)
    /// của đối tượng phe địch dựa vào trạng thái sương mù tại vị trí hiện tại.
    /// An toàn khi gắn lên Prefab dùng chung: tự bỏ qua nếu thuộc phe Local Player.
    /// </summary>
    public class FogVisibilityTarget : MonoBehaviour
    {
        [Header("Visibility Rules")]
        [Tooltip("Nếu bật (phù hợp với Công trình), vẫn hiển thị khi ô đã được khám phá (vùng xám Explored)")]
        [SerializeField] private bool _visibleInExploredFog = false;

        [Tooltip("Ẩn chấm đỏ trên Minimap khi Unit địch đứng trong sương mù")]
        [SerializeField] private bool _hideMinimapMarkerInFog = true;

        [Header("Optional Extra Visuals")]
        [SerializeField] private GameObject[] _extraVisualObjects;

        private SpriteRenderer[] _spriteRenderers;
        private Canvas[] _worldCanvases;
        private MinimapTarget _minimapTarget;
        private HealthSystem _healthSystem;
        private FactionVisual _factionVisual;
        private BuildingBase _buildingBase;

        private bool _isCurrentlyVisible = true;

        private void Awake()
        {
            CacheComponents();
        }

        private void OnEnable()
        {
            CacheComponents();
            FogOfWarManager.OnFogUpdated += RefreshVisibility;
            RefreshVisibility();
        }

        private void OnDisable()
        {
            FogOfWarManager.OnFogUpdated -= RefreshVisibility;
            SetVisualsVisible(true);
        }

        private void CacheComponents()
        {
            if (_spriteRenderers == null || _spriteRenderers.Length == 0)
                _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

            if (_worldCanvases == null || _worldCanvases.Length == 0)
                _worldCanvases = GetComponentsInChildren<Canvas>(true);

            if (_minimapTarget == null)
                _minimapTarget = GetComponent<MinimapTarget>();

            if (_healthSystem == null)
                _healthSystem = GetComponent<HealthSystem>();

            if (_factionVisual == null)
                _factionVisual = GetComponent<FactionVisual>();

            if (_buildingBase == null)
                _buildingBase = GetComponent<BuildingBase>();
        }

        public void RefreshVisibility()
        {
            FogOfWarManager fogManager = FogOfWarManager.Instance;
            if (fogManager == null || !fogManager.IsInitialized)
            {
                SetVisualsVisible(true);
                return;
            }

            if (GetOwnerFaction() == fogManager.LocalPlayerFaction)
            {
                SetVisualsVisible(true);
                return;
            }

            bool shouldShow = _visibleInExploredFog
                ? fogManager.IsPositionExplored(transform.position)
                : fogManager.IsPositionVisible(transform.position);

            SetVisualsVisible(shouldShow);
        }

        private Faction GetOwnerFaction()
        {
            if (_healthSystem != null)
                return _healthSystem.TeamFaction;

            if (_factionVisual != null && !string.IsNullOrWhiteSpace(_factionVisual.FactionId))
                return FactionUtility.FromString(_factionVisual.FactionId);

            if (_buildingBase != null)
                return _buildingBase.TeamFaction;

            return Faction.Red;
        }

        private void SetVisualsVisible(bool visible)
        {
            if (_isCurrentlyVisible == visible)
                return;

            _isCurrentlyVisible = visible;

            if (_spriteRenderers != null)
            {
                for (int i = 0; i < _spriteRenderers.Length; i++)
                {
                    if (_spriteRenderers[i] != null)
                    {
                        _spriteRenderers[i].enabled = visible;
                    }
                }
            }

            if (_worldCanvases != null)
            {
                for (int i = 0; i < _worldCanvases.Length; i++)
                {
                    if (_worldCanvases[i] != null)
                    {
                        _worldCanvases[i].enabled = visible;
                    }
                }
            }

            if (_extraVisualObjects != null)
            {
                for (int i = 0; i < _extraVisualObjects.Length; i++)
                {
                    if (_extraVisualObjects[i] != null)
                    {
                        _extraVisualObjects[i].SetActive(visible);
                    }
                }
            }

            if (_hideMinimapMarkerInFog && _minimapTarget != null)
            {
                _minimapTarget.enabled = visible;
            }
        }
    }
}
