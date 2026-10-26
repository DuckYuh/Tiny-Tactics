using UnityEngine;
using TinyTactics.Buildings;

namespace TinyTactics.UI
{
    public enum MinimapTargetType
    {
        Unit,
        Building
    }

    /// <summary>
    /// Đánh dấu một đối tượng cần hiển thị trên Minimap.
    /// Không phụ thuộc logic di chuyển, xây dựng hoặc combat.
    /// </summary>
    [DisallowMultipleComponent]
    public class MinimapTarget : MonoBehaviour
    {
        [Header("Marker Configuration")]
        [SerializeField]
        private MinimapTargetType targetType =
            MinimapTargetType.Unit;

        [SerializeField] private string factionId = "Blue";

        [Tooltip("Điểm hiển thị Marker. Để trống sẽ dùng Transform hiện tại.")]
        [SerializeField] private Transform markerPosition;

        private BuildingInstance buildingInstance;

        public MinimapTargetType TargetType => targetType;

        public Vector3 WorldPosition =>
            markerPosition != null
                ? markerPosition.position
                : transform.position;

        public string FactionId
        {
            get
            {
                // Building được xây dựng có dữ liệu phe riêng.
                if (buildingInstance != null &&
                    buildingInstance.IsInitialized)
                {
                    return buildingInstance.FactionId;
                }

                return factionId;
            }
        }

        private void Awake()
        {
            buildingInstance = GetComponent<BuildingInstance>();
        }

        private void OnEnable()
        {
            MinimapMarkerManager.RegisterTarget(this);
        }

        private void OnDisable()
        {
            MinimapMarkerManager.UnregisterTarget(this);
        }
    }
}
