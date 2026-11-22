
using UnityEngine;
using UnityEngine.UI;
using TinyTactics.Buildings;

namespace TinyTactics.UI
{
    /// <summary>
    /// Xử lý hành động Spawn Pawn.
    /// Việc hiển thị panel thuộc trách nhiệm BuildingMenuUI.
    /// </summary>
    public sealed class PawnSpawnUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private BuildingDemolitionController demolitionController;

        [Header("Spawn Button")]
        [SerializeField]
        private Button spawnPawnButton;

        private CastleBuilding selectedCastle;

        private void OnEnable()
        {
            if (demolitionController != null)
            {
                demolitionController.OnSelectionChanged +=
                    HandleSelectionChanged;

                HandleSelectionChanged(
                    demolitionController.SelectedBuilding);
            }

            if (spawnPawnButton != null)
            {
                spawnPawnButton.onClick.AddListener(
                    OnSpawnPawnClicked);
            }
        }

        private void OnDisable()
        {
            if (demolitionController != null)
            {
                demolitionController.OnSelectionChanged -=
                    HandleSelectionChanged;
            }

            if (spawnPawnButton != null)
            {
                spawnPawnButton.onClick.RemoveListener(
                    OnSpawnPawnClicked);
            }

            selectedCastle = null;
        }

        private void HandleSelectionChanged(
            BuildingInstance building)
        {
            selectedCastle = null;

            if (building == null ||
                !building.IsInitialized ||
                building.FactionId != "Blue")
                return;

            selectedCastle =
                building.GetComponent<CastleBuilding>();
        }

        private void OnSpawnPawnClicked()
        {
            if (selectedCastle == null ||
                demolitionController == null)
                return;

            BuildingInstance currentBuilding =
                demolitionController.SelectedBuilding;

            // Không thực hiện lệnh nếu Castle không còn được chọn.
            if (currentBuilding == null ||
                currentBuilding.gameObject !=
                selectedCastle.gameObject)
                return;

            selectedCastle.TrySpawnPawn();
        }
    }
}
