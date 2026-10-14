using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TinyTactics.Buildings;

namespace TinyTactics.UI
{
    /// <summary>
    /// Hiển thị thông tin công trình đang được chọn.
    /// Cập nhật bằng sự kiện, không cần Update().
    /// </summary>
    public sealed class BuildingSelectionUI : MonoBehaviour
    {
        private const string PlayerFactionId = "Blue";

        [Header("References")]
        [SerializeField]
        private BuildingDemolitionController demolitionController;

        [Header("UI Panel")]
        [SerializeField]
        private GameObject buildingInfoPanel;

        [Header("Building Information")]
        [SerializeField]
        private Image buildingIcon;

        [SerializeField]
        private TMP_Text buildingNameText;

        [SerializeField]
        private TMP_Text factionText;

        [Header("Actions")]
        [SerializeField]
        private Button demolishButton;

        private void OnEnable()
        {
            if (demolitionController != null)
            {
                demolitionController.OnSelectionChanged +=
                    HandleSelectionChanged;
            }

            if (demolishButton != null)
            {
                demolishButton.onClick.AddListener(
                    OnDemolishClicked);
            }

            // Đồng bộ trạng thái khi UI được bật.
            HandleSelectionChanged(
                demolitionController != null
                    ? demolitionController.SelectedBuilding
                    : null);
        }

        private void OnDisable()
        {
            if (demolitionController != null)
            {
                demolitionController.OnSelectionChanged -=
                    HandleSelectionChanged;
            }

            if (demolishButton != null)
            {
                demolishButton.onClick.RemoveListener(
                    OnDemolishClicked);
            }
        }

        /// <summary>
        /// Nhận sự kiện khi công trình được chọn hoặc bỏ chọn.
        /// </summary>
        private void HandleSelectionChanged(
            BuildingInstance building)
        {
            bool hasBuilding =
                building != null && building.IsInitialized;

            if (buildingInfoPanel != null)
            {
                buildingInfoPanel.SetActive(hasBuilding);
            }

            if (!hasBuilding)
                return;

            // Hiển thị tên công trình.
            if (buildingNameText != null)
            {
                buildingNameText.text =
                    building.Data != null
                        ? building.Data.BuildingName
                        : building.gameObject.name;
            }

            // Hiển thị phe sở hữu.
            if (factionText != null)
            {
                factionText.text =
                    $"Faction: {building.FactionId}";

                factionText.color =
                    building.FactionId == PlayerFactionId
                        ? new Color(0.4f, 0.7f, 1f)
                        : new Color(1f, 0.45f, 0.45f);
            }

            // Lấy Sprite đại diện từ công trình.
            // Lấy icon đại diện trực tiếp từ BuildingData.
            if (buildingIcon != null)
            {
                Sprite sprite = building.Data != null
                    ? building.Data.BuildingIcon
                    : null;

                buildingIcon.sprite = sprite;
                buildingIcon.enabled = sprite != null;

                // Giữ kích thước RectTransform đã thiết lập.
                buildingIcon.preserveAspect = true;
            }

            // Chỉ cho phép người chơi phá nhà Blue.
            if (demolishButton != null)
            {
                demolishButton.gameObject.SetActive(
                    building.FactionId == PlayerFactionId);
            }
        }

        /// <summary>
        /// Gọi chức năng phá nhà từ nút UI.
        /// </summary>
        private void OnDemolishClicked()
        {
            if (demolitionController == null)
                return;

            demolitionController.DemolishSelectedBuilding();
        }
    }
}