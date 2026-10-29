
using UnityEngine;

namespace TinyTactics.Buildings
{
    /// <summary>
    /// Khởi tạo BuildingInstance cho công trình
    /// được đặt sẵn trong Scene, không qua Build System.
    /// </summary>
    [RequireComponent(typeof(BuildingInstance))]
    [DisallowMultipleComponent]
    public sealed class SceneBuildingInitializer : MonoBehaviour
    {
        [Header("Scene Building Configuration")]
        [SerializeField] private BuildingData buildingData;
        [SerializeField] private string factionId = "Blue";

        private void Awake()
        {
            BuildingInstance instance =
                GetComponent<BuildingInstance>();

            if (instance.IsInitialized)
                return;

            if (buildingData == null)
            {
                Debug.LogError(
                    "[SceneBuildingInitializer] Missing BuildingData.",
                    this);
                return;
            }

            if (string.IsNullOrWhiteSpace(factionId))
            {
                Debug.LogError(
                    "[SceneBuildingInitializer] Missing FactionId.",
                    this);
                return;
            }

            instance.Initialize(buildingData, factionId);
        }
    }
}
