using System;
using UnityEngine;

namespace TinyTactics.Core
{
    /// <summary>
    /// Quản lý vòng đời trận đấu, trạng thái Thắng / Thua (Victory / Defeat).
    /// Áp dụng mô hình Singleton với cơ chế tự khởi tạo (Auto-create) nếu chưa có trong Scene.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;

        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameManager>();
                    if (_instance == null)
                    {
                        GameObject managerObj = new GameObject("[GameManager]");
                        _instance = managerObj.AddComponent<GameManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        /// <summary>
        /// Bắn ra khi Lâu đài của Player bị hủy -> Kích hoạt Thua cuộc (Defeat).
        /// </summary>
        public static event Action OnPlayerDefeated;

        /// <summary>
        /// Bắn ra khi Lâu đài của Địch (Red) bị hủy -> Kích hoạt Chiến thắng (Victory).
        /// </summary>
        public static event Action OnPlayerVictory;

        /// <summary>
        /// Bắn ra khi trận đấu kết thúc (true: Thắng, false: Thua).
        /// </summary>
        public static event Action<bool> OnGameOver;

        [Header("Game State")]
        [SerializeField] private bool isGameOver = false;

        public bool IsGameOver => isGameOver;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Kích hoạt Thua cuộc khi Lâu đài của Player bị tiêu diệt.
        /// </summary>
        public static void TriggerPlayerDefeated()
        {
            GameManager gm = Instance;
            if (gm != null)
            {
                if (gm.isGameOver) return;
                gm.isGameOver = true;
            }

            Debug.Log("<color=red><b>[GameManager] THUA CUỘC (DEFEAT)!</b> Lâu đài của Player đã bị phá hủy.</color>");
            OnPlayerDefeated?.Invoke();
            OnGameOver?.Invoke(false);
        }

        /// <summary>
        /// Kích hoạt Chiến thắng khi Lâu đài của Đối phương bị tiêu diệt.
        /// </summary>
        public static void TriggerPlayerVictory()
        {
            GameManager gm = Instance;
            if (gm != null)
            {
                if (gm.isGameOver) return;
                gm.isGameOver = true;
            }

            Debug.Log("<color=green><b>[GameManager] CHIẾN THẮNG (VICTORY)!</b> Lâu đài của kẻ địch đã bị tiêu diệt.</color>");
            OnPlayerVictory?.Invoke();
            OnGameOver?.Invoke(true);
        }
    }
}
