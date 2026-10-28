using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TinyTactics.Economy;
using TinyTactics.Input;
using TinyTactics.Audio;

namespace TinyTactics.Buildings
{
    /// <summary>
    /// Input và Preview xây dựng cho người chơi Blue.
    /// Logic tạo nhà được xử lý bởi BuildingPlacementService.
    /// </summary>
    public class BuildingPlacementController : MonoBehaviour
    {
        private const string PlayerFactionId = "Blue";

        [Header("References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private BuildingData defaultBuildingData;

        [Header("Preview Colors")]
        [SerializeField]
        private Color validColor = new Color(0f, 1f, 0f, 0.5f);

        [SerializeField]
        private Color invalidColor = new Color(1f, 0f, 0f, 0.5f);

        private BuildingData selectedBuildingData;
        private GameObject previewObject;
        private SpriteRenderer previewRenderer;

        private bool isPlacementMode;
        private bool canPlace;

        private Vector3 currentAnchorPosition;

        public bool IsPlacementMode => isPlacementMode;

        public BuildingData SelectedBuildingData =>
            selectedBuildingData;

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
        }

        private void Update()
        {
            HandleKeyboardInput();

            if (!isPlacementMode)
                return;

            UpdatePreview();
            HandlePlacementInput();
        }

        private void HandleKeyboardInput()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
                return;

            // ESC chỉ dùng để hủy chế độ đặt nhà.
            if (isPlacementMode &&
                keyboard.escapeKey.wasPressedThisFrame)
            {
                CancelPlacement();
            }
        }

        /// <summary>
        /// Chọn loại nhà bất kỳ từ UI hoặc script test.
        /// </summary>
        public void StartPlacement(BuildingData data)
        {
            if (data == null || !data.IsValid())
            {
                Debug.LogWarning(
                    "[BuildingPlacement] BuildingData không hợp lệ.");
                return;
            }

            if (mainCamera == null)
            {
                Debug.LogWarning(
                    "[BuildingPlacement] Không tìm thấy Camera.");
                return;
            }

            GameObject prefab =
                data.GetPrefab(PlayerFactionId);

            if (prefab == null)
            {
                Debug.LogWarning(
                    "[BuildingPlacement] Thiếu prefab Blue.");
                return;
            }

            SpriteRenderer sourceRenderer =
                prefab.GetComponent<SpriteRenderer>();

            if (sourceRenderer == null ||
                sourceRenderer.sprite == null)
            {
                Debug.LogWarning(
                    "[BuildingPlacement] Prefab cần SpriteRenderer " +
                    "trên GameObject gốc.");
                return;
            }

            // Chuyển loại nhà mà không thoát Input Mode.
            DestroyPreview();

            selectedBuildingData = data;

            CreatePreview(prefab, sourceRenderer);

            isPlacementMode = true;
            canPlace = false;

            RTSInputMode.EnterBuildingPlacement();
        }

        public void StartDefaultPlacement()
        {
            if (defaultBuildingData != null)
                StartPlacement(defaultBuildingData);
        }

        private void CreatePreview(
            GameObject prefab,
            SpriteRenderer sourceRenderer)
        {
            previewObject =
                new GameObject("BuildingPlacementPreview");

            previewRenderer =
                previewObject.AddComponent<SpriteRenderer>();

            previewRenderer.sprite = sourceRenderer.sprite;
            previewRenderer.flipX = sourceRenderer.flipX;
            previewRenderer.flipY = sourceRenderer.flipY;

            previewRenderer.sortingLayerID =
                sourceRenderer.sortingLayerID;

            previewRenderer.sortingOrder =
                sourceRenderer.sortingOrder + 10;

            previewRenderer.color = invalidColor;

            previewObject.transform.localScale =
                prefab.transform.localScale;
        }

        private void UpdatePreview()
        {
            Mouse mouse = Mouse.current;

            if (mouse == null ||
                previewObject == null ||
                selectedBuildingData == null)
            {
                canPlace = false;
                return;
            }

            Vector2 screenPosition =
                mouse.position.ReadValue();

            Ray ray =
                mainCamera.ScreenPointToRay(screenPosition);

            Plane groundPlane =
                new Plane(Vector3.forward, Vector3.zero);

            if (!groundPlane.Raycast(ray, out float distance))
            {
                canPlace = false;
                previewRenderer.color = invalidColor;
                return;
            }

            currentAnchorPosition = ray.GetPoint(distance);
            currentAnchorPosition.z = 0f;

            bool validPosition =
                BuildingPlacementValidator.TryValidatePlacement(
                    selectedBuildingData,
                    currentAnchorPosition,
                    out Vector3 snappedPosition,
                    out _);

            ResourceManager resources =
                ResourceManager.Instance;

            bool enoughWood =
                resources != null &&
                resources.GetResourceAmount(ResourceType.Wood) >=
                selectedBuildingData.WoodCost;

            canPlace = validPosition && enoughWood;

            previewObject.transform.position =
                snappedPosition;

            previewRenderer.color =
                canPlace ? validColor : invalidColor;
        }

        private void HandlePlacementInput()
        {
            Mouse mouse = Mouse.current;

            if (mouse == null)
                return;

            if (mouse.rightButton.wasPressedThisFrame)
            {
                CancelPlacement();
                return;
            }

            if (!mouse.leftButton.wasPressedThisFrame)
                return;

            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // Chỉ phản hồi lỗi khi người chơi thực sự click đặt nhà.
            if (!canPlace)
            {
                SFXManager.Instance?.Play(SFXType.UIError);
                return;
            }

            // Service vẫn có thể từ chối nếu trạng thái Grid
            // hoặc tài nguyên thay đổi tại thời điểm xác nhận.
            if (!TryPlaceBuilding())
            {
                SFXManager.Instance?.Play(SFXType.UIError);
            }
        }

        /// <summary>
        /// Chỉ truyền yêu cầu xây tới Service.
        /// Không trực tiếp Instantiate hoặc cập nhật Grid.
        /// </summary>
        
        private bool TryPlaceBuilding()
        {
            ResourceManager resources = ResourceManager.Instance;

            if (resources == null || selectedBuildingData == null)
            {
                return false;
            }

            bool success = BuildingPlacementService.TryPlace(
                selectedBuildingData,
                PlayerFactionId,
                currentAnchorPosition,
                amount => resources.ConsumeResource(
                    ResourceType.Wood, amount),
                out GameObject building);

            if (success)
            {
                // Chỉ phát âm thanh sau khi Service
                // xác nhận công trình được tạo thành công.
                SFXManager.Instance?.PlayAtPosition(
                    SFXType.BuildingPlace,
                    building.transform.position
                );

                Debug.Log(
                    $"[BuildingPlacement] Blue đã xây " +
                    $"{selectedBuildingData.BuildingName}: " +
                    $"{building.name}");
            }

            return success;
        }


        public void CancelPlacement()
        {
            isPlacementMode = false;
            canPlace = false;
            selectedBuildingData = null;

            RTSInputMode.ExitBuildingPlacement();

            DestroyPreview();
        }

        private void DestroyPreview()
        {
            if (previewObject != null)
                Destroy(previewObject);

            previewObject = null;
            previewRenderer = null;
        }

        private void OnDisable()
        {
            CancelPlacement();
        }

        private void OnDestroy()
        {
            CancelPlacement();
        }
    }
}