
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TinyTactics.Buildings;
using TinyTactics.Economy;

namespace TinyTactics.UI
{
    [Serializable]
    public class BuildingMenuEntry
    {
        public BuildingData buildingData;
        public Button button;
        public TMP_Text nameText;
        public TMP_Text costText;
        public Image iconImage;
    }

    /// <summary>
    /// Điều phối các menu trong CommandHUD:
    /// Build, danh sách công trình, Train Barracks.
    /// </summary>
    public class BuildingMenuUI : MonoBehaviour
    {
        private const string PlayerFactionId = "Blue";

        [Header("References")]
        [SerializeField]
        private BuildingPlacementController placementController;

        [SerializeField]
        private BuildingDemolitionController demolitionController;

        [Header("Panels")]
        [SerializeField] private GameObject commandPanel;
        [SerializeField] private GameObject buildingPanel;
        [SerializeField] private GameObject barracksCommandPanel;

        [Header("Buttons")]
        [SerializeField] private Button buildButton;
        [SerializeField] private Button backButton;

        [Header("Building Entries")]
        [SerializeField]
        private List<BuildingMenuEntry> buildingEntries = new();

        private int lastWoodAmount = int.MinValue;
        private bool isBuildingMenuOpen;

        private void Awake()
        {
            if (buildButton != null)
                buildButton.onClick.AddListener(OpenBuildingMenu);

            if (backButton != null)
                backButton.onClick.AddListener(CloseBuildingMenu);

            foreach (BuildingMenuEntry entry in buildingEntries)
            {
                if (entry == null || entry.button == null)
                    continue;

                BuildingData data = entry.buildingData;

                entry.button.onClick.AddListener(
                    () => SelectBuilding(data));
            }
        }

        private void OnEnable()
        {
            if (demolitionController != null)
            {
                demolitionController.OnSelectionChanged +=
                    HandleSelectionChanged;
            }

            RefreshCommandPanels();
        }

        private void Start()
        {
            isBuildingMenuOpen = false;
            RefreshCommandPanels();
            RefreshBuildingButtons();
        }

        private void OnDisable()
        {
            if (demolitionController != null)
            {
                demolitionController.OnSelectionChanged -=
                    HandleSelectionChanged;
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null &&
                keyboard.bKey.wasPressedThisFrame)
            {
                ToggleBuildingMenu();
            }

            ResourceManager resources = ResourceManager.Instance;

            if (resources == null)
                return;

            int wood = resources.GetResourceAmount(
                ResourceType.Wood);

            if (wood != lastWoodAmount)
                RefreshBuildingButtons();
        }

        private void HandleSelectionChanged(BuildingInstance building)
        {
            // Khi chuyển lựa chọn công trình, đóng menu xây.
            isBuildingMenuOpen = false;
            RefreshCommandPanels();
        }

        /// <summary>
        /// Chỉ hàm này được quản lý trạng thái ba panel.
        /// </summary>
        private void RefreshCommandPanels()
        {
            bool showBarracks = false;

            if (demolitionController != null)
            {
                BuildingInstance building =
                    demolitionController.SelectedBuilding;

                if (building != null &&
                    building.IsInitialized &&
                    building.FactionId == PlayerFactionId)
                {
                    showBarracks =
                        building.GetComponent<BarracksBuilding>() != null;
                }
            }

            bool showBuildingMenu = isBuildingMenuOpen;

            if (buildingPanel != null)
                buildingPanel.SetActive(showBuildingMenu);

            if (barracksCommandPanel != null)
            {
                barracksCommandPanel.SetActive(
                    !showBuildingMenu && showBarracks);
            }

            if (commandPanel != null)
            {
                commandPanel.SetActive(
                    !showBuildingMenu && !showBarracks);
            }
        }

        public void OpenBuildingMenu()
        {
            isBuildingMenuOpen = true;
            RefreshCommandPanels();
            RefreshBuildingButtons();
        }

        public void CloseBuildingMenu()
        {
            isBuildingMenuOpen = false;
            RefreshCommandPanels();
        }

        public void ToggleBuildingMenu()
        {
            if (buildingPanel == null)
                return;

            if (isBuildingMenuOpen)
                CloseBuildingMenu();
            else
                OpenBuildingMenu();
        }

        private void SelectBuilding(BuildingData data)
        {
            if (placementController == null ||
                data == null ||
                !data.IsValid())
                return;

            ResourceManager resources = ResourceManager.Instance;

            if (resources == null ||
                resources.GetResourceAmount(ResourceType.Wood) <
                data.WoodCost)
                return;

            if (data.GetPrefab(PlayerFactionId) == null)
                return;

            placementController.StartPlacement(data);
            CloseBuildingMenu();
        }

        public void RefreshBuildingButtons()
        {
            ResourceManager resources = ResourceManager.Instance;

            int wood = resources != null
                ? resources.GetResourceAmount(ResourceType.Wood)
                : 0;

            lastWoodAmount = wood;

            foreach (BuildingMenuEntry entry in buildingEntries)
            {
                if (entry == null)
                    continue;

                BuildingData data = entry.buildingData;

                bool valid = data != null && data.IsValid();

                if (entry.nameText != null)
                {
                    entry.nameText.text =
                        valid ? data.BuildingName : "Unknown";
                }

                if (entry.costText != null)
                {
                    entry.costText.text =
                        valid ? $"{data.WoodCost}" : "--";
                }

                if (entry.button != null)
                {
                    entry.button.interactable =
                        valid &&
                        resources != null &&
                        placementController != null &&
                        wood >= data.WoodCost &&
                        data.GetPrefab(PlayerFactionId) != null;
                }

                if (entry.iconImage != null)
                {
                    Sprite sprite =
                        valid ? data.BuildingIcon : null;

                    entry.iconImage.sprite = sprite;
                    entry.iconImage.enabled = sprite != null;
                    entry.iconImage.preserveAspect = true;
                }
            }
        }

        private void OnDestroy()
        {
            if (buildButton != null)
                buildButton.onClick.RemoveListener(OpenBuildingMenu);

            if (backButton != null)
                backButton.onClick.RemoveListener(CloseBuildingMenu);

            foreach (BuildingMenuEntry entry in buildingEntries)
            {
                if (entry != null && entry.button != null)
                {
                    // Listeners được Unity giải phóng khi UI bị hủy.
                    // Không dùng RemoveAllListeners để tránh xóa callback khác.
                }
            }
        }
    }
}
