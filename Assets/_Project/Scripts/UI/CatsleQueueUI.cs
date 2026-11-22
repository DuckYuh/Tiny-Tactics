using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TinyTactics.Buildings;
using TinyTactics.Units;

namespace TinyTactics.UI
{
    [Serializable]
    public class CastleQueueSlot
    {
        public Button button;
        public Image unitIcon;
    }

    public sealed class CatsleQueueUI : MonoBehaviour
    {
        private const string PlayerFactionId = "Blue";

        [Header("References")]
        [SerializeField]
        private BuildingDemolitionController demolitionController;

        [SerializeField]
        private GameObject castleQueuePanel;

        [Header("Queue")]
        [SerializeField]
        private TMP_Text queueCountText;

        [SerializeField]
        private CastleQueueSlot[] slots =
            new CastleQueueSlot[5];

        [Header("Pawn Spawn Progress")]
        [SerializeField]
        private Image progressBar;

        [SerializeField]
        private Image progressFill;

        [SerializeField]
        private TMP_Text progressText;

        [SerializeField]
        private TMP_Text pawnNameText;

        private CastleBuilding selectedCastle;

        private void Awake()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                int index = i;

                if (slots[i]?.button != null)
                {
                    slots[i].button.onClick.AddListener(
                        () => CancelSlot(index));
                }
            }
        }

        private void OnEnable()
        {
            if (demolitionController != null)
            {
                demolitionController.OnSelectionChanged +=
                    HandleSelectionChanged;
            }

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

            UnbindCastle();
        }

        private void HandleSelectionChanged(
            BuildingInstance building)
        {
            CastleBuilding nextCastle = null;

            if (building != null &&
                building.IsInitialized &&
                building.FactionId == PlayerFactionId)
            {
                nextCastle =
                    building.GetComponent<CastleBuilding>();
            }

            if (selectedCastle != nextCastle)
            {
                UnbindCastle();
                selectedCastle = nextCastle;

                if (selectedCastle != null)
                {
                    selectedCastle.OnPawnQueueUpdated +=
                        HandleQueueUpdated;
                    selectedCastle.OnPawnSpawnProgressChanged +=
                        HandleProgressChanged;
                }
            }

            bool visible = selectedCastle != null;

            if (castleQueuePanel != null)
                castleQueuePanel.SetActive(visible);

            if (!visible)
            {
                SetProgressVisible(false);
                return;
            }

            RefreshQueue(selectedCastle.PawnQueue);
            RefreshProgress(
                selectedCastle.PawnSpawnProgress,
                selectedCastle.CurrentPawnQueueUnit);
        }

        private void UnbindCastle()
        {
            if (selectedCastle != null)
            {
                selectedCastle.OnPawnQueueUpdated -=
                    HandleQueueUpdated;
                selectedCastle.OnPawnSpawnProgressChanged -=
                    HandleProgressChanged;
            }

            selectedCastle = null;
        }

        private void HandleQueueUpdated(
            IReadOnlyList<UnitDataSO> queue)
        {
            RefreshQueue(queue);

            if (selectedCastle != null)
            {
                RefreshProgress(
                    selectedCastle.PawnSpawnProgress,
                    selectedCastle.CurrentPawnQueueUnit);
            }
        }

        private void HandleProgressChanged(float progress)
        {
            if (selectedCastle == null)
                return;

            RefreshProgress(
                progress,
                selectedCastle.CurrentPawnQueueUnit);
        }

        private void RefreshQueue(
            IReadOnlyList<UnitDataSO> queue)
        {
            int count = queue != null ? queue.Count : 0;
            SetProgressVisible(count > 0);

            if (queueCountText != null)
            {
                int max = selectedCastle != null
                    ? selectedCastle.MaxPawnQueueSize
                    : slots.Length;

                queueCountText.text = $"Queue: {count}/{max}";
            }

            for (int i = 0; i < slots.Length; i++)
            {
                CastleQueueSlot slot = slots[i];

                if (slot == null)
                    continue;

                UnitDataSO unit =
                    i < count ? queue[i] : null;
                bool occupied = unit != null;

                if (slot.button != null)
                    slot.button.interactable = occupied;

                if (slot.unitIcon != null)
                {
                    slot.unitIcon.sprite =
                        occupied ? unit.UnitIcon : null;
                    slot.unitIcon.enabled =
                        occupied && unit.UnitIcon != null;
                    slot.unitIcon.preserveAspect = true;
                }
            }
        }

        private void RefreshProgress(
            float progress,
            UnitDataSO unit)
        {
            progress = Mathf.Clamp01(progress);
            SetProgressVisible(unit != null);

            if (progressFill != null)
                progressFill.fillAmount = progress;

            if (progressText != null)
            {
                progressText.text =
                    unit != null
                        ? $"{Mathf.RoundToInt(progress * 100f)}%"
                        : "0%";
            }

            if (pawnNameText != null)
            {
                pawnNameText.text =
                    unit != null
                        ? $"Training: {unit.UnitName}"
                        : "Idle";
            }
        }

        private void SetProgressVisible(bool visible)
        {
            if (progressBar != null)
                progressBar.gameObject.SetActive(visible);

            if (progressFill != null)
                progressFill.gameObject.SetActive(visible);

            if (progressText != null)
                progressText.gameObject.SetActive(visible);

            if (pawnNameText != null)
                pawnNameText.gameObject.SetActive(visible);
        }

        private void CancelSlot(int index)
        {
            if (selectedCastle == null ||
                demolitionController == null)
                return;

            BuildingInstance current =
                demolitionController.SelectedBuilding;

            if (current == null ||
                current.gameObject != selectedCastle.gameObject)
                return;

            if (index < 0 ||
                index >= selectedCastle.CurrentPawnQueueCount)
                return;

            selectedCastle.CancelPawnQueue(index);
        }
    }
}
