using UnityEngine;
using UnityEngine.UI;
using TinyTactics.CameraSystem;

namespace TinyTactics.UI
{
    /// <summary>
    /// Đồng bộ khung Viewport trên Minimap với Main Camera.
    /// Hỗ trợ di chuyển, zoom và giới hạn trong Map Bounds.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MinimapViewport : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RTSCameraController cameraController;
        [SerializeField] private RectTransform minimapDisplay;

        private RectTransform _viewportRect;

        private void Awake()
        {
            _viewportRect = GetComponent<RectTransform>();
        }

        private void LateUpdate()
        {
            UpdateViewport();
        }

        private void UpdateViewport()
        {
            if (cameraController == null || minimapDisplay == null)
                return;

            Camera mainCamera = cameraController.MainCamera;
            Rect mapBounds = cameraController.MapBounds;

            if (mainCamera == null ||
                mapBounds.width <= 0f ||
                mapBounds.height <= 0f)
                return;

            // Kích thước vùng nhìn trong World Space.
            float worldHeight = mainCamera.orthographicSize * 2f;
            float worldWidth = worldHeight * mainCamera.aspect;

            Vector3 cameraPosition = mainCamera.transform.position;

            // Giới hạn phần khung Camera vào Map Bounds.
            float left = Mathf.Clamp(
                cameraPosition.x - worldWidth * 0.5f,
                mapBounds.xMin,
                mapBounds.xMax
            );

            float right = Mathf.Clamp(
                cameraPosition.x + worldWidth * 0.5f,
                mapBounds.xMin,
                mapBounds.xMax
            );

            float bottom = Mathf.Clamp(
                cameraPosition.y - worldHeight * 0.5f,
                mapBounds.yMin,
                mapBounds.yMax
            );

            float top = Mathf.Clamp(
                cameraPosition.y + worldHeight * 0.5f,
                mapBounds.yMin,
                mapBounds.yMax
            );

            // Chuẩn hóa tọa độ về khoảng 0–1.
            Vector2 anchorMin = new Vector2(
                (left - mapBounds.xMin) / mapBounds.width,
                (bottom - mapBounds.yMin) / mapBounds.height
            );

            Vector2 anchorMax = new Vector2(
                (right - mapBounds.xMin) / mapBounds.width,
                (top - mapBounds.yMin) / mapBounds.height
            );

            // Đặt vị trí và kích thước khung bằng Anchors.
            _viewportRect.anchorMin = anchorMin;
            _viewportRect.anchorMax = anchorMax;

            _viewportRect.offsetMin = Vector2.zero;
            _viewportRect.offsetMax = Vector2.zero;
        }
    }
}