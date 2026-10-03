using UnityEngine;

namespace TinyTactics.Buildings
{
    /// <summary>
    /// Gắn lên Công trình Lâu đài (Castle) để làm điểm nộp tài nguyên cho Nông dân.
    /// </summary>
    public class CastleDepositArea : MonoBehaviour
    {
        /// <summary>
        /// Trả về vị trí để nông dân tiến lại giao nộp.
        /// </summary>
        public Vector3 GetDropOffPosition()
        {
            // Trả về tâm của Castle (sau này có thể đổi thành các điểm neo nếu Castle lớn)
            return transform.position;
        }
    }
}
