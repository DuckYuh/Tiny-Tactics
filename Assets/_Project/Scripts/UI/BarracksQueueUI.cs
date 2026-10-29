
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
    public class TrainingQueueSlot
    {
        public Button button;
        public Image unitIcon;
    }

    /// <summary>
    /// Hiển thị hàng đợi và tiến độ huấn luyện Barracks.
    /// Không trực tiếp quản lý việc sinh quân hoặc tài nguyên.
    /// </summary>
    public sealed class BarracksQueueUI : MonoBehaviour
    {
        private const string PlayerFactionId = "Blue";

        [Header("References")]
        [SerializeField]
        private BuildingDemolitionController demolitionController;

        [SerializeField]
        private GameObject barracksQueuePanel;

        [Header("Queue")]
        [SerializeField]
        private TMP_Text queueCountText;

        [SerializeField]
        private TrainingQueueSlot[] slots =
            new TrainingQueueSlot[5];

        [Header("Training Progress")]
        [SerializeField]
        private Image progressFill;

        [SerializeField]
        private TMP_Text progressText;

        [SerializeField]
        private TMP_Text trainingNameText;

        private BarracksBuilding selectedBarracks;

        private void Awake()
        {
            // Đăng ký sự kiện Click một lần cho từng slot.
            // Không sử dụng biến vòng lặp trực tiếp trong lambda.
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

            UnbindBarracks();
        }

        private void OnDestroy()
        {
            // Các Button thuộc vòng đời UI.
            // Unity tự hủy listener khi Button bị hủy.
        }

        /// <summary>
        /// Thay đổi Barracks được theo dõi khi Selection đổi.
        /// </summary>
        private void HandleSelectionChanged(
            BuildingInstance building)
        {
            BarracksBuilding nextBarracks = null;

            if (building != null &&
                building.IsInitialized &&
                building.FactionId == PlayerFactionId)
            {
                nextBarracks =
                    building.GetComponent<BarracksBuilding>();
            }

            if (selectedBarracks != nextBarracks)
            {
                UnbindBarracks();
                selectedBarracks = nextBarracks;

                if (selectedBarracks != null)
                {
                    selectedBarracks.OnQueueUpdated +=
                        HandleQueueUpdated;

                    selectedBarracks.OnTrainingProgressChanged +=
                        HandleTrainingProgress;
                }
            }

            bool visible = selectedBarracks != null;

            if (barracksQueuePanel != null)
                barracksQueuePanel.SetActive(visible);

            if (!visible)
                return;

            // Đồng bộ dữ liệu hiện tại khi chọn lại nhà lính.
            RefreshQueue(selectedBarracks.TrainingQueue);
            RefreshProgress(
                selectedBarracks.CurrentProgress,
                selectedBarracks.CurrentTrainingUnit);
        }

        private void UnbindBarracks()
        {
            if (selectedBarracks != null)
            {
                selectedBarracks.OnQueueUpdated -=
                    HandleQueueUpdated;

                selectedBarracks.OnTrainingProgressChanged -=
                    HandleTrainingProgress;
            }

            selectedBarracks = null;
        }

        private void HandleQueueUpdated(
            IReadOnlyList<UnitDataSO> queue)
        {
            RefreshQueue(queue);

            if (selectedBarracks != null)
            {
                RefreshProgress(
                    selectedBarracks.CurrentProgress,
                    selectedBarracks.CurrentTrainingUnit);
            }
        }

        /// <summary>
        /// Chỉ vẽ lại slot khi hàng đợi thay đổi.
        /// </summary>
        private void RefreshQueue(
            IReadOnlyList<UnitDataSO> queue)
        {
            int count = queue != null ? queue.Count : 0;

            if (queueCountText != null)
            {
                int max = selectedBarracks != null
                    ? selectedBarracks.MaxQueueSize
                    : slots.Length;

                queueCountText.text = $"Queue: {count}/{max}";
            }

            for (int i = 0; i < slots.Length; i++)
            {
                TrainingQueueSlot slot = slots[i];

                if (slot == null)
                    continue;

                UnitDataSO unit =
                    i < count ? queue[i] : null;

                bool occupied = unit != null;

                if (slot.button != null)
                    slot.button.interactable = occupied;

                if (slot.unitIcon != null)
                {
                    Sprite icon = occupied
                        ? unit.UnitIcon
                        : null;

                    slot.unitIcon.sprite = icon;
                    slot.unitIcon.enabled = icon != null;
                    slot.unitIcon.preserveAspect = true;
                }
            }
        }

        private void HandleTrainingProgress(
            float progress,
            UnitDataSO unit)
        {
            RefreshProgress(progress, unit);
        }

        /// <summary>
        /// Event từ Barracks cập nhật tiến độ.
        /// Không cần Update() riêng trong UI.
        /// </summary>
        private void RefreshProgress(
            float progress,
            UnitDataSO unit)
        {
            progress = Mathf.Clamp01(progress);

            if (progressFill != null)
                progressFill.fillAmount = progress;

            if (progressText != null)
            {
                progressText.text =
                    unit != null
                        ? $"{Mathf.RoundToInt(progress * 100f)}%"
                        : "0%";
            }

            if (trainingNameText != null)
            {
                trainingNameText.text =
                    unit != null
                        ? $"Training: {unit.UnitName}"
                        : "Idle";
            }
        }

        /// <summary>
        /// Click slot để hủy huấn luyện.
        /// BarracksBuilding chịu trách nhiệm hoàn tài nguyên
        /// và giải phóng dân số.
        /// </summary>
        private void CancelSlot(int index)
        {
            if (selectedBarracks == null ||
                demolitionController == null)
                return;

            BuildingInstance current =
                demolitionController.SelectedBuilding;

            if (current == null ||
                current.gameObject != selectedBarracks.gameObject)
                return;

            if (index < 0 ||
                index >= selectedBarracks.CurrentQueueCount)
                return;

            selectedBarracks.CancelQueue(index);
        }
    }
}
