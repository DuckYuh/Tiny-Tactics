using UnityEngine;
using TinyTactics.Units;

namespace TinyTactics.Buildings
{
    /// <summary>
    /// Công trình Nhà luyện cung (Archery) — Kế thừa BarracksBuilding,
    /// chỉ cho phép huấn luyện Cung thủ (SO_Archer).
    /// </summary>
    public class ArcheryBuilding : BarracksBuilding
    {
        private const string RequiredUnitName = "Archer";

        [Header("Archery Preset")]
        [Tooltip("Dữ liệu SO_Archer mặc định nếu Trainable Units đang trống")]
        [SerializeField] private UnitDataSO _defaultArcherData;

        protected override void ConfigureBuildingDefaults()
        {
            buildingName = "Archery";
            if (maxHP <= 500) maxHP = 1000;
            buildingGridSize = new Vector2Int(2, 2);
            cameraShakeDuration = 0.25f;
            cameraShakeMagnitude = 0.15f;

            EnforceSingleUnitType(RequiredUnitName, _defaultArcherData);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnforceSingleUnitType(RequiredUnitName, _defaultArcherData);
        }
#endif
    }
}
