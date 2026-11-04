
using UnityEngine;

namespace TinyTactics.Animation
{
    /// <summary>
    /// Quản lý animation của Unit.
    /// Hỗ trợ Idle, Run, Harvest và Attack.
    /// Không xử lý gameplay, sát thương hoặc cooldown.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class UnitAnimationController : MonoBehaviour
    {
        private static readonly int IsMovingHash =
            Animator.StringToHash("IsMoving");

        private static readonly int HarvestTypeHash =
            Animator.StringToHash("HarvestType");

        private static readonly int AttackHash =
            Animator.StringToHash("Attack");

        private static readonly int AttackDirectionHash =
            Animator.StringToHash("AttackDirection");

        private Animator animator;

        private bool isMoving;
        private int harvestType;

        private bool supportsHarvest;
        private bool supportsAttack;

        private SpriteRenderer spriteRenderer;
        private bool supportsAttackDirection;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            // Kiểm tra các parameter tùy theo từng loại Unit.
            // Chỉ thực hiện một lần khi khởi tạo.
            for (int i = 0; i < animator.parameterCount; i++)
            {
                AnimatorControllerParameter parameter =
                    animator.GetParameter(i);

                if (parameter.nameHash == HarvestTypeHash &&
                    parameter.type == AnimatorControllerParameterType.Int)
                {
                    supportsHarvest = true;
                }
                else if (parameter.nameHash == AttackHash &&
                        parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    supportsAttack = true;
                }
                else if (parameter.nameHash == AttackDirectionHash &&
                        parameter.type == AnimatorControllerParameterType.Int)
                {
                    supportsAttackDirection = true;
                }
            }

            isMoving = false;
            harvestType = 0;

            animator.SetBool(IsMovingHash, false);

            if (supportsHarvest)
                animator.SetInteger(HarvestTypeHash, 0);
        }


        /// <summary>
        /// Phát animation Attack theo hướng mục tiêu.
        /// Dùng cho Lancer có 5 hướng tấn công.
        /// </summary>
        /// <param name="targetPosition">
        /// Vị trí mục tiêu trong World Space.
        /// </param>
        public bool PlayDirectionalAttack(Vector3 targetPosition)
        {
            if (!supportsAttack || !supportsAttackDirection ||
                !animator.isActiveAndEnabled)
            {
                return false;
            }

            Vector2 direction =
                (Vector2)(targetPosition - transform.position);

            // Không đổi hướng khi mục tiêu trùng vị trí Unit.
            if (direction.sqrMagnitude < 0.0001f)
            {
                return PlayAttack();
            }

            float absX = Mathf.Abs(direction.x);
            float absY = Mathf.Abs(direction.y);

            // Ngưỡng phân vùng 22.5° và 67.5°.
            const float tan22 = 0.41421356f;
            const float tan67 = 2.41421356f;

            int attackDirection;

            if (absY <= absX * tan22)
            {
                // Đánh ngang.
                attackDirection = 0;
            }
            else if (absY >= absX * tan67)
            {
                // Đánh dọc.
                attackDirection = direction.y > 0 ? 2 : 4;
            }
            else
            {
                // Đánh chéo.
                attackDirection = direction.y > 0 ? 1 : 3;
            }

            // Flip sprite theo phía trái/phải của mục tiêu.
            if (spriteRenderer != null && absX > 0.0001f)
            {
                spriteRenderer.flipX = direction.x < 0;
            }

            // Chọn hướng trước khi kích hoạt Attack.
            animator.SetInteger(AttackDirectionHash, attackDirection);
            animator.SetTrigger(AttackHash);

            return true;
        }


        /// <summary>
        /// Chuyển đổi Idle và Run.
        /// </summary>
        public void SetMoving(bool moving)
        {
            if (isMoving == moving)
                return;

            isMoving = moving;
            animator.SetBool(IsMovingHash, moving);
        }

        /// <summary>
        /// Đặt animation khai thác cho Pawn.
        /// 0 = None, 1 = Wood, 2 = Gold, 3 = Meat.
        /// </summary>
        public void SetHarvestType(int type)
        {
            if (!supportsHarvest)
                return;

            type = Mathf.Clamp(type, 0, 3);

            if (harvestType == type)
                return;

            harvestType = type;
            animator.SetInteger(HarvestTypeHash, type);
        }

        /// <summary>
        /// Ngừng khai thác.
        /// </summary>
        public void StopHarvest()
        {
            SetHarvestType(0);
        }

        /// <summary>
        /// Kích hoạt animation tấn công.
        /// Trả về false nếu Animator không hỗ trợ Attack.
        /// </summary>
        public bool PlayAttack()
        {
            if (!supportsAttack || !animator.isActiveAndEnabled)
                return false;

            animator.SetTrigger(AttackHash);
            return true;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Kiểm thử Attack bằng Context Menu trong Play Mode.
        /// Không sử dụng trong Build.
        /// </summary>
        [ContextMenu("Test Attack (Play Mode)")]
        private void TestAttack()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "Chỉ kiểm thử Attack trong Play Mode.",
                    this
                );
                return;
            }

            if (!PlayAttack())
            {
                Debug.LogWarning(
                    "Animator không hỗ trợ Attack Trigger.",
                    this
                );
            }
        }

        [ContextMenu("Test Attack Right")]
        private void TestAttackRight()
        {
            if (!Application.isPlaying) return;

            PlayDirectionalAttack(
                transform.position + Vector3.right
            );
        }

        [ContextMenu("Test Attack Left")]
        private void TestAttackLeft()
        {
            if (!Application.isPlaying) return;

            PlayDirectionalAttack(
                transform.position + Vector3.left
            );
        }

        [ContextMenu("Test Attack Up")]
        private void TestAttackUp()
        {
            if (!Application.isPlaying) return;

            PlayDirectionalAttack(
                transform.position + Vector3.up
            );
        }

        [ContextMenu("Test Attack Down")]
        private void TestAttackDown()
        {
            if (!Application.isPlaying) return;

            PlayDirectionalAttack(
                transform.position + Vector3.down
            );
        }

        [ContextMenu("Test Attack UpRight")]
        private void TestAttackUpRight()
        {
            if (!Application.isPlaying) return;

            PlayDirectionalAttack(
                transform.position + new Vector3(1, 1, 0)
            );
        }

        [ContextMenu("Test Attack DownLeft")]
        private void TestAttackDownLeft()
        {
            if (!Application.isPlaying) return;

            PlayDirectionalAttack(
                transform.position + new Vector3(-1, -1, 0)
            );
        }
#endif
    }
}
