using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Pathfinding;

namespace TinyTactics.Units
{
    /// <summary>
    /// Component điều khiển hành vi di chuyển và né nhau (Separation) của từng đơn vị lính.
    /// Tích hợp trực tiếp với thuật toán A* Pathfinding và hỗ trợ làm mượt đường đi (Path Smoothing).
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class UnitController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Tốc độ di chuyển cơ sở của đơn vị")]
        [SerializeField] private float moveSpeed = 3.5f;

        [Tooltip("Khoảng cách tối thiểu để xác nhận đã chạm mốc Waypoint")]
        [SerializeField] private float waypointTolerance = 0.15f;

        [Header("Separation Settings (Tránh đè nhau)")]
        [Tooltip("Bán kính phát hiện các đồng minh xung quanh để kích hoạt lực tách")]
        [SerializeField] private float separationRadius = 0.6f;

        [Tooltip("Độ mạnh của lực đẩy tách nhau")]
        [SerializeField] private float separationStrength = 2.5f;

        [Tooltip("LayerMask định danh các đơn vị lính đồng minh")]
        [SerializeField] private LayerMask unitLayerMask;

        // Dữ liệu đường đi
        private List<Vector3> currentPath;
        private int currentWaypointIndex = 0;
        private bool isMoving = false;

        // Service tìm đường A*
        private Pathfinding.Pathfinding pathfindingService;

        private void Awake()
        {
            // Khởi tạo Pure C# Service Pathfinding
            pathfindingService = new Pathfinding.Pathfinding();
        }



        private void Update()
        {
            HandleMovement();
        }

        /// <summary>
        /// Nhận lệnh di chuyển từ RTSUnitManager, yêu cầu đường đi A* và làm mượt Waypoint.
        /// </summary>
        /// <param name="destination">Điểm đích phân bổ trong đội hình</param>
        public void MoveTo(Vector3 destination)
        {
            if (pathfindingService == null)
            {
                pathfindingService = new Pathfinding.Pathfinding();
            }

            // 1. Tìm đường đi thô qua A*
            List<Vector3> rawPath = pathfindingService.FindPath(transform.position, destination);

            if (rawPath != null && rawPath.Count > 0)
            {
                // 2. Kiểm tra ô tại tọa độ destination có thực sự đi được hay không
                PathNode destNode = PathfindingGrid.Instance != null 
                    ? PathfindingGrid.Instance.GetNodeFromWorldPosition(destination) 
                    : null;

                // Chỉ gán đè destination nếu ô đó thực sự đi được (isWalkable == true)
                // Nếu destination là vật cản, giữ nguyên rawPath[rawPath.Count - 1] (là tâm ô trống gần nhất do A* tìm)
                if (destNode != null && destNode.isWalkable)
                {
                    rawPath[rawPath.Count - 1] = new Vector3(destination.x, destination.y, transform.position.z);
                }

                // 3. Lược bỏ các node thừa trên cùng đường thẳng
                currentPath = SmoothPath(rawPath);
                currentWaypointIndex = 0;
                isMoving = true;
            }
            else
            {
                // Không tìm thấy đường tới đích (bị chặn hoặc ngoài map)
                isMoving = false;
                currentPath = null;
            }
        }

        /// <summary>
        /// Lược bỏ các waypoint dư thừa thẳng hàng bằng phép kiểm tra Vector2.Dot giữa 2 vector chỉ hướng.
        /// </summary>
        private List<Vector3> SmoothPath(List<Vector3> path)
        {
            if (path == null || path.Count <= 2)
            {
                return path;
            }

            List<Vector3> smoothed = new List<Vector3>();
            smoothed.Add(path[0]);

            for (int i = 1; i < path.Count - 1; i++)
            {
                Vector2 dirPrev = ((Vector2)(path[i] - smoothed[smoothed.Count - 1])).normalized;
                Vector2 dirNext = ((Vector2)(path[i + 1] - path[i])).normalized;

                // Nếu góc giữa 2 vector gần như thẳng hàng (Dot product ~ 1.0f) thì bỏ qua node ở giữa
                if (Vector2.Dot(dirPrev, dirNext) < 0.999f)
                {
                    smoothed.Add(path[i]);
                }
            }

            // Luôn giữ lại điểm đích cuối cùng
            smoothed.Add(path[path.Count - 1]);
            return smoothed;
        }

