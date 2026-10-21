
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace TinyTactics.Audio
{
    /// <summary>
    /// Global Audio Manager:
    /// - Phát playlist riêng theo scene.
    /// - Shuffle toàn bộ playlist trước mỗi lượt.
    /// - Không lặp bài cho đến khi hết playlist.
    /// - Quản lý Master/Music/SFX Volume.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private AudioMixerGroup musicGroup;

        [Header("Scene Names")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string gameplaySceneName = "PvE";

        [Header("Main Menu Playlist")]
        [SerializeField] private AudioClip[] mainMenuTracks;

        [Header("Gameplay Playlist")]
        [SerializeField] private AudioClip[] gameplayTracks;

        private AudioSource musicSource;
        private Coroutine playlistCoroutine;

        private AudioClip[] currentPlaylist;

        // Shuffle Bag: chỉ lưu index, không copy AudioClip.
        private int[] shuffledIndices;
        private int nextTrackPosition;
        private int lastPlayedIndex = -1;

        private readonly WaitForSecondsRealtime trackCheckInterval =
            new WaitForSecondsRealtime(0.2f);

        private const string MasterKey = "MasterVolume";
        private const string MusicKey = "MusicVolume";
        private const string SFXKey = "SFXVolume";

        private const float MinimumDecibels = -80f;
        private const float MinimumVolume = 0.0001f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            musicSource = GetComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = false;
            musicSource.spatialBlend = 0f;

            if (musicGroup != null)
                musicSource.outputAudioMixerGroup = musicGroup;

            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private void Start()
        {
            ApplySavedVolumes();
            SelectPlaylist(SceneManager.GetActiveScene().name);
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            SceneManager.activeSceneChanged -= OnSceneChanged;
            Instance = null;
        }

        private void OnSceneChanged(Scene previous, Scene next)
        {
            SelectPlaylist(next.name);
        }

        /// <summary>
        /// Tự chọn playlist theo scene.
        /// </summary>
        private void SelectPlaylist(string sceneName)
        {
            AudioClip[] nextPlaylist = null;

            if (sceneName == mainMenuSceneName)
                nextPlaylist = mainMenuTracks;
            else if (sceneName == gameplaySceneName)
                nextPlaylist = gameplayTracks;

            if (nextPlaylist == null || nextPlaylist.Length == 0)
            {
                StopMusic();
                return;
            }

            if (currentPlaylist == nextPlaylist &&
                playlistCoroutine != null)
            {
                return;
            }

            StopMusic();

            currentPlaylist = nextPlaylist;
            lastPlayedIndex = -1;

            InitializeShuffleBag();

            if (shuffledIndices.Length == 0)
            {
                Debug.LogWarning(
                    "[AudioManager] Playlist không có bài hợp lệ."
                );
                currentPlaylist = null;
                return;
            }

            playlistCoroutine = StartCoroutine(PlayPlaylist());
        }

        /// <summary>
        /// Tạo danh sách index cho các AudioClip hợp lệ.
        /// </summary>
        private void InitializeShuffleBag()
        {
            int validCount = 0;

            for (int i = 0; i < currentPlaylist.Length; i++)
            {
                if (currentPlaylist[i] != null)
                    validCount++;
            }

            shuffledIndices = new int[validCount];

            int position = 0;

            for (int i = 0; i < currentPlaylist.Length; i++)
            {
                if (currentPlaylist[i] != null)
                    shuffledIndices[position++] = i;
            }

            ShuffleTracks();
        }

        /// <summary>
        /// Fisher-Yates Shuffle: O(n).
        /// Chống trùng bài ở ranh giới hai lượt.
        /// </summary>
        private void ShuffleTracks()
        {
            if (shuffledIndices == null ||
                shuffledIndices.Length == 0)
                return;

            int count = shuffledIndices.Length;

            // Xáo trộn trực tiếp mảng index.
            for (int i = count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);

                int temp = shuffledIndices[i];
                shuffledIndices[i] = shuffledIndices[randomIndex];
                shuffledIndices[randomIndex] = temp;
            }

            // Tránh trùng bài cuối lượt trước
            // với bài đầu lượt mới.
            if (count > 1 &&
                shuffledIndices[0] == lastPlayedIndex)
            {
                int swapIndex = Random.Range(1, count);

                int temp = shuffledIndices[0];
                shuffledIndices[0] = shuffledIndices[swapIndex];
                shuffledIndices[swapIndex] = temp;
            }

            nextTrackPosition = 0;
        }

        /// <summary>
        /// Lấy bài tiếp theo trong Shuffle Bag.
        /// Hết lượt thì xáo trộn lại.
        /// </summary>
        private int GetNextTrackIndex()
        {
            if (shuffledIndices == null ||
                shuffledIndices.Length == 0)
                return -1;

            if (nextTrackPosition >= shuffledIndices.Length)
            {
                ShuffleTracks();
            }

            return shuffledIndices[nextTrackPosition++];
        }

        /// <summary>
        /// Phát liên tục cho đến khi chuyển scene.
        /// </summary>
        private IEnumerator PlayPlaylist()
        {
            while (currentPlaylist != null)
            {
                int trackIndex = GetNextTrackIndex();

                if (trackIndex < 0)
                    yield break;

                AudioClip track = currentPlaylist[trackIndex];

                if (track == null)
                    continue;

                lastPlayedIndex = trackIndex;

                musicSource.clip = track;
                musicSource.Play();

                Debug.Log(
                    $"[AudioManager] Playing: {track.name}"
                );

                while (musicSource.isPlaying)
                {
                    yield return trackCheckInterval;
                }
            }
        }

        /// <summary>
        /// Dừng nhạc, xóa trạng thái Shuffle Bag.
        /// </summary>
        public void StopMusic()
        {
            if (playlistCoroutine != null)
            {
                StopCoroutine(playlistCoroutine);
                playlistCoroutine = null;
            }

            if (musicSource != null)
            {
                musicSource.Stop();
                musicSource.clip = null;
            }

            currentPlaylist = null;
            shuffledIndices = null;
            nextTrackPosition = 0;
            lastPlayedIndex = -1;
        }

        // ---------- Volume Settings ----------

        public void SetMasterVolume(float value)
        {
            SetVolume(MasterKey, value);
        }

        public void SetMusicVolume(float value)
        {
            SetVolume(MusicKey, value);
        }

        public void SetSFXVolume(float value)
        {
            SetVolume(SFXKey, value);
        }

        private void SetVolume(string parameter, float value)
        {
            value = Mathf.Clamp01(value);

            if (audioMixer == null)
            {
                Debug.LogError("[AudioManager] Missing AudioMixer.");
                return;
            }

            float decibels = value <= 0f
                ? MinimumDecibels
                : 20f * Mathf.Log10(
                    Mathf.Max(value, MinimumVolume)
                );

            if (!audioMixer.SetFloat(parameter, decibels))
            {
                Debug.LogError(
                    $"[AudioManager] Mixer parameter not found: {parameter}"
                );
                return;
            }

            PlayerPrefs.SetFloat(parameter, value);
        }

        public float GetMasterVolume()
        {
            return PlayerPrefs.GetFloat(MasterKey, 1f);
        }

        public float GetMusicVolume()
        {
            return PlayerPrefs.GetFloat(MusicKey, 1f);
        }

        public float GetSFXVolume()
        {
            return PlayerPrefs.GetFloat(SFXKey, 1f);
        }

        private void ApplySavedVolumes()
        {
            SetMasterVolume(GetMasterVolume());
            SetMusicVolume(GetMusicVolume());
            SetSFXVolume(GetSFXVolume());

            PlayerPrefs.Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                PlayerPrefs.Save();
        }

        private void OnApplicationQuit()
        {
            PlayerPrefs.Save();
        }
    }
}
