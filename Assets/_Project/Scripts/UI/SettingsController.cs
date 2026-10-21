
using UnityEngine;
using UnityEngine.UI;
using TinyTactics.Audio;

namespace TinyTactics.UI
{
    /// <summary>
    /// Quản lý giao diện Settings và đồng bộ với AudioManager.
    /// </summary>
    public class SettingsController : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;

        [Header("Display Settings")]
        [SerializeField] private Toggle fullscreenToggle;

        private const string FullscreenKey = "Fullscreen";

        private void Start()
        {
            // Đăng ký sự kiện UI.
            masterSlider.onValueChanged.AddListener(OnMasterChanged);
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
            sfxSlider.onValueChanged.AddListener(OnSFXChanged);
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);

            LoadSettings();
        }

        /// <summary>
        /// Đồng bộ UI với cấu hình đã lưu.
        /// </summary>
        private void LoadSettings()
        {
            AudioManager audio = AudioManager.Instance;

            if (audio != null)
            {
                // Không gọi callback khi khởi tạo giá trị.
                masterSlider.SetValueWithoutNotify(
                    audio.GetMasterVolume()
                );

                musicSlider.SetValueWithoutNotify(
                    audio.GetMusicVolume()
                );

                sfxSlider.SetValueWithoutNotify(
                    audio.GetSFXVolume()
                );
            }

            bool fullscreen = PlayerPrefs.GetInt(
                FullscreenKey,
                Screen.fullScreen ? 1 : 0
            ) == 1;

            fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
            Screen.fullScreen = fullscreen;
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

        private void OnFullscreenChanged(bool enabled)
        {
            Screen.fullScreen = enabled;
            PlayerPrefs.SetInt(FullscreenKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void OnDisable()
        {
            // Lưu dữ liệu khi đóng Settings Panel.
            PlayerPrefs.Save();
        }

        private void OnDestroy()
        {
            masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
            musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            sfxSlider.onValueChanged.RemoveListener(OnSFXChanged);
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
        }
    }
}
