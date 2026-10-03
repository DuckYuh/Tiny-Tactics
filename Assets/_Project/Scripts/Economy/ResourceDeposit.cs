using UnityEngine;

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

        public event System.Action OnDepleted;

        /// <summary>
        /// Nông dân gọi hàm này để khai thác tài nguyên.
        /// </summary>
        /// <param name="requestedAmount">Lượng tài nguyên muốn lấy</param>
        /// <param name="harvestedAmount">Lượng tài nguyên thực tế lấy được</param>
        /// <returns>Trả về true nếu khai thác thành công, false nếu cạn kiệt</returns>
        public bool Harvest(int requestedAmount, out int harvestedAmount)
        {
            if (currentAmount <= 0)
            {
                harvestedAmount = 0;
                return false;
            }

            harvestedAmount = Mathf.Min(requestedAmount, currentAmount);
            currentAmount -= harvestedAmount;

            if (currentAmount <= 0)
            {
                OnDepleted?.Invoke();
                Destroy(gameObject); // Huỷ object khi cạn kiệt (có thể đổi thành Pool sau này)
            }

            return true;
        }
    }
}
