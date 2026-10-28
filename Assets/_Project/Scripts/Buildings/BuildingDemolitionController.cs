using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TinyTactics.Audio;

namespace TinyTactics.Buildings
{
    /// <summary>
    /// Quản lý lựa chọn và phá hủy công trình.
    /// Chỉ người chơi Blue được chủ động phá nhà Blue.
    /// </summary>
    public sealed class BuildingDemolitionController : MonoBehaviour
    {
        private const string PlayerFactionId = "Blue";

        [Header("Selection")]
        [SerializeField] private Camera mainCamera;

        private BuildingInstance selectedBuilding;

        private BuildingSelectionVisual selectedVisual;

        public BuildingInstance SelectedBuilding => selectedBuilding;

        // Dành cho SelectionHUD ở bước sau.
        public event Action<BuildingInstance> OnSelectionChanged;

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
                return;

            // Không phá nhà khi đang đặt công trình.
            if (TinyTactics.Input.RTSInputMode.IsWorldInputBlocked)
                return;

            if (keyboard.deleteKey.wasPressedThisFrame)
                DemolishSelectedBuilding();
        }

        /// <summary>
        /// Tìm công trình tại vị trí click trên màn hình.
        /// Trả về true nếu click trúng công trình.
        /// </summary>
        public bool TrySelectAtScreenPosition(Vector2 screenPosition)
        {
            if (mainCamera == null)
                return false;

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);

            Plane groundPlane =
                new Plane(Vector3.forward, Vector3.zero);

            if (!groundPlane.Raycast(ray, out float distance))
                return false;

            Vector3 worldPoint = ray.GetPoint(distance);

            // Gọi khi click, không gọi mỗi frame.
            Collider2D[] hits = Physics2D.OverlapPointAll(
                new Vector2(worldPoint.x, worldPoint.y));

            foreach (Collider2D hit in hits)
            {
                if (hit == null)
                    continue;

                BuildingInstance building =
                    hit.GetComponentInParent<BuildingInstance>();

                if (building == null || !building.IsInitialized)
                    continue;

                SelectBuilding(building);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Chọn công trình và kích hoạt highlight.
        /// </summary>
        public void SelectBuilding(BuildingInstance building)
        {
            if (selectedBuilding == building)
                return;

            // Tắt highlight công trình trước đó.
            if (selectedVisual != null)
                selectedVisual.SetHighlighted(false);

            selectedBuilding = building;
            selectedVisual = null;

            if (selectedBuilding != null)
            {
                selectedVisual =
                    selectedBuilding.GetComponent<BuildingSelectionVisual>();

                if (selectedVisual != null)
                    selectedVisual.SetHighlighted(true);
            }

            OnSelectionChanged?.Invoke(selectedBuilding);
        }

        /// <summary>
        /// Bỏ chọn và khôi phục màu gốc của công trình.
        /// </summary>
        public void ClearSelection()
        {
            if (selectedVisual != null)
                selectedVisual.SetHighlighted(false);

            if (selectedBuilding == null && selectedVisual == null)
                return;

            selectedBuilding = null;
            selectedVisual = null;

            OnSelectionChanged?.Invoke(null);
        }

        /// <summary>
        /// Phá công trình được chọn nếu thuộc phe Blue.
        /// </summary>
        public bool DemolishSelectedBuilding()
        {
            if (TinyTactics.Input.RTSInputMode.IsWorldInputBlocked)
                return false;

            if (selectedBuilding == null)
                return false;

            if (!selectedBuilding.IsInitialized)
            {
                ClearSelection();
                return false;
            }

            if (selectedBuilding.FactionId != PlayerFactionId)
            {
                Debug.Log(
                    "[BuildingDemolition] Không thể phá nhà của phe Red.");
                return false;
            }

            GameObject buildingObject = selectedBuilding.gameObject;

            // Bỏ chọn trước khi hủy để UI không giữ reference cũ.
            ClearSelection();

            // Phát SFX tại vị trí công trình được phá.
            SFXManager.Instance?.PlayAtPosition(
                SFXType.BuildingDestroy,
                buildingObject.transform.position
            );

            Destroy(buildingObject);

            Debug.Log(
                $"[BuildingDemolition] Đã phá {buildingObject.name}.");

            return true;
        }

        private void OnDisable()
        {
            ClearSelection();
        }
    }
}