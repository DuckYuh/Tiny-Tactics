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
    /// Quản lý CommandHUD / BuildingPanel cho người chơi Blue.
    /// </summary>
    public class BuildingMenuUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private BuildingPlacementController placementController;

        [Header("Panels")]
        [SerializeField] private GameObject commandPanel;
        [SerializeField] private GameObject buildingPanel;

        [Header("Buttons")]
        [SerializeField] private Button buildButton;
        [SerializeField] private Button backButton;

        [Header("Building Entries")]
        [SerializeField]
        private List<BuildingMenuEntry> buildingEntries = new();

        private int lastWoodAmount = int.MinValue;

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

        private void Start()
        {
            CloseBuildingMenu();
            RefreshBuildingButtons();
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

        public void OpenBuildingMenu()
        {
            if (commandPanel != null)
                commandPanel.SetActive(false);

            if (buildingPanel != null)
                buildingPanel.SetActive(true);

            RefreshBuildingButtons();
        }

        public void CloseBuildingMenu()
        {
            if (buildingPanel != null)
                buildingPanel.SetActive(false);

            if (commandPanel != null)
                commandPanel.SetActive(true);
        }

        public void ToggleBuildingMenu()
        {
            if (buildingPanel == null)
                return;

            if (buildingPanel.activeSelf)
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

            if (data.GetPrefab("Blue") == null)
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
                        data.GetPrefab("Blue") != null;
                }
                // Cập nhật icon công trình từ BuildingData.
                if (entry.iconImage != null)
                {
                    Sprite sprite = valid
                        ? data.BuildingIcon
                        : null;

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
        }
    }
}