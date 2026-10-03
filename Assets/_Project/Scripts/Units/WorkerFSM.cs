using UnityEngine;
using TinyTactics.Economy;
using TinyTactics.Buildings;

namespace TinyTactics.Units
{
    public enum WorkerState
    {
        Idle,
        MovingToResource,
        Harvesting,
        ReturningToCastle,
        Depositing,
        MovingToBuild,
        Building
    }

    [RequireComponent(typeof(UnitController))]
    public class WorkerFSM : MonoBehaviour
    {
        [Header("References")]
        private UnitController unitController;

        [Header("Worker Settings")]
        [Tooltip("Sức chứa tài nguyên tối đa trong túi")]
        [SerializeField] private int backpackCapacity = 10;
        
        [Tooltip("Khoảng cách tương tác với tài nguyên và nhà")]
        [SerializeField] private float interactionRange = 1.0f;
        
        [Tooltip("Bán kính tìm kiếm tài nguyên mới nếu tài nguyên cũ cạn kiệt")]
        [SerializeField] private float scanRadius = 15f;

        [Header("Runtime Info - Read Only")]
        [SerializeField] private WorkerState currentState = WorkerState.Idle;
        [SerializeField] private ResourceType heldType;
        [SerializeField] private int heldAmount = 0;

        // Cache state
        private ResourceDeposit targetResource;
        private CastleDepositArea targetCastle;
        private float gatherTimer = 0f;

        private void Awake()
        {
            unitController = GetComponent<UnitController>();
        }

        private void Update()
        {
            switch (currentState)
            {
                case WorkerState.Idle:
                    HandleIdleState();
                    break;
                case WorkerState.MovingToResource:
                    HandleMovingToResourceState();
                    break;
                case WorkerState.Harvesting:
                    HandleHarvestingState();
                    break;
                case WorkerState.ReturningToCastle:
                    HandleReturningToCastleState();
                    break;
                case WorkerState.Depositing:
                    HandleDepositingState();
                    break;
                case WorkerState.MovingToBuild:
                    // TODO: Logic di chuyển đến công trình
                    break;
                case WorkerState.Building:
                    // TODO: Logic thi công
                    break;
            }
        }

        /// <summary>
        /// Gọi hàm này từ RTSUnitManager (hoặc chuột) để gán lệnh lấy tài nguyên.
        /// </summary>
        public void AssignToResource(ResourceDeposit resource)
        {
            if (resource == null) return;
            
            targetResource = resource;
            heldType = targetResource.resourceType; // Ghi nhớ loại tài nguyên cần lấy
            ChangeState(WorkerState.MovingToResource);
        }

        private void ChangeState(WorkerState newState)
        {
            currentState = newState;
            
            if (currentState == WorkerState.MovingToResource)
            {
                if (targetResource != null)
                {
                    unitController.MoveTo(targetResource.transform.position);
                }
            }
            else if (currentState == WorkerState.ReturningToCastle)
            {
                FindNearestCastle();
                if (targetCastle != null)
                {
                    unitController.MoveTo(targetCastle.GetDropOffPosition());
                }
                else
                {
                    Debug.LogWarning("Không tìm thấy Castle! Nông dân sẽ đứng yên chờ lệnh.");
                    ChangeState(WorkerState.Idle);
                }
            }
            else if (currentState == WorkerState.Idle)
            {
                unitController.Stop();
            }
            else if (currentState == WorkerState.Harvesting)
            {
                unitController.Stop(); // Dừng lại để bắt đầu cuốc/chặt
            }
        }

        private void HandleIdleState()
        {
            // Nông dân đứng yên, chờ lệnh.
        }

        private void HandleMovingToResourceState()
        {
            // Xử lý Target Null: Mỏ cạn hoặc cây bị chặt trong lúc đang đi
            if (targetResource == null)
            {
                FindAlternativeResource();
                return;
            }

            float distance = Vector2.Distance(transform.position, targetResource.transform.position);
            
            // Đã tới gần đủ tầm tương tác
            if (distance <= interactionRange)
            {
                gatherTimer = 0f;
                ChangeState(WorkerState.Harvesting);
            }
            else
            {
                // Có thể cập nhật lại đường đi nếu resource (đàn cừu) di chuyển
                // Nhưng hiện tại để đơn giản thì unitController.MoveTo() ở ChangeState là đủ
            }
        }

