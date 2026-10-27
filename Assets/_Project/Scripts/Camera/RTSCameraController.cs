using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TinyTactics.CameraSystem
{
    /// <summary>
    /// Camera controller dành cho RTS:
    /// - Di chuyển khi chuột chạm mép màn hình.
    /// - Zoom bằng Mouse Wheel.
    /// - Giới hạn camera trong phạm vi map.
    ///
    /// Sử dụng LateUpdate để camera di chuyển ổn định sau gameplay update.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class RTSCameraController : MonoBehaviour
    {
        [Header("Edge Scrolling")]
        [SerializeField, Min(0f)]
        private float moveSpeed = 12f;

        [SerializeField, Range(1f, 200f)]
        private float edgeSize = 20f;

        [Header("Zoom")]
        [SerializeField, Min(0f)]
        private float zoomSpeed = 5f;

        [SerializeField, Min(0.1f)]
        private float minZoom = 4f;

        [SerializeField, Min(0.1f)]
        private float maxZoom = 15f;

        [Header("Map Bounds")]
        [Tooltip("Phạm vi World Space của toàn bộ map.")]
        [SerializeField]
        private Rect mapBounds = new Rect(0f, 0f, 100f, 100f);

        private Camera _camera;

        public Camera MainCamera => _camera;
        public Rect MapBounds => mapBounds;

        private void Awake()
        {
            _camera = GetComponent<Camera>();

            // RTS camera nên dùng Orthographic.
            _camera.orthographic = true;

            // Đảm bảo zoom ban đầu nằm trong giới hạn.
            _camera.orthographicSize = Mathf.Clamp(
                _camera.orthographicSize,
                minZoom,
                maxZoom
            );
        }

        private Vector3 _shakeOffset = Vector3.zero;
        private Coroutine _shakeCoroutine;

        private void LateUpdate()
        {
            HandleEdgeScrolling();
            HandleZoom();

            ClampCameraPosition();

            if (_shakeOffset != Vector3.zero)
            {
                transform.position += _shakeOffset;
            }
        }

        /// <summary>
        /// Kích hoạt hiệu ứng rung màn hình (Camera Shake) khi công trình bị phá hủy hoặc nhận chấn động mạnh.
        /// </summary>
        public void Shake(float duration = 0.25f, float magnitude = 0.15f)
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
            }

            _shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, magnitude));
        }

        private System.Collections.IEnumerator ShakeCoroutine(float duration, float magnitude)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                _shakeOffset = new Vector3(x, y, 0f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            _shakeOffset = Vector3.zero;
            _shakeCoroutine = null;
        }

        /// <summary>
        /// Di chuyển camera khi chuột nằm gần mép màn hình.
        /// </summary>
        private void HandleEdgeScrolling()
        {
            if (Mouse.current == null)
                return;

            // Không scroll camera khi người chơi đang tương tác với UI.
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Vector2 mousePosition = Mouse.current.position.ReadValue();

            Vector3 direction = Vector3.zero;

            // Left
            if (mousePosition.x <= edgeSize)
            {
                direction.x -= 1f;
            }
            // Right
            else if (mousePosition.x >= Screen.width - edgeSize)
            {
                direction.x += 1f;
            }

            // Bottom
            if (mousePosition.y <= edgeSize)
            {
                direction.y -= 1f;
            }
            // Top
            else if (mousePosition.y >= Screen.height - edgeSize)
            {
                direction.y += 1f;
            }

            if (direction.sqrMagnitude <= 0f)
                return;

            // Normalize để di chuyển chéo không nhanh hơn
            // di chuyển ngang/dọc.
            direction.Normalize();

            transform.position += direction * moveSpeed * Time.deltaTime;
        }

        /// <summary>
        /// Zoom camera bằng Mouse Wheel.
        /// Orthographic Size càng nhỏ -> zoom càng gần.
        /// </summary>
        private void HandleZoom()
        {
            if (Mouse.current == null)
                return;

            float scrollValue = Mouse.current.scroll.ReadValue().y;

            if (Mathf.Approximately(scrollValue, 0f))
                return;

            // Scroll lên -> zoom in
            // Scroll xuống -> zoom out
            _camera.orthographicSize -= scrollValue * zoomSpeed * 0.01f;

            _camera.orthographicSize = Mathf.Clamp(
                _camera.orthographicSize,
                minZoom,
                maxZoom
            );
        }

        /// <summary>
        /// Giữ camera nằm trong map.
        /// Có tính tới kích thước viewport của camera.
        /// </summary>
        private void ClampCameraPosition()
        {
            float verticalHalfSize = _camera.orthographicSize;
            float horizontalHalfSize =
                verticalHalfSize * _camera.aspect;

            float minX = mapBounds.xMin + horizontalHalfSize;
            float maxX = mapBounds.xMax - horizontalHalfSize;

            float minY = mapBounds.yMin + verticalHalfSize;
            float maxY = mapBounds.yMax - verticalHalfSize;

            Vector3 position = transform.position;

            // Nếu camera lớn hơn map thì giữ camera ở giữa map.
            float clampedX = minX <= maxX
                ? Mathf.Clamp(position.x, minX, maxX)
                : mapBounds.center.x;

            float clampedY = minY <= maxY
                ? Mathf.Clamp(position.y, minY, maxY)
                : mapBounds.center.y;

            transform.position = new Vector3(
                clampedX,
                clampedY,
                position.z
            );
        }

        /// <summary>
        /// Di chuyển Camera đến vị trí được chỉ định.
        /// Giữ nguyên tọa độ Z và giới hạn trong Map.
        /// </summary>
        public void MoveToPosition(Vector2 worldPosition)
        {
            transform.position = new Vector3(
                worldPosition.x,
                worldPosition.y,
                transform.position.z
            );

            ClampCameraPosition();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireCube(
                mapBounds.center,
                mapBounds.size
            );
        }
#endif
    }
}