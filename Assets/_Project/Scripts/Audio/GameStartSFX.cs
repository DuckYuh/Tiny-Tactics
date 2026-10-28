
using System.Collections;
using UnityEngine;

namespace TinyTactics.Audio
{
    /// <summary>
    /// Phát âm thanh bắt đầu trận đấu.
    /// Gắn vào một GameObject trong scene PvE.
    /// </summary>
    public sealed class GameStartSFX : MonoBehaviour
    {
        private IEnumerator Start()
        {
            // Chờ một frame để các component trong scene
            // hoàn tất quá trình khởi tạo ban đầu.
            yield return null;

            SFXManager.Instance?.Play(SFXType.GameStart);
        }
    }
}
