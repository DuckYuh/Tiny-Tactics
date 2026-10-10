using TMPro;
using UnityEngine;
using TinyTactics.Economy;

namespace TinyTactics.UI
{
    /// <summary>
    /// Hiển thị tài nguyên của người chơi trên HUD.
    ///
    /// HUD cập nhật thông qua event OnResourceChanged
    /// thay vì kiểm tra ResourceManager mỗi frame.
    /// </summary>
    public class ResourceHUD : MonoBehaviour
    {
        [Header("Resource Amounts")]
        [SerializeField] private TMP_Text meatAmount;
        [SerializeField] private TMP_Text woodAmount;
        [SerializeField] private TMP_Text goldAmount;

        private void Start()
        {
            if (ResourceManager.Instance == null)
            {
                Debug.LogError(
                    "[ResourceHUD] Không tìm thấy ResourceManager."
                );

                return;
            }

            // Đăng ký nhận thông báo khi tài nguyên thay đổi.
            ResourceManager.Instance.OnResourceChanged += HandleResourceChanged;

            // Đồng bộ dữ liệu ban đầu.
            RefreshAllResources();
        }

        private void OnDestroy()
        {
            // Hủy đăng ký event khi HUD bị destroy.
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnResourceChanged -= HandleResourceChanged;
            }
        }

        /// <summary>
        /// Nhận thông báo từ ResourceManager và cập nhật đúng loại tài nguyên.
        /// </summary>
        private void HandleResourceChanged(ResourceType type, int amount)
        {
            switch (type)
            {
                case ResourceType.Meat:
                    UpdateAmount(meatAmount, amount);
                    break;

                case ResourceType.Wood:
                    UpdateAmount(woodAmount, amount);
                    break;

                case ResourceType.Gold:
                    UpdateAmount(goldAmount, amount);
                    break;
            }
        }

        /// <summary>
        /// Đồng bộ toàn bộ tài nguyên khi HUD khởi tạo.
        /// </summary>
        private void RefreshAllResources()
        {
            UpdateAmount(
                meatAmount,
                ResourceManager.Instance.GetResourceAmount(ResourceType.Meat)
            );

            UpdateAmount(
                woodAmount,
                ResourceManager.Instance.GetResourceAmount(ResourceType.Wood)
            );

            UpdateAmount(
                goldAmount,
                ResourceManager.Instance.GetResourceAmount(ResourceType.Gold)
            );
        }

        /// <summary>
        /// Cập nhật text tài nguyên.
        /// </summary>
        private void UpdateAmount(TMP_Text target, int amount)
        {
            if (target == null)
            {
                return;
            }

            target.text = amount.ToString();
        }
    }
}