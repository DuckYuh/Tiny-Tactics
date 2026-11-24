using System;
using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Combat;
using TinyTactics.Pathfinding;

namespace TinyTactics.FogOfWar
{
    /// <summary>
    /// Quản lý Sương mù chiến tranh (Fog of War) theo 3 trạng thái:
    /// 0 = Unexplored (Đen), 1 = Explored (Xám mờ), 2 = Visible (Sáng rõ).
    /// Đồng bộ lưới với PathfindingGrid và cập nhật Texture2D định kỳ để tối ưu hiệu năng.
    /// </summary>
    public class FogOfWarManager : MonoBehaviour
    {
        public const byte StateUnexplored = 0;
        public const byte StateExplored = 1;
        public const byte StateVisible = 2;

        private static readonly HashSet<VisionProvider> _providers = new HashSet<VisionProvider>();

        public static FogOfWarManager Instance { get; private set; }

        public static event Action OnFogUpdated;

        [Header("Player & Timing")]
        [SerializeField] private Faction _localPlayerFaction = Faction.Blue;

        [Tooltip("Chu kỳ cập nhật tầm nhìn (0.1s = 10 lần/giây)")]
        [SerializeField, Min(0.02f)] private float _updateInterval = 0.1f;

        [Header("Fog Colors")]
        [SerializeField] private Color _unexploredColor = new Color(0f, 0f, 0f, 1f);
        [SerializeField] private Color _exploredColor = new Color(0f, 0f, 0f, 0.55f);
        [SerializeField] private Color _visibleColor = new Color(0f, 0f, 0f, 0f);

        [Header("Renderer Settings")]
        [SerializeField] private SpriteRenderer _fogSpriteRenderer;
        [SerializeField] private FilterMode _filterMode = FilterMode.Bilinear;
        [SerializeField] private int _sortingOrder = 50;
        [SerializeField] private string _fogLayerName = "FogOfWar";

        private int _gridWidth;
        private int _gridHeight;
        private float _cellSize = 1f;
        private Vector3 _originPosition;

        private byte[] _fogStates;
        private Color32[] _pixelColors;
        private Texture2D _fogTexture;
        private Sprite _fogSprite;

        private Color32 _unexploredColor32;
        private Color32 _exploredColor32;
        private Color32 _visibleColor32;

        private float _updateTimer;
        private bool _isInitialized;

        public Faction LocalPlayerFaction => _localPlayerFaction;
        public bool IsInitialized => _isInitialized;

        public static void RegisterVisionProvider(VisionProvider provider)
        {
            if (provider != null)
            {
                _providers.Add(provider);
            }
        }

        public static void UnregisterVisionProvider(VisionProvider provider)
        {
            if (provider != null)
            {
                _providers.Remove(provider);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            InitializeFogOfWar();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                _providers.Clear();
            }

            if (_fogSprite != null)
            {
                Destroy(_fogSprite);
            }

            if (_fogTexture != null)
            {
                Destroy(_fogTexture);
            }
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                if (PathfindingGrid.Instance != null)
                {
                    InitializeFogOfWar();
                }

                return;
            }

            _updateTimer -= Time.deltaTime;
            if (_updateTimer > 0f)
                return;

            _updateTimer = _updateInterval;
            UpdateFogOfWar();
        }

        private void InitializeFogOfWar()
        {
            if (_isInitialized)
                return;

            PathfindingGrid grid = PathfindingGrid.Instance;
            if (grid == null)
            {
                Debug.LogWarning("[FogOfWarManager] Không tìm thấy PathfindingGrid.Instance.");
                return;
            }

            _gridWidth = Mathf.Max(1, grid.GridWidth);
            _gridHeight = Mathf.Max(1, grid.GridHeight);
            _cellSize = Mathf.Max(0.01f, grid.CellSize);
            _originPosition = grid.OriginPosition;

            int totalCells = _gridWidth * _gridHeight;
            _fogStates = new byte[totalCells];
            _pixelColors = new Color32[totalCells];

            _unexploredColor32 = _unexploredColor;
            _exploredColor32 = _exploredColor;
            _visibleColor32 = _visibleColor;

            _fogTexture = new Texture2D(_gridWidth, _gridHeight, TextureFormat.RGBA32, false)
            {
                filterMode = _filterMode,
                wrapMode = TextureWrapMode.Clamp,
                name = "FogOfWarTexture"
            };

            _fogSprite = Sprite.Create(
                _fogTexture,
                new Rect(0f, 0f, _gridWidth, _gridHeight),
                Vector2.zero,
                1f);

            SetupFogRenderer();

            _isInitialized = true;
            _updateTimer = 0f;
            UpdateFogOfWar();
        }

