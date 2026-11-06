
using System;
using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Buildings;

namespace TinyTactics.Factions
{
    /// <summary>
    /// Cấu hình bộ Animator Blue/Red cho từng binh chủng.
    /// </summary>
    [Serializable]
    public sealed class UnitFactionAnimationEntry
    {
        public string unitName;

        public RuntimeAnimatorController blueController;
        public RuntimeAnimatorController redController;
    }

    /// <summary>
    /// Tự động thiết lập FactionVisual cho Unit
    /// được sinh ra từ Barracks.
    ///
    /// Không thay đổi gameplay, combat hoặc prefab gốc.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FactionSpawnVisualBinder : MonoBehaviour
    {
        [Header("Unit Animation Mapping")]
        [SerializeField]
        private List<UnitFactionAnimationEntry> unitAnimations =
            new List<UnitFactionAnimationEntry>();

        private void OnEnable()
        {
            // Đăng ký nhận thông báo sinh quân.
            BarracksBuilding.OnAnyUnitTrained += HandleUnitTrained;
        }

        private void OnDisable()
        {
            // Hủy đăng ký để tránh giữ reference không cần thiết.
            BarracksBuilding.OnAnyUnitTrained -= HandleUnitTrained;
        }

        private void HandleUnitTrained(
            BarracksBuilding barracks,
            GameObject spawnedUnit)
        {
            if (barracks == null || spawnedUnit == null)
                return;

            Animator animator = spawnedUnit.GetComponent<Animator>();

            if (animator == null)
            {
                Debug.LogWarning(
                    "[FactionBinder] Unit không có Animator.",
                    spawnedUnit);
                return;
            }

            // Xác định phe của công trình sinh quân.
            string factionId = GetBuildingFaction(barracks);

            if (factionId != "Blue" && factionId != "Red")
            {
                Debug.LogWarning(
                    $"[FactionBinder] Phe không được hỗ trợ: {factionId}",
                    barracks);
                return;
            }

            // Tìm cấu hình dựa trên Animator Controller gốc.
            UnitFactionAnimationEntry entry =
                FindAnimationEntry(animator.runtimeAnimatorController);

            if (entry == null)
            {
                Debug.LogWarning(
                    $"[FactionBinder] Chưa cấu hình animation cho " +
                    $"{spawnedUnit.name}.",
                    spawnedUnit);
                return;
            }

            // Chỉ thêm component khi Unit chưa có.
            FactionVisual visual =
                spawnedUnit.GetComponent<FactionVisual>();

            if (visual == null)
                visual = spawnedUnit.AddComponent<FactionVisual>();

            // Áp dụng bộ animation theo phe.
            if (!visual.Configure(
                    entry.blueController,
                    entry.redController,
                    factionId))
            {
                return;
            }

            Debug.Log(
                $"[FactionBinder] {spawnedUnit.name} " +
                $"đã được gán visual phe {factionId}.",
                spawnedUnit);
        }

        /// <summary>
        /// Ưu tiên FactionId từ BuildingInstance đã khởi tạo.
        /// Fallback về BuildingBase cho công trình đặt sẵn.
        /// </summary>
        private static string GetBuildingFaction(
            BarracksBuilding barracks)
        {
            BuildingInstance instance =
                barracks.GetComponent<BuildingInstance>();

            if (instance != null &&
                instance.IsInitialized &&
                !string.IsNullOrWhiteSpace(instance.FactionId))
            {
                return instance.FactionId;
            }

            return barracks.FactionId;
        }

        /// <summary>
        /// Tìm cấu hình tương ứng với Animator hiện tại.
        /// </summary>
        private UnitFactionAnimationEntry FindAnimationEntry(
            RuntimeAnimatorController controller)
        {
            if (controller == null)
                return null;

            for (int i = 0; i < unitAnimations.Count; i++)
            {
                UnitFactionAnimationEntry entry = unitAnimations[i];

                if (entry == null ||
                    entry.blueController == null ||
                    entry.redController == null)
                {
                    continue;
                }

                if (controller == entry.blueController)
                    return entry;
            }

            return null;
        }
    }
}
