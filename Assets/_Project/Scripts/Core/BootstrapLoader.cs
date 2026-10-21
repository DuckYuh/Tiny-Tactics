
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TinyTactics.Core
{
    /// <summary>
    /// Khởi tạo game và tải MainMenu bất đồng bộ.
    /// Hiển thị Loading Screen trong quá trình tải.
    /// </summary>
    public class BootstrapLoader : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField]
        private string mainMenuSceneName = "MainMenu";

        [Header("Loading UI")]
        [SerializeField]
        private Image progressBarFill;

        [SerializeField]
        private TMP_Text progressPercentText;

        [Header("Loading Settings")]
        [SerializeField, Min(0f)]
        private float minimumDisplaySeconds = 1.5f;

        [SerializeField, Min(0.1f)]
        private float progressAnimationSpeed = 1f;

        private bool isLoading;
        private int lastDisplayedPercent = -1;

        private void Start()
        {
            StartCoroutine(LoadMainMenu());
        }

        /// <summary>
        /// Tải MainMenu, chờ hoàn tất và kích hoạt scene.
        /// </summary>
        private IEnumerator LoadMainMenu()
        {
            if (isLoading)
                yield break;

            isLoading = true;
            UpdateProgress(0f);

            if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Debug.LogError(
                    $"[Bootstrap] Scene '{mainMenuSceneName}' " +
                    "chưa được thêm vào Scene List."
                );
                isLoading = false;
                yield break;
            }

            // Cho Unity render Loading Screen ít nhất 1 frame.
            yield return null;

            float startTime = Time.unscaledTime;

            AsyncOperation operation =
                SceneManager.LoadSceneAsync(mainMenuSceneName);

            if (operation == null)
            {
                Debug.LogError("[Bootstrap] Không thể tải MainMenu.");
                isLoading = false;
                yield break;
            }

            // Chưa cho phép chuyển scene ngay.
            operation.allowSceneActivation = false;

            float displayedProgress = 0f;

            while (true)
            {
                // Unity báo progress tối đa 0.9
                // khi chưa cho phép kích hoạt scene.
                float realProgress =
                    Mathf.Clamp01(operation.progress / 0.9f);

                // Giữ 5% cuối cho giai đoạn kích hoạt.
                float targetProgress = realProgress * 0.95f;

                displayedProgress = Mathf.MoveTowards(
                    displayedProgress,
                    targetProgress,
                    progressAnimationSpeed * Time.unscaledDeltaTime
                );

                UpdateProgress(displayedProgress);

                bool sceneReady = operation.progress >= 0.9f;

                bool minimumTimeReached =
                    Time.unscaledTime - startTime >=
                    minimumDisplaySeconds;

                if (sceneReady && minimumTimeReached)
                    break;

                yield return null;
            }

            // Scene đã sẵn sàng để kích hoạt.
            UpdateProgress(1f);

            // Hiển thị trạng thái hoàn tất ít nhất 1 frame.
            yield return null;

            operation.allowSceneActivation = true;
        }

        /// <summary>
        /// Cập nhật thanh Loading và phần trăm.
        /// </summary>
        private void UpdateProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);

            if (progressBarFill != null)
                progressBarFill.fillAmount = progress;

            int percent = Mathf.RoundToInt(progress * 100f);

            // Chỉ cập nhật TMP khi giá trị thay đổi.
            if (progressPercentText != null &&
                percent != lastDisplayedPercent)
            {
                progressPercentText.text = $"{percent}%";
                lastDisplayedPercent = percent;
            }
        }
    }
}
