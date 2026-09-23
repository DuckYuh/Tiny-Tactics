using UnityEngine;

namespace TinyTactics.Pathfinding
{
    /// <summary>
    /// Quản lý dữ liệu hệ thống lưới 2D, sinh tọa độ thế giới và nhận diện vật cản.
    /// Áp dụng Singleton pattern để Pathfinding, Unit, Building dễ dàng truy xuất.
    /// </summary>
    public class PathfindingGrid : MonoBehaviour
    {
        public static PathfindingGrid Instance { get; private set; }

        [Header("Grid Configuration")]
        [SerializeField] private int gridWidth = 100;
        [SerializeField] private int gridHeight = 100;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector3 originPosition = Vector3.zero;
        
        [Header("Obstacle Detection")]
        [SerializeField] private LayerMask obstacleLayerMask;

        private PathNode[,] gridArray;

        // Các Property để script bên ngoài truy cập kích thước
        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public float CellSize => cellSize;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializeGrid();
        }

        /// <summary>
        /// Sinh dữ liệu lưới và dùng OverlapBox để xác định ô nào bị cản ngay từ đầu.
        /// </summary>
        private void InitializeGrid()
        {
            gridArray = new PathNode[gridWidth, gridHeight];

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    // Lấy tọa độ tâm của ô
                    Vector3 worldPos = GetWorldPosition(x, y) + new Vector3(cellSize / 2, cellSize / 2, 0);
                    
                    // Box size nhỏ hơn cellSize 1 chút (0.9f) để tránh dính biên tường ngoài ý muốn
                    bool isObstacle = Physics2D.OverlapBox(worldPos, new Vector2(cellSize * 0.9f, cellSize * 0.9f), 0f, obstacleLayerMask);
                    
                    gridArray[x, y] = new PathNode(x, y, worldPos, !isObstacle);
                }
            }
        }

        /// <summary>
        /// Chuyển từ tọa độ (x, y) trên ma trận sang tọa độ thế giới góc dưới bên trái của ô
        /// </summary>
        public Vector3 GetWorldPosition(int x, int y)
        {
            return new Vector3(x, y, 0) * cellSize + originPosition;
        }

        /// <summary>
        /// Chuyển từ không gian thế giới vào chỉ số (x, y) trên mảng
        /// </summary>
        public void GetGridCoordinates(Vector3 worldPosition, out int x, out int y)
        {
            x = Mathf.FloorToInt((worldPosition.x - originPosition.x) / cellSize);
            y = Mathf.FloorToInt((worldPosition.y - originPosition.y) / cellSize);
        }

        /// <summary>
        /// Trả về đối tượng PathNode từ chỉ số (x, y), xử lý giới hạn an toàn.
        /// </summary>
        public PathNode GetNode(int x, int y)
        {
            if (x >= 0 && y >= 0 && x < gridWidth && y < gridHeight)
            {
                return gridArray[x, y];
            }
            return null; // Nằm ngoài map
        }

        public PathNode GetNodeFromWorldPosition(Vector3 worldPosition)
        {
            GetGridCoordinates(worldPosition, out int x, out int y);
            return GetNode(x, y);
        }

        /// <summary>
        /// Dùng khi có công trình mới được xây dựng hoặc bị hủy thời gian thực
        /// </summary>
        public void UpdateNodeWalkable(Vector3 worldPosition, bool isWalkable)
        {
            PathNode node = GetNodeFromWorldPosition(worldPosition);
            if (node != null)
            {
                node.isWalkable = isWalkable;
            }
        }

        /// <summary>
        /// Tìm ô có thể đi được (isWalkable == true) gần nhất với ô mục tiêu khi ô mục tiêu rơi vào vật cản.
        /// Sử dụng thuật toán duyệt theo chu vi hình hộp mở rộng dần (Breadth-First Perimeter Expansion).
        /// </summary>
        /// <param name="targetNode">Ô mục tiêu ban đầu</param>
        /// <param name="maxSearchRadius">Bán kính quét tối đa (tính theo ô Grid)</param>
        /// <returns>PathNode có thể đi được gần nhất, hoặc null nếu không tìm thấy</returns>
        public PathNode GetNearestWalkableNode(PathNode targetNode, int maxSearchRadius = 15)
        {
            if (targetNode == null)
            {
                return null;
            }

            if (targetNode.isWalkable)
            {
                return targetNode;
            }

            // Quét mở rộng theo từng lớp chu vi hình hộp từ r = 1 đến maxSearchRadius
            for (int r = 1; r <= maxSearchRadius; r++)
            {
                for (int x = -r; x <= r; x++)
                {
                    for (int y = -r; y <= r; y++)
                    {
                        // Chỉ duyệt các ô nằm chính xác trên chu vi ngoài cùng của bán kính r
                        if (Mathf.Abs(x) == r || Mathf.Abs(y) == r)
                        {
                            PathNode neighbor = GetNode(targetNode.x + x, targetNode.y + y);
                            if (neighbor != null && neighbor.isWalkable)
                            {
                                return neighbor;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private void OnDrawGizmos()
        {
            if (gridArray != null)
            {
                // 1. Chỉ vẽ các ô là VẬT CẢN để tiết kiệm đỉnh Gizmos
                for (int x = 0; x < gridWidth; x++)
                {
                    for (int y = 0; y < gridHeight; y++)
                    {
                        PathNode node = gridArray[x, y];
                        if (node != null && !node.isWalkable)
                        {
                            Gizmos.color = new Color(1f, 0f, 0f, 0.6f); // Đỏ mờ cho vật cản
                            Gizmos.DrawCube(node.worldPosition, new Vector3(cellSize, cellSize, 0.1f) * 0.9f);
                        }
                    }
                }

                // 2. Vẽ khung bao quanh toàn bộ Grid
                Gizmos.color = Color.white;
                Vector3 boundsSize = new Vector3(gridWidth * cellSize, gridHeight * cellSize, 0);
                Vector3 center = originPosition + boundsSize / 2f;
                Gizmos.DrawWireCube(center, boundsSize);
            }
            else
            {
                // Khi chưa ấn Play, vẽ khung viền màu xanh cyan
                Gizmos.color = Color.cyan;
                Vector3 boundsSize = new Vector3(gridWidth * cellSize, gridHeight * cellSize, 0);
                Vector3 center = originPosition + boundsSize / 2f;
                Gizmos.DrawWireCube(center, boundsSize);
            }
        }
    }
}
