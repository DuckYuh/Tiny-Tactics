
using UnityEngine;
using TinyTactics.Pathfinding;

namespace TinyTactics.Economy
{
    public enum ResourceType
    {
        Meat,
        Wood,
        Gold
    }

    public class ResourceDeposit : MonoBehaviour
    {
        [Tooltip("Loại tài nguyên: Thịt, Gỗ, hoặc Vàng")]
        public ResourceType resourceType;

        [Tooltip("Số lượng tài nguyên hiện tại")]
        public int currentAmount = 100;

        [Tooltip("Số lượng tối đa")]
        public int maxAmount = 100;

        [Tooltip("Thời gian để nông dân thu thập 1 lần (giây)")]
        public float gatherTimePerCycle = 1f;

        [Tooltip("Số lượng tài nguyên thu thập được mỗi chu kỳ")]
        public int amountPerCycle = 2;

        public event System.Action<ResourceDeposit> OnDepleted;

        private bool isDepleted;

        /// <summary>
        /// Nông dân gọi hàm này để khai thác tài nguyên.
        /// </summary>
        public bool Harvest(int requestedAmount, out int harvestedAmount)
        {
            harvestedAmount = 0;

            if (isDepleted || currentAmount <= 0 || requestedAmount <= 0)
                return false;

            harvestedAmount = Mathf.Min(requestedAmount, currentAmount);

            currentAmount -= harvestedAmount;

            if (currentAmount <= 0)
            {
                Deplete();
            }

            return true;
        }

        /// <summary>
        /// Xử lý khi tài nguyên cạn kiệt.
        /// </summary>
        private void Deplete()
        {
            if (isDepleted)
                return;

            isDepleted = true;

            // Lấy toàn bộ collider thuộc resource,
            // bao gồm cả collider ở các object con.
            Collider2D[] colliders =
                GetComponentsInChildren<Collider2D>(true);

            Bounds resourceBounds = new Bounds(
                transform.position,
                Vector3.zero
            );

            bool hasBounds = false;

            foreach (Collider2D col in colliders)
            {
                if (col == null)
                    continue;

                // Lưu lại vùng chiếm trước khi tắt collider.
                if (col.enabled)
                {
                    if (!hasBounds)
                    {
                        resourceBounds = col.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        resourceBounds.Encapsulate(col.bounds);
                    }

                    // Xóa obstacle vật lý ngay lập tức.
                    col.enabled = false;
                }
            }

            // Đồng bộ lại Physics và Grid.
            if (hasBounds && PathfindingGrid.Instance != null)
            {
                Physics2D.SyncTransforms();

                PathfindingGrid.Instance.RefreshObstacleNodes(
                    resourceBounds
                );
            }

            // Thông báo cho MapResourceManager.
            OnDepleted?.Invoke(this);

            // Xóa resource sau khi cập nhật Grid.
            Destroy(gameObject);
        }
    }
}
