using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TinyTactics.Units
{
    /// <summary>
    /// Quản lý danh sách các đơn vị quân (Unit) đang được chọn và điều phối lệnh di chuyển.
    /// Tích hợp thuật toán phân bổ đội hình tỏa tròn (Sunflower/Spiral Distribution) để tránh dồn cục lính.
    /// </summary>
    public class RTSUnitManager : MonoBehaviour
    {
        // Singleton pattern cho Main Thread
        public static RTSUnitManager Instance { get; private set; }

        [Header("Selection Configuration")]
        [Tooltip("Danh sách các đơn vị quân đang được người chơi chọn")]
        public List<UnitController> selectedUnits = new List<UnitController>();

        [Header("Formation Configuration")]
        [Tooltip("Khoảng cách giãn cách giữa các đơn vị khi phân bổ đội hình")]
        [SerializeField] private float unitSpacing = 0.3f;

        // Góc vàng (Golden Angle tính bằng Radian ~ 137.5 độ) dùng cho thuật toán Sunflower Spiral
        private const float GOLDEN_ANGLE = 2.39996323f;

        private void Awake()
        {
            // Thiết lập Singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            HandleMovementInput();
        }

        /// <summary>
        /// Bắt sự kiện click chuột phải từ Unity New Input System để ra lệnh di chuyển cho toàn bộ đơn vị được chọn.
        /// </summary>
        private void HandleMovementInput()
        {
            // Kiểm tra chuột và sự kiện click chuột phải trong frame hiện tại
            if (Mouse.current == null || !Mouse.current.rightButton.wasPressedThisFrame)
            {
                return;
            }

            // Nếu không có đơn vị nào được chọn thì không xử lý tiếp
            if (selectedUnits == null || selectedUnits.Count == 0)
            {
                return;
            }

            // Lấy tọa độ màn hình từ Input System
            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();

            // Chuyển đổi sang tọa độ thế giới (World Space) 2D
            if (Camera.main == null)
            {
                Debug.LogWarning("[RTSUnitManager] Không tìm thấy Camera.main để tính toán tọa độ thế giới!");
                return;
            }

            // Khoảng cách từ Camera đến mặt phẳng z = 0 để ScreenToWorldPoint tính chính xác
            float distanceFromCamera = Mathf.Abs(Camera.main.transform.position.z);
            Vector3 screenPointWithDistance = new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, distanceFromCamera);
            Vector3 targetWorldPosition = Camera.main.ScreenToWorldPoint(screenPointWithDistance);
            targetWorldPosition.z = 0f; // Khóa trục z cho không gian 2D

            // Loại bỏ các phần tử null khỏi danh sách trước khi ra lệnh
            selectedUnits.RemoveAll(unit => unit == null);

            int unitCount = selectedUnits.Count;
            if (unitCount == 0) return;

            // Tính toán mảng điểm đến theo đội hình
            List<Vector3> formationPositions = GetFormationPositions(targetWorldPosition, unitCount);

            // Gửi lệnh di chuyển đến từng đơn vị tương ứng
            for (int i = 0; i < unitCount; i++)
            {
                if (selectedUnits[i] != null)
                {
                    selectedUnits[i].MoveTo(formationPositions[i]);
                }
            }
        }

        /// <summary>
        /// Phân bổ các điểm đích theo các vòng tròn đồng tâm (Concentric Rings / Hexagonal Packing).
        /// Đảm bảo mọi đơn vị đều đứng cách nhau đúng bằng khoảng cách `unitSpacing`, tạo thành khối quân gắn kết khít nhau.
        /// </summary>
        /// <param name="center">Tọa độ đích trung tâm người chơi click</param>
        /// <param name="unitCount">Số lượng đơn vị quân cần phân bổ</param>
        /// <returns>Danh sách các điểm đến riêng biệt cho từng đơn vị</returns>
        private List<Vector3> GetFormationPositions(Vector3 center, int unitCount)
        {
            List<Vector3> positions = new List<Vector3>(unitCount);

            if (unitCount <= 0) return positions;

            // Đơn vị đầu tiên luôn chiếm vị trí trung tâm (Tâm click chuột)
            positions.Add(center);
            if (unitCount == 1) return positions;

            int currentRing = 1;
            int unitsPlaced = 1;

            // Xếp các đơn vị thành từng vòng tròn đồng tâm mở rộng dần
            while (unitsPlaced < unitCount)
            {
                // Chu vi vòng tròn xấp xỉ 2 * PI * R ≈ 6 * R, do đó vòng thứ k sẽ chứa tối đa 6 * k đơn vị
                int maxUnitsInThisRing = 6 * currentRing;
                int unitsInThisRing = Mathf.Min(maxUnitsInThisRing, unitCount - unitsPlaced);
                float ringRadius = currentRing * unitSpacing;
                float angleStep = (2f * Mathf.PI) / maxUnitsInThisRing;

                for (int i = 0; i < unitsInThisRing; i++)
                {
                    float angle = i * angleStep;
                    float offsetX = Mathf.Cos(angle) * ringRadius;
                    float offsetY = Mathf.Sin(angle) * ringRadius;

                    positions.Add(new Vector3(center.x + offsetX, center.y + offsetY, 0f));
                    unitsPlaced++;
                }

                currentRing++;
            }

            return positions;
        }

        /// <summary>
        /// Hàm hỗ trợ đăng ký/hủy chọn đơn vị từ SelectionSystem bên ngoài
        /// </summary>
        public void SetSelectedUnits(List<UnitController> newSelection)
        {
            selectedUnits.Clear();
            if (newSelection != null)
            {
                selectedUnits.AddRange(newSelection);
            }
        }
    }
}