        /// <summary>
        /// Điều khiển di chuyển tuần tự qua các Waypoint, kết hợp lực tách nhau (Separation Force).
        /// </summary>
        private void HandleMovement()
        {
            if (!isMoving || currentPath == null || currentWaypointIndex >= currentPath.Count)
            {
                isMoving = false;
                return;
            }

            Vector3 targetWaypoint = currentPath[currentWaypointIndex];
            targetWaypoint.z = transform.position.z; // Giữ nguyên độ sâu Z 2D

            // Kiểm tra nếu đã chạm mốc Waypoint hiện tại
            float distanceToWaypoint = Vector2.Distance(transform.position, targetWaypoint);
            if (distanceToWaypoint <= waypointTolerance)
            {
                currentWaypointIndex++;
                if (currentWaypointIndex >= currentPath.Count)
                {
                    isMoving = false;
                    currentPath = null;
                    return;
                }
                targetWaypoint = currentPath[currentWaypointIndex];
                targetWaypoint.z = transform.position.z;
            }

            // Hướng di chuyển chính về phía Waypoint
            Vector3 dirTowardsWaypoint = (targetWaypoint - transform.position).normalized;

            // Tính toán lực đẩy tránh đè nhau với các lính xung quanh
            Vector3 separationForce = CalculateSeparation();

            // Tổng hợp vector di chuyển
            Vector3 movementVector = dirTowardsWaypoint * moveSpeed + separationForce;

            // Cập nhật vị trí đơn vị
            transform.position += movementVector * Time.deltaTime;

            // Tự động xoay hướng nhìn (Flip Sprite) theo chiều ngang
            if (Mathf.Abs(movementVector.x) > 0.01f)
            {
                Vector3 scale = transform.localScale;
                scale.x = movementVector.x > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }

        /// <summary>
        /// Tính toán lực đẩy tách nhau (Separation Force) từ các đồng minh lân cận để tránh đè lấn lính.
        /// </summary>
        private Vector3 CalculateSeparation()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, separationRadius, unitLayerMask);
            if (colliders == null || colliders.Length <= 1)
            {
                return Vector3.zero;
            }

            Vector2 totalPushForce = Vector2.zero;
            int neighborCount = 0;

            foreach (Collider2D col in colliders)
            {
                // Bỏ qua chính bản thân đơn vị
                if (col.gameObject == gameObject)
                {
                    continue;
                }

                Vector2 diff = (Vector2)transform.position - (Vector2)col.transform.position;
                float distance = diff.magnitude;

                if (distance < separationRadius && distance > 0.001f)
                {
                    // Lực đẩy tỷ lệ nghịch với khoảng cách (càng gần nhau lực đẩy càng mạnh)
                    float factor = (separationRadius - distance) / separationRadius;
                    totalPushForce += diff.normalized * factor;
                    neighborCount++;
                }
                else if (distance <= 0.001f)
                {
                    // Nếu 2 lính đứng trùng khít tọa độ, sinh lực đẩy ngẫu nhiên để tách ra
                    totalPushForce += Random.insideUnitCircle.normalized;
                    neighborCount++;
                }
            }

            if (neighborCount > 0)
            {
                Vector2 avgPush = (totalPushForce / neighborCount) * separationStrength;
                return new Vector3(avgPush.x, avgPush.y, 0f);
            }

            return Vector3.zero;
        }

        /// <summary>
        /// Dừng di chuyển ngay lập tức.
        /// </summary>
        public void Stop()
        {
            isMoving = false;
            currentPath = null;
            currentWaypointIndex = 0;
        }

        /// <summary>
        /// Hiển thị trực quan bán kính Separation và đường đi trên Scene View khi chọn Unit.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Vẽ bán kính lực tách nhau (màu vàng)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, separationRadius);

            // Vẽ đường đi hiện tại của unit (màu xanh lá)
            if (currentPath != null && currentPath.Count > 0)
            {
                Gizmos.color = Color.green;
                for (int i = currentWaypointIndex; i < currentPath.Count - 1; i++)
                {
                    Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
                }
            }
        }
    }
}