        private void HandleHarvestingState()
        {
            if (targetResource == null)
            {
                // Cây bị chặt hoặc mỏ vàng hết trong lúc đang khai thác dở
                if (heldAmount > 0)
                {
                    ChangeState(WorkerState.ReturningToCastle);
                }
                else
                {
                    FindAlternativeResource();
                }
                return;
            }

            // Phòng trường hợp tài nguyên di chuyển (đàn cừu) rời xa tầm tay
            float distance = Vector2.Distance(transform.position, targetResource.transform.position);
            if (distance > interactionRange)
            {
                ChangeState(WorkerState.MovingToResource);
                return;
            }

            // Đếm ngược thời gian khai thác
            gatherTimer += Time.deltaTime;
            if (gatherTimer >= targetResource.gatherTimePerCycle)
            {
                gatherTimer = 0f;
                
                // Chỉ lấy đủ sức chứa của túi
                int amountToRequest = Mathf.Min(targetResource.amountPerCycle, backpackCapacity - heldAmount);
                
                if (targetResource.Harvest(amountToRequest, out int harvested))
                {
                    heldAmount += harvested;
                    if (heldAmount >= backpackCapacity)
                    {
                        // Đầy túi, về Lâu đài
                        ChangeState(WorkerState.ReturningToCastle);
                    }
                }
                else
                {
                    // Tài nguyên hết
                    if (heldAmount > 0)
                        ChangeState(WorkerState.ReturningToCastle);
                    else
                        FindAlternativeResource();
                }
            }
        }

        private void HandleReturningToCastleState()
        {
            if (targetCastle == null)
            {
                FindNearestCastle();
                if (targetCastle == null)
                {
                    ChangeState(WorkerState.Idle);
                    return;
                }
                // Tìm lại đường
                unitController.MoveTo(targetCastle.GetDropOffPosition());
            }

            float distance = Vector2.Distance(transform.position, targetCastle.transform.position);
            if (distance <= interactionRange)
            {
                unitController.Stop();
                ChangeState(WorkerState.Depositing);
            }
        }

        private void HandleDepositingState()
        {
            if (ResourceManager.Instance != null && heldAmount > 0)
            {
                // Giao nộp
                ResourceManager.Instance.AddResource(heldType, heldAmount);
                heldAmount = 0;
            }

            // Ngay lập tức quay lại tài nguyên cũ (nếu còn)
            if (targetResource != null)
            {
                ChangeState(WorkerState.MovingToResource);
            }
            else
            {
                // Tìm mỏ/cây cùng loại
                FindAlternativeResource(); 
            }
        }

        /// <summary>
        /// Quét tìm CastleDepositArea gần nhất trong cảnh.
        /// </summary>
        private void FindNearestCastle()
        {
            CastleDepositArea[] castles = FindObjectsOfType<CastleDepositArea>();
            float nearestDist = float.MaxValue;
            CastleDepositArea nearest = null;

            foreach (var c in castles)
            {
                float d = Vector2.Distance(transform.position, c.transform.position);
                if (d < nearestDist)
                {
                    nearestDist = d;
                    nearest = c;
                }
            }
            targetCastle = nearest;
        }

        /// <summary>
        /// Tìm tài nguyên cùng loại ở gần (khi tài nguyên cũ cạn kiệt).
        /// </summary>
        private void FindAlternativeResource()
        {
            ResourceDeposit[] deposits = FindObjectsOfType<ResourceDeposit>();
            float nearestDist = float.MaxValue;
            ResourceDeposit nearest = null;

            foreach (var d in deposits)
            {
                if (d.resourceType == heldType)
                {
                    float dist = Vector2.Distance(transform.position, d.transform.position);
                    if (dist <= scanRadius && dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = d;
                    }
                }
            }

            if (nearest != null)
            {
                targetResource = nearest;
                ChangeState(WorkerState.MovingToResource);
            }
            else
            {
                // Không còn mỏ nào quanh đây
                ChangeState(WorkerState.Idle);
            }
        }
    }
}
