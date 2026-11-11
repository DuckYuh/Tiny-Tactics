
using UnityEngine;
using UnityEngine.InputSystem;
using TinyTactics.Core;

namespace TinyTactics.Test
{
    /// <summary>
    /// Kiểm thử Win/Loss UI độc lập với AI và Combat.
    /// Phím F8: Victory.
    /// Phím F9: Defeat.
    /// Chỉ sử dụng trong Editor.
    /// </summary>
    public sealed class GameOverUITest : MonoBehaviour
    {
#if UNITY_EDITOR
        private void Update()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.f8Key.wasPressedThisFrame)
                GameManager.TriggerPlayerVictory();

            if (Keyboard.current.f9Key.wasPressedThisFrame)
                GameManager.TriggerPlayerDefeated();
        }
#endif
    }
}
