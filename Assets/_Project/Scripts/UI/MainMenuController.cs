
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TinyTactics.UI
{
    /// <summary>
    /// Điều khiển giao diện Main Menu và chuyển sang PvE.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField]
        private string pveSceneName = "PvE";

        [Header("UI References")]
        [SerializeField]
        private GameObject mainMenuPanel;

        [SerializeField]
        private GameObject settingsPanel;

        private bool isLoadingScene;

        private void Start()
        {
            ShowMainMenu();
        }

        /// <summary>
        /// Chuyển sang scene PvE.
        /// </summary>
        public void PlayPvE()
        {
            if (isLoadingScene)
                return;

            if (!Application.CanStreamedLevelBeLoaded(pveSceneName))
            {
                Debug.LogError(
                    $"[MainMenu] Scene '{pveSceneName}' " +
                    "chưa được thêm vào Build Profiles."
                );
                return;
            }

            isLoadingScene = true;
            SceneManager.LoadScene(pveSceneName);
        }

        /// <summary>
        /// Hiển thị bảng Settings.
        /// </summary>
        public void OpenSettings()
        {
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(false);

            if (settingsPanel != null)
                settingsPanel.SetActive(true);
        }

        /// <summary>
        /// Quay lại Main Menu từ Settings.
        /// </summary>
        public void ShowMainMenu()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);

            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(true);
        }

        /// <summary>
        /// Thoát ứng dụng.
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[MainMenu] Quit Game");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
