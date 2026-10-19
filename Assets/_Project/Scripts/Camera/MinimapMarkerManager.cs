
using System.Collections.Generic;
using UnityEngine;
using TinyTactics.CameraSystem;

namespace TinyTactics.UI
{
    /// <summary>
    /// Quản lý Minimap Marker bằng Object Pooling.
    /// Hiển thị Unit và Building theo màu phe.
    /// </summary>
    public class MinimapMarkerManager : MonoBehaviour
    {
        private static readonly HashSet<MinimapTarget> targets = new();

        [Header("References")]
        [SerializeField] private RTSCameraController cameraController;
        [SerializeField] private RectTransform markerContainer;
        [SerializeField] private MinimapMarker markerPrefab;

        [Header("Marker Sprites")]
        [SerializeField] private Sprite unitSprite;
        [SerializeField] private Sprite buildingSprite;

        [Header("Marker Sizes")]
        [SerializeField, Min(1f)] private float unitSize = 5f;
        [SerializeField, Min(1f)] private float buildingSize = 9f;

        [Header("Faction Colors")]
        [SerializeField] private Color blueColor = Color.cyan;
        [SerializeField] private Color redColor = Color.red;
        [SerializeField] private Color neutralColor = Color.white;

        [Header("Performance")]
        [SerializeField, Range(1f, 30f)]
        private float updateRate = 10f;

        private readonly Dictionary<MinimapTarget, MinimapMarker>
            activeMarkers = new();

        private readonly Stack<MinimapMarker> markerPool = new();

        private readonly List<MinimapTarget> removalBuffer = new();

        private float updateTimer;

        public static void RegisterTarget(MinimapTarget target)
        {
            if (target != null)
                targets.Add(target);
        }

        public static void UnregisterTarget(MinimapTarget target)
        {
            targets.Remove(target);
        }

        private void OnEnable()
        {
            updateTimer = 0f;
        }

        private void Update()
        {
            updateTimer -= Time.deltaTime;

            if (updateTimer > 0f)
                return;

            updateTimer = 1f / updateRate;
            UpdateMarkers();
        }

        private void UpdateMarkers()
        {
            if (cameraController == null ||
                markerContainer == null ||
                markerPrefab == null)
                return;

            Rect bounds = cameraController.MapBounds;

            if (bounds.width <= 0f || bounds.height <= 0f)
                return;

            removalBuffer.Clear();

            // Thu hồi Marker của Target không còn hợp lệ.
            foreach (var pair in activeMarkers)
            {
                if (pair.Key == null ||
                    !pair.Key.isActiveAndEnabled ||
                    !targets.Contains(pair.Key))
                {
                    removalBuffer.Add(pair.Key);
                }
            }

            foreach (MinimapTarget target in removalBuffer)
            {
                MinimapMarker marker = activeMarkers[target];

                activeMarkers.Remove(target);
                marker.gameObject.SetActive(false);
                markerPool.Push(marker);
            }

            // Tạo và cập nhật Marker.
            foreach (MinimapTarget target in targets)
            {
                if (target == null || !target.isActiveAndEnabled)
                    continue;

                if (!activeMarkers.TryGetValue(
                    target, out MinimapMarker marker))
                {
                    marker = GetMarker();

                    marker.Configure(
                        target.TargetType,
                        GetFactionColor(target.FactionId),
                        unitSprite,
                        buildingSprite,
                        unitSize,
                        buildingSize
                    );

                    activeMarkers.Add(target, marker);
                }

                Vector3 position = target.WorldPosition;

                Vector2 worldPosition =
                    new Vector2(position.x, position.y);

                bool insideMap = bounds.Contains(worldPosition);

                if (marker.gameObject.activeSelf != insideMap)
                    marker.gameObject.SetActive(insideMap);

                if (!insideMap)
                    continue;

                Vector2 normalizedPosition = new Vector2(
                    Mathf.Clamp01(
                        (position.x - bounds.xMin) / bounds.width
                    ),
                    Mathf.Clamp01(
                        (position.y - bounds.yMin) / bounds.height
                    )
                );

                marker.SetPosition(normalizedPosition);

                // Đồng bộ màu nếu FactionId thay đổi.
                marker.SetColor(GetFactionColor(target.FactionId));
            }
        }

        private MinimapMarker GetMarker()
        {
            MinimapMarker marker;

            if (markerPool.Count > 0)
            {
                marker = markerPool.Pop();
            }
            else
            {
                marker = Instantiate(markerPrefab, markerContainer);
            }

            return marker;
        }

        private Color GetFactionColor(string factionId)
        {
            switch (factionId)
            {
                case "Blue":
                    return blueColor;

                case "Red":
                    return redColor;

                default:
                    return neutralColor;
            }
        }

        private void OnDestroy()
        {
            targets.Clear();
            activeMarkers.Clear();
            markerPool.Clear();
        }
    }
}
