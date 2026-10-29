
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TinyTactics.Buildings;

namespace TinyTactics.UI
{
    /// <summary>
    /// Hiển thị thông tin công trình đang chọn.
    /// Castle Blue có thêm thông tin dân số.
    /// UI cập nhật thông qua Event, không cần Update().
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

        [Header("Castle Population")]
        [SerializeField]
        private GameObject castlePopulationPanel;

        [SerializeField]
        private TMP_Text populationText;

        [SerializeField]
        private Color normalPopulationColor = Color.white;

        [SerializeField]
        private Color fullPopulationColor = Color.red;

        [Header("Actions")]
        [SerializeField]
        private Button demolishButton;

        private BuildingInstance selectedBuilding;

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

            PopulationManager.OnPopulationChanged +=
                HandlePopulationChanged;

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

            PopulationManager.OnPopulationChanged -=
                HandlePopulationChanged;
        }

        /// <summary>
        /// Xử lý khi chọn hoặc bỏ chọn công trình.
        /// </summary>
        private void HandleSelectionChanged(
            BuildingInstance building)
        {
            selectedBuilding =
                building != null && building.IsInitialized
                    ? building
                    : null;

            bool hasBuilding = selectedBuilding != null;

            if (buildingInfoPanel != null)
                buildingInfoPanel.SetActive(hasBuilding);

            // Chỉ hiện Population khi chọn Castle Blue.
            bool isPlayerCastle =
                hasBuilding &&
                selectedBuilding.FactionId == PlayerFactionId &&
                selectedBuilding.GetComponent<CastleBuilding>() != null;

            if (castlePopulationPanel != null)
            {
                castlePopulationPanel.SetActive(isPlayerCastle);
            }

            if (!hasBuilding)
                return;

            if (buildingNameText != null)
            {
                buildingNameText.text =
                    selectedBuilding.Data != null
                        ? selectedBuilding.Data.BuildingName
                        : selectedBuilding.gameObject.name;
            }

            if (factionText != null)
            {
                factionText.text =
                    $"Faction: {selectedBuilding.FactionId}";

                factionText.color =
                    selectedBuilding.FactionId == PlayerFactionId
                        ? new Color(0.4f, 0.7f, 1f)
                        : new Color(1f, 0.45f, 0.45f);
            }

            if (buildingIcon != null)
            {
                Sprite sprite =
                    selectedBuilding.Data != null
                        ? selectedBuilding.Data.BuildingIcon
                        : null;

                buildingIcon.sprite = sprite;
                buildingIcon.enabled = sprite != null;
                buildingIcon.preserveAspect = true;
            }

            if (demolishButton != null)
            {
                demolishButton.gameObject.SetActive(
                    selectedBuilding.FactionId == PlayerFactionId);
            }

            // Đồng bộ ngay khi chọn Castle,
            // không phải chờ event dân số tiếp theo.
            if (isPlayerCastle)
                RefreshPopulation();
        }

        /// <summary>
        /// Nhận thay đổi dân số từ PopulationManager.
        /// </summary>
        private void HandlePopulationChanged(int current, int max)
        {
            if (selectedBuilding == null)
                return;

            if (selectedBuilding.FactionId != PlayerFactionId)
                return;

            if (selectedBuilding.GetComponent<CastleBuilding>() == null)
                return;

            UpdatePopulationText(current, max);
        }

        private void RefreshPopulation()
        {
            PopulationManager manager = PopulationManager.Instance;

            if (manager == null)
                return;

            UpdatePopulationText(
                manager.CurrentPopulation,
                manager.MaxPopulation);
        }

        private void UpdatePopulationText(int current, int max)
        {
            if (populationText == null)
                return;

            populationText.text = $"Population: {current}/{max}";

            populationText.color =
                current >= max
                    ? fullPopulationColor
                    : normalPopulationColor;
        }

        private void OnDemolishClicked()
        {
            if (demolitionController == null)
                return;

            demolitionController.DemolishSelectedBuilding();
        }
    }
}
