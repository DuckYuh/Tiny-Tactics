
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TinyTactics.Buildings;
using TinyTactics.Units;

namespace TinyTactics.UI
{
    /// <summary>
    /// Điều khiển các nút huấn luyện quân trong CommandHUD.
    /// Việc bật/tắt panel thuộc trách nhiệm BuildingMenuUI.
    /// </summary>
    public sealed class BarracksTrainingUI : MonoBehaviour
    {
        private const string PlayerFactionId = "Blue";

        [Header("References")]
        [SerializeField]
        private BuildingDemolitionController demolitionController;

        [Header("Training Buttons")]
        [SerializeField] private Button warriorButton;
        [SerializeField] private Button archerButton;
        [SerializeField] private Button lancerButton;

        [Header("Unit Icons")]
        [SerializeField] private Image warriorIcon;
        [SerializeField] private Image archerIcon;
        [SerializeField] private Image lancerIcon;

        [Header("Unit Names")]
        [SerializeField] private TMP_Text warriorName;
        [SerializeField] private TMP_Text archerName;
        [SerializeField] private TMP_Text lancerName;

        [Header("Unit Costs")]
        [SerializeField] private TMP_Text warriorCost;
        [SerializeField] private TMP_Text archerCost;
        [SerializeField] private TMP_Text lancerCost;

        private BarracksBuilding selectedBarracks;

        private void OnEnable()
        {
            if (demolitionController != null)
            {
                demolitionController.OnSelectionChanged +=
                    HandleSelectionChanged;

                HandleSelectionChanged(
                    demolitionController.SelectedBuilding);
            }

            if (warriorButton != null)
                warriorButton.onClick.AddListener(TrainWarrior);

            if (archerButton != null)
                archerButton.onClick.AddListener(TrainArcher);

            if (lancerButton != null)
                lancerButton.onClick.AddListener(TrainLancer);
        }

        private void OnDisable()
        {
            if (demolitionController != null)
            {
                demolitionController.OnSelectionChanged -=
                    HandleSelectionChanged;
            }

            if (warriorButton != null)
                warriorButton.onClick.RemoveListener(TrainWarrior);

            if (archerButton != null)
                archerButton.onClick.RemoveListener(TrainArcher);

            if (lancerButton != null)
                lancerButton.onClick.RemoveListener(TrainLancer);

            selectedBarracks = null;
        }

        private void HandleSelectionChanged(
            BuildingInstance building)
        {
            selectedBarracks = null;

            if (building != null &&
                building.IsInitialized &&
                building.FactionId == PlayerFactionId)
            {
                selectedBarracks =
                    building.GetComponent<BarracksBuilding>();
            }

            RefreshTrainingButtons();
        }

        private void RefreshTrainingButtons()
        {
            var units = selectedBarracks != null
                ? selectedBarracks.TrainableUnits
                : null;

            SetupButton(
                warriorButton, warriorIcon,
                warriorName, warriorCost,
                units != null && units.Count > 0
                    ? units[0] : null);

            SetupButton(
                archerButton, archerIcon,
                archerName, archerCost,
                units != null && units.Count > 1
                    ? units[1] : null);

            SetupButton(
                lancerButton, lancerIcon,
                lancerName, lancerCost,
                units != null && units.Count > 2
                    ? units[2] : null);
        }

        private void SetupButton(
            Button button,
            Image icon,
            TMP_Text nameText,
            TMP_Text costText,
            UnitDataSO data)
        {
            bool valid = data != null;

            if (button != null)
                button.interactable = valid;

            if (icon != null)
            {
                icon.sprite = valid ? data.UnitIcon : null;
                icon.enabled = valid && data.UnitIcon != null;
                icon.preserveAspect = true;
            }

            if (nameText != null)
                nameText.text = valid ? data.UnitName : "-";

            if (costText != null)
            {
                costText.text = valid
                    ? $"M:{data.MeatCost} " +
                      $"W:{data.WoodCost} " +
                      $"G:{data.GoldCost}"
                    : "-";
            }
        }

        private void TrainWarrior() => TrainUnit(0);
        private void TrainArcher() => TrainUnit(1);
        private void TrainLancer() => TrainUnit(2);

        private void TrainUnit(int index)
        {
            if (selectedBarracks == null)
                return;

            // Chặn gửi lệnh nếu Barracks không còn được chọn.
            if (demolitionController == null ||
                demolitionController.SelectedBuilding == null ||
                demolitionController.SelectedBuilding.gameObject !=
                selectedBarracks.gameObject)
                return;

            var units = selectedBarracks.TrainableUnits;

            if (index < 0 || index >= units.Count)
                return;

            UnitDataSO unit = units[index];

            if (unit == null)
                return;

            if (!selectedBarracks.TryQueueUnit(unit))
            {
                Debug.LogWarning(
                    $"[BarracksTrainingUI] Không thể huấn luyện {unit.UnitName}.");
            }
        }
    }
}
