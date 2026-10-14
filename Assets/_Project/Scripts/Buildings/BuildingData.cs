using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinyTactics.Buildings
{
    /// <summary>
    /// Liên kết một phe với prefab công trình tương ứng.
    /// </summary>
    [Serializable]
    public class FactionBuildingPrefab
    {
        [Tooltip("ID của phe, ví dụ: Blue, Red, Green")]
        public string factionId;

        [Tooltip("Prefab công trình của phe này")]
        public GameObject prefab;
    }

    /// <summary>
    /// Dữ liệu chung của công trình.
    /// Hỗ trợ nhiều phe thông qua danh sách prefab.
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewBuildingData",
        menuName = "Tiny Tactics/Building Data")]
    public class BuildingData : ScriptableObject
    {
        [Header("Building")]
        [SerializeField] private string buildingName;

        [Header("Faction Prefabs")]
        [SerializeField]
        private List<FactionBuildingPrefab> factionPrefabs = new();

        [Header("Grid Footprint")]
        [SerializeField]
        private Vector2Int footprint = Vector2Int.one;

        [Header("Resource Cost")]
        [Min(0)]
        [SerializeField] private int woodCost = 0;

        [Header("UI")]
        [Tooltip("Icon đại diện dùng chung cho Building Menu và SelectionHUD.")]
        [SerializeField] private Sprite buildingIcon;

        public Sprite BuildingIcon => buildingIcon;

        public string BuildingName => buildingName;
        public Vector2Int Footprint => footprint;
        public int WoodCost => woodCost;

        /// <summary>
        /// Lấy prefab công trình dựa trên ID của phe.
        /// </summary>
        public GameObject GetPrefab(string factionId)
        {
            if (string.IsNullOrEmpty(factionId))
                return null;

            foreach (FactionBuildingPrefab entry in factionPrefabs)
            {
                if (entry != null &&
                    entry.factionId == factionId)
                {
                    return entry.prefab;
                }
            }

            return null;
        }

        /// <summary>
        /// Kiểm tra cấu hình công trình.
        /// </summary>
        public bool IsValid()
        {
            if (footprint.x <= 0 ||
                footprint.y <= 0 ||
                woodCost < 0 ||
                factionPrefabs == null ||
                factionPrefabs.Count == 0)
            {
                return false;
            }

            HashSet<string> factionIds = new();

            foreach (FactionBuildingPrefab entry in factionPrefabs)
            {
                if (entry == null ||
                    string.IsNullOrWhiteSpace(entry.factionId) ||
                    entry.prefab == null ||
                    !factionIds.Add(entry.factionId))
                {
                    return false;
                }
            }

            return true;
        }
    }
}