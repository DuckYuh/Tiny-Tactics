using UnityEngine;

namespace TinyTactics.Buildings
{
    /// <summary>
    /// Hiệu ứng highlight cho công trình được chọn.
    /// Lưu lại màu gốc để có thể khôi phục chính xác.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildingSelectionVisual : MonoBehaviour
    {
        [Header("Highlight")]
        [SerializeField]
        private Color highlightTint =
            new Color(0.6f, 1f, 0.6f, 1f);

        private SpriteRenderer[] spriteRenderers;
        private Color[] originalColors;

        private bool isHighlighted;

        private void Awake()
        {
            // Hỗ trợ prefab có nhiều SpriteRenderer ở các object con.
            spriteRenderers =
                GetComponentsInChildren<SpriteRenderer>(true);

            originalColors =
                new Color[spriteRenderers.Length];

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                originalColors[i] = spriteRenderers[i].color;
            }
        }

        /// <summary>
        /// Bật hoặc tắt highlight.
        /// Không tạo Material mới.
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (isHighlighted == highlighted)
                return;

            isHighlighted = highlighted;

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                SpriteRenderer renderer = spriteRenderers[i];

                if (renderer == null)
                    continue;

                if (highlighted)
                {
                    Color original = originalColors[i];

                    // Giữ lại Alpha của sprite.
                    renderer.color = new Color(
                        original.r * highlightTint.r,
                        original.g * highlightTint.g,
                        original.b * highlightTint.b,
                        original.a);
                }
                else
                {
                    renderer.color = originalColors[i];
                }
            }
        }

        private void OnDisable()
        {
            if (isHighlighted)
                SetHighlighted(false);
        }
    }
}