using UnityEngine;
using TinyTactics.Units;

namespace TinyTactics.Buildings
{
    /// <summary>
    /// Công trình Tu viện (Monastery) — Kế thừa BarracksBuilding,
    /// chỉ cho phép huấn luyện Tu sĩ (SO_Monk).
    /// </summary>
    public class MonasteryBuilding : BarracksBuilding
    {
        private const string RequiredUnitName = "Monk";

        [Header("Monastery Preset")]
        [Tooltip("Dữ liệu SO_Monk mặc định nếu Trainable Units đang trống")]
        [SerializeField] private UnitDataSO _defaultMonkData;

        protected override void ConfigureBuildingDefaults()
        {
            buildingName = "Monastery";
            if (maxHP <= 500) maxHP = 900;
            buildingGridSize = new Vector2Int(2, 2);
            cameraShakeDuration = 0.25f;
            cameraShakeMagnitude = 0.15f;

            EnforceSingleUnitType(RequiredUnitName, _defaultMonkData);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnforceSingleUnitType(RequiredUnitName, _defaultMonkData);
        }
#endif
    }
}
