
using UnityEngine;
using UnityEngine.SceneManagement;
using TinyTactics.Audio;

namespace TinyTactics.UI
{
    /// <summary>
    /// Điều khiển Main Menu, Settings Panel và SFX.
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
            ShowPanels(false);
        }

        public void PlayPvE()
        {
            if (isLoadingScene)
                return;

            if (!Application.CanStreamedLevelBeLoaded(pveSceneName))
            {
                PlaySFX(SFXType.UIError);

                Debug.LogError(
                    $"[MainMenu] Scene '{pveSceneName}' " +
                    "chưa được thêm vào Build Profiles."
                );
                return;
            }

            PlaySFX(SFXType.UIConfirm);
            isLoadingScene = true;

            SceneManager.LoadScene(pveSceneName);
        }

        public void OpenSettings()
        {
            PlaySFX(SFXType.UIClick);
            ShowPanels(true);
        }

        public void ShowMainMenu()
        {
            PlaySFX(SFXType.UICancel);
            ShowPanels(false);
        }

        private void ShowPanels(bool showSettings)
        {
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(!showSettings);

            if (settingsPanel != null)
                settingsPanel.SetActive(showSettings);
        }

        public void QuitGame()
        {
            PlaySFX(SFXType.UIClick);

            Debug.Log("[MainMenu] Quit Game");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void PlaySFX(SFXType type)
        {
            SFXManager.Instance?.Play(type);
        }
    }
}
