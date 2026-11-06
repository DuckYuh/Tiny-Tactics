
using System;
using UnityEngine;

namespace TinyTactics.Factions
{
    /// <summary>
    /// Quản lý hình ảnh Unit theo phe.
    /// Không xử lý movement, combat hoặc gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class FactionVisual : MonoBehaviour
    {
        [Header("Faction")]
        [SerializeField] private string factionId = "Blue";

        [Header("Animator Controllers")]
        [SerializeField] private RuntimeAnimatorController blueController;
        [SerializeField] private RuntimeAnimatorController redController;
        [SerializeField] private RuntimeAnimatorController purpleController;
        [SerializeField] private RuntimeAnimatorController yellowController;

        private Animator animator;

        public string FactionId => factionId;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            ApplyFactionVisual();
        }

        /// <summary>
        /// Đổi phe của Unit và cập nhật animation.
        /// Có thể được gọi từ Spawn System hoặc AI.
        /// </summary>
        public bool SetFaction(string newFactionId)
        {
            if (string.IsNullOrWhiteSpace(newFactionId))
            {
                Debug.LogWarning(
                    "[FactionVisual] FactionId không hợp lệ.",
                    this);
                return false;
            }

            // Chỉ cập nhật khi tìm thấy controller hợp lệ.
            RuntimeAnimatorController controller =
                GetController(newFactionId);

            if (controller == null)
            {
                Debug.LogWarning(
                    $"[FactionVisual] Chưa cấu hình phe: {newFactionId}",
                    this);
                return false;
            }

            factionId = newFactionId;
            ApplyController(controller);

            return true;
        }

        /// <summary>
        /// Chọn bộ animation tương ứng với phe.
        /// </summary>
        private void ApplyFactionVisual()
        {
            RuntimeAnimatorController controller =
                GetController(factionId);

            if (controller == null)
            {
                Debug.LogWarning(
                    $"[FactionVisual] Thiếu controller cho {factionId}",
                    this);
                return;
            }

            ApplyController(controller);
        }

        private RuntimeAnimatorController GetController(string id)
        {
            switch (id)
            {
                case "Blue":
                    return blueController;

                case "Red":
                    return redController;

                case "Purple":
                    return purpleController;

                case "Yellow":
                    return yellowController;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Chỉ thay controller nếu thực sự cần thiết.
        /// Tránh reset Animator khi không đổi phe.
        /// </summary>
        private void ApplyController(
            RuntimeAnimatorController controller)
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            if (animator.runtimeAnimatorController == controller)
                return;

            animator.runtimeAnimatorController = controller;
        }

#if UNITY_EDITOR
        [ContextMenu("Test Faction Blue")]
        private void TestBlue()
        {
            TestFaction("Blue");
        }

        [ContextMenu("Test Faction Red")]
        private void TestRed()
        {
            TestFaction("Red");
        }

        [ContextMenu("Test Faction Purple")]
        private void TestPurple()
        {
            TestFaction("Purple");
        }

        [ContextMenu("Test Faction Yellow")]
        private void TestYellow()
        {
            TestFaction("Yellow");
        }

        private void TestFaction(string id)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "[FactionVisual] Chỉ test trong Play Mode.",
                    this);
                return;
            }

            SetFaction(id);
        }
#endif
    }
}
