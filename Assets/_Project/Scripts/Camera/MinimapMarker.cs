
using UnityEngine;
using UnityEngine.UI;

namespace TinyTactics.UI
{
    /// <summary>
    /// Hiển thị Marker UI trên Minimap.
    /// Hỗ trợ thay đổi hình dạng, kích thước và màu sắc.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class MinimapMarker : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Image markerImage;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            markerImage = GetComponent<Image>();

            markerImage.raycastTarget = false;
        }

        /// <summary>
        /// Cấu hình Marker theo loại đối tượng.
        /// </summary>
        public void Configure(
            MinimapTargetType targetType,
            Color color,
            Sprite unitSprite,
            Sprite buildingSprite,
            float unitSize,
            float buildingSize)
        {
            bool isBuilding =
                targetType == MinimapTargetType.Building;

            markerImage.sprite = isBuilding
                ? buildingSprite
                : unitSprite;

            markerImage.type = Image.Type.Simple;
            markerImage.color = color;
            markerImage.raycastTarget = false;

            float size = isBuilding
                ? buildingSize
                : unitSize;

            rectTransform.sizeDelta = new Vector2(size, size);
        }

        /// <summary>
        /// Đặt Marker tại tọa độ chuẩn hóa 0-1.
        /// </summary>
        public void SetPosition(Vector2 normalizedPosition)
        {
            rectTransform.anchorMin = normalizedPosition;
            rectTransform.anchorMax = normalizedPosition;
            rectTransform.anchoredPosition = Vector2.zero;
        }


        /// <summary>
        /// Cập nhật màu Marker khi phe của Target thay đổi.
        /// </summary>
        public void SetColor(Color color)
        {
            if (markerImage.color != color)
            {
                markerImage.color = color;
            }
        }

    }
}