        private void SetupFogRenderer()
        {
            if (_fogSpriteRenderer == null)
            {
                GameObject overlayObj = new GameObject("FogOfWarOverlay");
                overlayObj.transform.SetParent(transform, false);
                _fogSpriteRenderer = overlayObj.AddComponent<SpriteRenderer>();
            }

            int fogLayer = LayerMask.NameToLayer(_fogLayerName);
            if (fogLayer >= 0)
            {
                _fogSpriteRenderer.gameObject.layer = fogLayer;
            }

            _fogSpriteRenderer.sprite = _fogSprite;
            _fogSpriteRenderer.sortingOrder = _sortingOrder;
            _fogSpriteRenderer.transform.position = _originPosition;
            _fogSpriteRenderer.transform.localScale = new Vector3(_cellSize, _cellSize, 1f);
        }

        /// <summary>
        /// Chuyển ô Visible cũ sang Explored, mở vùng sáng cho các VisionProvider còn sống và cập nhật Texture.
        /// </summary>
        public void UpdateFogOfWar()
        {
            if (!_isInitialized)
                return;

            int totalCells = _fogStates.Length;
            for (int i = 0; i < totalCells; i++)
            {
                if (_fogStates[i] == StateVisible)
                {
                    _fogStates[i] = StateExplored;
                }
            }

            foreach (VisionProvider provider in _providers)
            {
                if (provider == null || !provider.IsAliveAndActive)
                    continue;

                if (provider.OwnerFaction != _localPlayerFaction)
                    continue;

                float radius = provider.VisionRadius;
                if (radius <= 0f)
                    continue;

                RevealCircle(provider.transform.position, radius);
            }

            for (int i = 0; i < totalCells; i++)
            {
                switch (_fogStates[i])
                {
                    case StateVisible:
                        _pixelColors[i] = _visibleColor32;
                        break;
                    case StateExplored:
                        _pixelColors[i] = _exploredColor32;
                        break;
                    default:
                        _pixelColors[i] = _unexploredColor32;
                        break;
                }
            }

            _fogTexture.SetPixels32(_pixelColors);
            _fogTexture.Apply(false);

            OnFogUpdated?.Invoke();
        }

        private void RevealCircle(Vector3 worldCenter, float radius)
        {
            int centerX = Mathf.FloorToInt((worldCenter.x - _originPosition.x) / _cellSize);
            int centerY = Mathf.FloorToInt((worldCenter.y - _originPosition.y) / _cellSize);
            int cellRadius = Mathf.CeilToInt(radius / _cellSize);

            int minX = Mathf.Max(0, centerX - cellRadius);
            int maxX = Mathf.Min(_gridWidth - 1, centerX + cellRadius);
            int minY = Mathf.Max(0, centerY - cellRadius);
            int maxY = Mathf.Min(_gridHeight - 1, centerY + cellRadius);

            float sqrRadius = radius * radius;
            float halfCell = _cellSize * 0.5f;

            for (int y = minY; y <= maxY; y++)
            {
                float cellCenterY = _originPosition.y + y * _cellSize + halfCell;
                float dy = cellCenterY - worldCenter.y;
                float dySqr = dy * dy;
                int rowOffset = y * _gridWidth;

                for (int x = minX; x <= maxX; x++)
                {
                    float cellCenterX = _originPosition.x + x * _cellSize + halfCell;
                    float dx = cellCenterX - worldCenter.x;

                    if (dx * dx + dySqr <= sqrRadius)
                    {
                        _fogStates[rowOffset + x] = StateVisible;
                    }
                }
            }
        }

        /// <summary>
        /// Trả về true nếu ô tại tọa độ worldPos đang nằm trong vùng sáng (Visible = 2).
        /// </summary>
        public bool IsPositionVisible(Vector3 worldPos)
        {
            return GetCellStateAtWorldPosition(worldPos) == StateVisible;
        }

        /// <summary>
        /// Trả về true nếu ô tại tọa độ worldPos đã từng được khám phá (Explored hoặc Visible).
        /// </summary>
        public bool IsPositionExplored(Vector3 worldPos)
        {
            return GetCellStateAtWorldPosition(worldPos) >= StateExplored;
        }

        public byte GetCellStateAtWorldPosition(Vector3 worldPos)
        {
            if (!_isInitialized)
                return StateVisible;

            int x = Mathf.FloorToInt((worldPos.x - _originPosition.x) / _cellSize);
            int y = Mathf.FloorToInt((worldPos.y - _originPosition.y) / _cellSize);

            if (x < 0 || y < 0 || x >= _gridWidth || y >= _gridHeight)
                return StateUnexplored;

            return _fogStates[y * _gridWidth + x];
        }
    }
}
