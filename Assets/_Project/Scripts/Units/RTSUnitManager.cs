using System;
using System.Collections.Generic;
using UnityEngine;
using AoEMini.Selection;
using TinyTactics.Combat;
using TinyTactics.Economy;
using TinyTactics.Factions;

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

        [Header("Formation Configuration")]
        [Tooltip("Khoảng cách giãn cách giữa các đơn vị khi phân bổ đội hình")]
        [SerializeField] private float unitSpacing = 0.3f;

        // Góc vàng (Golden Angle tính bằng Radian ~ 137.5 độ) dùng cho thuật toán Sunflower Spiral
        private const float GOLDEN_ANGLE = 2.39996323f;

        /// <summary>
        /// Thông báo khi lệnh di chuyển đã được gửi cho Unit.
        /// </summary>
        public event Action MoveCommandIssued;

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

        /// <summary>
        /// Ra lệnh di chuyển thường cho toàn bộ đơn vị được chọn (bỏ qua địch trên đường đi).
        /// Được gọi từ InputController khi người chơi click chuột phải xuống mặt đất.
        /// </summary>
        public void CommandMoveTo(Vector3 targetWorldPosition)
        {
            List<UnitController> activeUnits = CollectSelectedUnitControllers();
            int unitCount = activeUnits.Count;
            if (unitCount == 0) return;

            List<Vector3> formationPositions = GetFormationPositions(targetWorldPosition, unitCount);

            for (int i = 0; i < unitCount; i++)
            {
                UnitCombatController combat = activeUnits[i].GetComponent<UnitCombatController>();
                if (combat != null)
                {
                    combat.OrderMoveTo(formationPositions[i]);
                }
                else
                {
                    activeUnits[i].MoveTo(formationPositions[i]);
                }
            }

            MoveCommandIssued?.Invoke();

            Debug.Log($"[RTSUnitManager] Đã ra lệnh di chuyển {unitCount} đơn vị tới {targetWorldPosition}");
        }

        /// <summary>
        /// Ra lệnh Attack-Move (A + Click) theo đội hình tới vị trí đích.
        /// </summary>
        public void IssueAttackMoveCommand(Vector3 targetCenter)
        {
            List<UnitController> activeUnits = CollectSelectedUnitControllers();
            int unitCount = activeUnits.Count;
            if (unitCount == 0) return;

            List<Vector3> formationPositions = GetFormationPositions(targetCenter, unitCount);

            for (int i = 0; i < unitCount; i++)
            {
                UnitCombatController combat = activeUnits[i].GetComponent<UnitCombatController>();
                if (combat != null)
                {
                    combat.AttackMoveTo(formationPositions[i]);
                }
                else
                {
                    activeUnits[i].MoveTo(formationPositions[i]);
                }
            }

            MoveCommandIssued?.Invoke();

            Debug.Log($"[RTSUnitManager] Đã ra lệnh Attack-Move {unitCount} đơn vị tới {targetCenter}");
        }

        /// <summary>
        /// Ra lệnh tấn công một mục tiêu chỉ định (Unit hoặc Công trình địch).
        /// </summary>
        public void CommandAttackTarget(IDamageable target)
        {
            if (target == null || target.IsDead)
                return;

            List<UnitController> activeUnits = CollectSelectedUnitControllers();
            int commandedCount = 0;

            for (int i = 0; i < activeUnits.Count; i++)
            {
                UnitCombatController combat = activeUnits[i].GetComponent<UnitCombatController>();
                if (combat == null)
                    continue;

                combat.AttackTarget(target);
                commandedCount++;
            }

            if (commandedCount > 0)
            {
                MoveCommandIssued?.Invoke();
                Debug.Log($"[RTSUnitManager] Đã ra lệnh cho {commandedCount} đơn vị tấn công mục tiêu.");
            }
        }

        private List<UnitController> CollectSelectedUnitControllers()
        {
            List<UnitController> activeUnits = new();

            if (SelectionSystem.Instance == null)
                return activeUnits;

            IReadOnlyList<SelectableUnit> selectedSelectables =
                SelectionSystem.Instance.SelectedUnits;

            for (int i = 0; i < selectedSelectables.Count; i++)
            {
                SelectableUnit selectable = selectedSelectables[i];

                if (!IsPlayerOwned(selectable))
                    continue;

                UnitController controller =
                    selectable.GetComponent<UnitController>();

                if (controller != null)
                    activeUnits.Add(controller);
            }

            return activeUnits;
        }

        /// <summary>
        /// Kiểm tra unit có thuộc quyền điều khiển của Player hay không.
        /// Player hiện tại sử dụng phe Blue.
        /// </summary>
        private bool IsPlayerOwned(SelectableUnit unit)
        {
            if (unit == null)
                return false;

            HealthSystem health = unit.GetComponent<HealthSystem>();

            if (health == null)
                health = unit.GetComponentInParent<HealthSystem>();

            return health != null &&
                   health.TeamFaction == Faction.Blue;
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
        /// Ra lệnh cho các nông dân đang được chọn
        /// khai thác một ResourceDeposit.
        /// Các unit không có WorkerFSM sẽ bị bỏ qua.
        /// </summary>
        public void CommandHarvest(ResourceDeposit resource)
        {
            if (resource == null || resource.currentAmount <= 0)
                return;

            if (SelectionSystem.Instance == null)
                return;

            IReadOnlyList<SelectableUnit> selectedUnits =
                SelectionSystem.Instance.SelectedUnits;

            int assignedWorkers = 0;

            for (int i = 0; i < selectedUnits.Count; i++)
            {
                SelectableUnit selected = selectedUnits[i];

                if (!IsPlayerOwned(selected))
                    continue;

                // Chỉ nông dân mới được thực hiện lệnh khai thác.
                WorkerFSM worker = selected.GetComponent<WorkerFSM>();

                if (worker == null)
                    continue;

                worker.AssignToResource(resource);
                assignedWorkers++;
            }

            Debug.Log(
                $"[RTSUnitManager] Harvest command sent to " +
                $"{assignedWorkers} worker(s). " +
                $"Resource: {resource.resourceType}"
            );
        }
    }
}
