
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TinyTactics.Audio;
using TinyTactics.Core;

namespace TinyTactics.UI
{
    /// <summary>
    /// Điều khiển Pause Menu trong gameplay PvE.
    /// Tái sử dụng AudioManager, không sửa các hệ thống cũ.
    /// </summary>
    public sealed class PauseMenuController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Audio Sliders")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;

        [Header("Scene Configuration")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private bool isPaused;
        private bool isChangingScene;

        private void Awake()
        {
            // Manager phải luôn Active, kể cả khi panel bị ẩn.
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
        }

        private void OnEnable()
        {
            settingsButton.onClick.AddListener(OpenPauseMenu);
            continueButton.onClick.AddListener(ContinueGame);
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);

            masterSlider.onValueChanged.AddListener(OnMasterChanged);
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
            sfxSlider.onValueChanged.AddListener(OnSFXChanged);

            // Đóng Pause Menu nếu Game Over xảy ra.
            GameManager.OnGameOver += HandleGameOver;
        }

        private void Start()
        {
            SyncVolumeSliders();
        }

        private void OnDisable()
        {
            settingsButton.onClick.RemoveListener(OpenPauseMenu);
            continueButton.onClick.RemoveListener(ContinueGame);
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);

            masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
            musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            sfxSlider.onValueChanged.RemoveListener(OnSFXChanged);

            GameManager.OnGameOver -= HandleGameOver;
        }

        /// <summary>
        /// Hiển thị Pause Menu và dừng simulation.
        /// </summary>
        public void OpenPauseMenu()
        {
            if (isChangingScene || isPaused)
                return;

            if (GameManager.Instance.IsGameOver)
                return;

            isPaused = true;

            SyncVolumeSliders();

            pauseMenuPanel.SetActive(true);
            Time.timeScale = 0f;

            SFXManager.Instance?.Play(SFXType.UIClick);
        }

        /// <summary>
        /// Đóng Pause Menu và tiếp tục gameplay.
        /// </summary>
        public void ContinueGame()
        {
            if (!isPaused || isChangingScene)
                return;

            isPaused = false;
            pauseMenuPanel.SetActive(false);

            // Chỉ tiếp tục khi trận đấu chưa kết thúc.
            if (!GameManager.Instance.IsGameOver)
                Time.timeScale = 1f;

            PlayerPrefs.Save();

            SFXManager.Instance?.Play(SFXType.UIConfirm);
        }

        /// <summary>
        /// Trở về Main Menu từ Pause Menu.
        /// </summary>
        public void ReturnToMainMenu()
        {
            if (isChangingScene)
                return;

            if (!Application.CanStreamedLevelBeLoaded(
                    mainMenuSceneName))
            {
                Debug.LogError(
                    $"[PauseMenu] Scene '{mainMenuSceneName}' " +
                    "chưa có trong Build Profiles."
                );
                return;
            }

            isChangingScene = true;

            PlayerPrefs.Save();
            Time.timeScale = 1f;

            SceneManager.LoadScene(mainMenuSceneName);
        }

        /// <summary>
        /// Đóng Settings khi trận kết thúc.
        /// Không tự Resume gameplay.
        /// </summary>
        private void HandleGameOver(bool isVictory)
        {
            isPaused = false;

            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);

            if (settingsButton != null)
                settingsButton.interactable = false;
        }

        /// <summary>
        /// Đồng bộ các Slider với âm lượng đã lưu.
        /// </summary>
        private void SyncVolumeSliders()
        {
            AudioManager audio = AudioManager.Instance;

            if (audio == null)
                return;

            masterSlider.SetValueWithoutNotify(
                audio.GetMasterVolume());

            musicSlider.SetValueWithoutNotify(
                audio.GetMusicVolume());

            sfxSlider.SetValueWithoutNotify(
                audio.GetSFXVolume());
        }

        private void OnMasterChanged(float value)
        {
            AudioManager.Instance?.SetMasterVolume(value);
        }

        private void OnMusicChanged(float value)
        {
            AudioManager.Instance?.SetMusicVolume(value);
        }

        private void OnSFXChanged(float value)
        {
            AudioManager.Instance?.SetSFXVolume(value);
        }
    }
}
