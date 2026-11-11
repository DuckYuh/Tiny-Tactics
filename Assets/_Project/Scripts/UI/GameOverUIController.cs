
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TinyTactics.Core;

namespace TinyTactics.UI
{
    /// <summary>
    /// Hiển thị kết quả trận đấu và điều hướng sau Game Over.
    /// Không can thiệp trực tiếp vào GameManager hoặc Combat.
    /// </summary>
    public sealed class GameOverUIController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TMP_Text resultTitle;
        [SerializeField] private TMP_Text resultDescription;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Scene Configuration")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Result Colors")]
        [SerializeField] private Color victoryColor =
            new Color(0.95f, 0.82f, 0.45f);

        [SerializeField] private Color defeatColor =
            new Color(0.9f, 0.3f, 0.3f);

        private bool hasShownResult;
        private bool isChangingScene;

        private void Awake()
        {
            // Không để kết quả xuất hiện khi mới vào trận.
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
        }

        private void OnEnable()
        {
            // Đăng ký nhận kết quả từ GameManager.
            GameManager.OnGameOver += HandleGameOver;

            if (playAgainButton != null)
                playAgainButton.onClick.AddListener(PlayAgain);

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        private void OnDisable()
        {
            // Hủy đăng ký để tránh giữ tham chiếu tới UI đã bị hủy.
            GameManager.OnGameOver -= HandleGameOver;

            if (playAgainButton != null)
                playAgainButton.onClick.RemoveListener(PlayAgain);

            if (mainMenuButton != null)
                mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
        }

        private void HandleGameOver(bool isVictory)
        {
            if (hasShownResult)
                return;

            hasShownResult = true;

            if (resultTitle != null)
            {
                resultTitle.text = isVictory
                    ? "VICTORY!"
                    : "DEFEAT!";

                resultTitle.color = isVictory
                    ? victoryColor
                    : defeatColor;
            }

            if (resultDescription != null)
            {
                resultDescription.text = isVictory
                    ? "Enemy Castle Destroyed"
                    : "Your Castle Has Fallen";
            }

            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);

            // Dừng simulation sau khi đã hiển thị kết quả.
            Time.timeScale = 0f;
        }

        public void PlayAgain()
        {
            if (isChangingScene)
                return;

            isChangingScene = true;
            Time.timeScale = 1f;

            // Tải lại scene gameplay hiện tại.
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }

        public void ReturnToMainMenu()
        {
            if (isChangingScene)
                return;

            if (!Application.CanStreamedLevelBeLoaded(
                    mainMenuSceneName))
            {
                Debug.LogError(
                    $"[GameOverUI] Scene '{mainMenuSceneName}' " +
                    "chưa được thêm vào Build Profiles."
                );
                return;
            }

            isChangingScene = true;
            Time.timeScale = 1f;

            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
