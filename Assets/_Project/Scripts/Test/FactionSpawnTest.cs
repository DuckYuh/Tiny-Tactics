
using UnityEngine;
using TinyTactics.Buildings;
using TinyTactics.Economy;
using TinyTactics.Units;

namespace TinyTactics.Factions.Tests
{
    /// <summary>
    /// Kiểm thử huấn luyện quân theo phe.
    /// Chỉ sử dụng trong scene test, không dùng cho gameplay.
    /// </summary>
    public sealed class FactionSpawnTest : MonoBehaviour
    {
        [Header("Barracks References")]
        [SerializeField] private BarracksBuilding blueBarracks;
        [SerializeField] private BarracksBuilding redBarracks;

        [Header("Test Settings")]
        [SerializeField, Min(0)]
        private int testUnitIndex = 0;

        [SerializeField] private bool provideTestResources = true;

        [ContextMenu("Test Spawn Blue")]
        private void TestSpawnBlue()
        {
            QueueTestUnit(blueBarracks, "Blue");
        }

        [ContextMenu("Test Spawn Red")]
        private void TestSpawnRed()
        {
            QueueTestUnit(redBarracks, "Red");
        }

        private void QueueTestUnit(
            BarracksBuilding barracks,
            string expectedFaction)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "[FactionSpawnTest] Chỉ chạy trong Play Mode.",
                    this);
                return;
            }

            if (barracks == null)
            {
                Debug.LogError(
                    $"[FactionSpawnTest] Thiếu {expectedFaction} Barracks.",
                    this);
                return;
            }

            if (barracks.FactionId != expectedFaction)
            {
                Debug.LogError(
                    $"[FactionSpawnTest] FactionId không đúng: " +
                    $"{barracks.FactionId}.",
                    barracks);
                return;
            }

            var units = barracks.TrainableUnits;

            if (testUnitIndex >= units.Count)
            {
                Debug.LogError(
                    "[FactionSpawnTest] Unit Index không hợp lệ.",
                    this);
                return;
            }

            UnitDataSO unitData = units[testUnitIndex];

            if (unitData == null)
            {
                Debug.LogError(
                    "[FactionSpawnTest] UnitDataSO bị null.",
                    this);
                return;
            }

            ResourceManager resources = ResourceManager.Instance;

            if (resources == null)
            {
                Debug.LogError(
                    "[FactionSpawnTest] Thiếu ResourceManager.",
                    this);
                return;
            }

            // Chỉ bổ sung phần tài nguyên còn thiếu để test.
            if (provideTestResources)
            {
                AddMissingResources(
                    resources,
                    ResourceType.Meat,
                    unitData.MeatCost);

                AddMissingResources(
                    resources,
                    ResourceType.Wood,
                    unitData.WoodCost);

                AddMissingResources(
                    resources,
                    ResourceType.Gold,
                    unitData.GoldCost);
            }

            PopulationManager population = PopulationManager.Instance;

            if (population == null ||
                !population.CanSpawnUnit(unitData.PopulationCost))
            {
                Debug.LogWarning(
                    "[FactionSpawnTest] Không đủ Population. " +
                    "Kiểm tra Castle/House trong scene.",
                    this);
                return;
            }

            bool queued = barracks.TryQueueUnit(unitData);

            if (queued)
            {
                Debug.Log(
                    $"[FactionSpawnTest] Đã yêu cầu sinh " +
                    $"{unitData.UnitName} phe {expectedFaction}.",
                    this);
            }
            else
            {
                Debug.LogWarning(
                    "[FactionSpawnTest] Không thể thêm Unit vào queue.",
                    this);
            }
        }

        private static void AddMissingResources(
            ResourceManager resources,
            ResourceType type,
            int required)
        {
            int current = resources.GetResourceAmount(type);
            int missing = Mathf.Max(0, required - current);

            if (missing > 0)
                resources.AddResource(type, missing);
        }
    }
}
