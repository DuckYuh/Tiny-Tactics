using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TinyTactics.CameraSystem;

namespace TinyTactics.UI
{
    /// <summary>
    /// Xử lý tương tác Minimap:
    /// - Click để di chuyển Main Camera.
    /// - Kéo chuột để điều khiển Camera liên tục.
    /// - Chuyển đổi tọa độ UI sang World Space.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class MinimapController : MonoBehaviour,
        IPointerDownHandler,
        IDragHandler
    {
        [Header("References")]
        [SerializeField]
        private RTSCameraController cameraController;

        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            MoveCamera(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            MoveCamera(eventData);
        }

        /// <summary>
        /// Chuyển tọa độ chuột trên Minimap thành World Position.
        /// </summary>
        private void MoveCamera(PointerEventData eventData)
        {
            if (cameraController == null)
                return;

            // Chỉ xử lý chuột trái.
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            // Lấy tọa độ chuột tương đối với RectTransform.
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                return;
            }

            Rect rect = _rectTransform.rect;

            // Chuyển từ Local Position sang tọa độ chuẩn hóa 0–1.
            float normalizedX = Mathf.InverseLerp(
                rect.xMin,
                rect.xMax,
                localPoint.x
            );

            float normalizedY = Mathf.InverseLerp(
                rect.yMin,
                rect.yMax,
                localPoint.y
            );

            // Giới hạn tọa độ trong Minimap.
            normalizedX = Mathf.Clamp01(normalizedX);
            normalizedY = Mathf.Clamp01(normalizedY);

            // Quy đổi sang tọa độ thực của Map.
            Rect bounds = cameraController.MapBounds;

            Vector2 worldPosition = new Vector2(
                Mathf.Lerp(bounds.xMin, bounds.xMax, normalizedX),
                Mathf.Lerp(bounds.yMin, bounds.yMax, normalizedY)
            );

            // Điều khiển Main Camera.
            cameraController.MoveToPosition(worldPosition);
        }
    }
}