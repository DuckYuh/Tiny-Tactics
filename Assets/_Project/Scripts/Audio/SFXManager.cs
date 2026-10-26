
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace TinyTactics.Audio
{
    /// <summary>
    /// Danh sách SFX được hỗ trợ trong Tiny Tactics.
    /// Có thể mở rộng khi bổ sung gameplay.
    /// </summary>
    public enum SFXType
    {
        UIClick,
        UIHover,
        UIConfirm,
        UICancel,
        UIError,
        UINotification,

        UnitSelect,
        UnitMove,

        WoodChop,
        GoldMine,
        FoodGather,

        BuildingPlace,
        BuildingDestroy,

        GameStart
    }

    /// <summary>
    /// Cấu hình riêng cho từng loại âm thanh.
    /// </summary>
    [Serializable]
    public class SFXEntry
    {
        public SFXType type;
        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 1f;

        [Min(0f)]
        public float cooldown = 0.05f;
    }

    /// <summary>
    /// Quản lý hiệu ứng âm thanh:
    /// - AudioSource Pooling
    /// - Cooldown chống spam
    /// - Giới hạn âm thanh đồng thời
    /// - AudioMixer dùng chung với AudioManager
    /// - Hỗ trợ phát 2D và giảm âm lượng theo khoảng cách
    /// </summary>
    public class SFXManager : MonoBehaviour
    {
        public static SFXManager Instance { get; private set; }

        [Header("Audio Mixer")]
        [SerializeField]
        private AudioMixerGroup sfxMixerGroup;

        [Header("Pool Configuration")]
        [SerializeField, Min(1)]
        private int poolSize = 12;

        [Header("World Sound")]
        [SerializeField, Min(0.1f)]
        private float maxHearingDistance = 20f;

        [Header("Sound Library")]
        [SerializeField]
        private SFXEntry[] soundEntries;

        private readonly Dictionary<SFXType, SFXEntry> soundLibrary =
            new Dictionary<SFXType, SFXEntry>();

        private readonly Dictionary<SFXType, float> lastPlayTimes =
            new Dictionary<SFXType, float>();

        private AudioSource[] sourcePool;
        private Transform poolRoot;

        private Camera mainCamera;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            BuildSoundLibrary();
            InitializePool();

            SceneManager.activeSceneChanged += OnSceneChanged;
            RefreshCamera();
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
            RefreshCamera();
        }

        private void RefreshCamera()
        {
            mainCamera = Camera.main;
        }

        /// <summary>
        /// Lưu cấu hình sound trong Dictionary
        /// để truy xuất O(1) trung bình.
        /// </summary>
        private void BuildSoundLibrary()
        {
            soundLibrary.Clear();

            if (soundEntries == null)
                return;

            foreach (SFXEntry entry in soundEntries)
            {
                if (entry == null || entry.clip == null)
                    continue;

                if (soundLibrary.ContainsKey(entry.type))
                {
                    Debug.LogWarning(
                        $"[SFXManager] Duplicate SFX: {entry.type}"
                    );
                    continue;
                }

                soundLibrary.Add(entry.type, entry);
            }
        }

        /// <summary>
        /// Khởi tạo một lần các AudioSource.
        /// Không Instantiate/Destroy khi phát SFX.
        /// </summary>
        private void InitializePool()
        {
            poolRoot = new GameObject("SFXPool").transform;
            poolRoot.SetParent(transform, false);

            sourcePool = new AudioSource[poolSize];

            for (int i = 0; i < poolSize; i++)
            {
                GameObject sourceObject =
                    new GameObject($"SFXSource_{i:00}");

                sourceObject.transform.SetParent(poolRoot, false);

                AudioSource source =
                    sourceObject.AddComponent<AudioSource>();

                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                source.volume = 1f;

                source.outputAudioMixerGroup = sfxMixerGroup;

                sourcePool[i] = source;
            }
        }

        /// <summary>
        /// Tìm AudioSource chưa sử dụng.
        /// Nếu pool đầy, bỏ qua âm thanh mới.
        /// </summary>
        private AudioSource GetAvailableSource()
        {
            foreach (AudioSource source in sourcePool)
            {
                if (!source.isPlaying)
                    return source;
            }

            return null;
        }

        /// <summary>
        /// Kiểm tra giới hạn phát âm thanh theo loại.
        /// </summary>
        private bool CanPlay(SFXType type, float cooldown)
        {
            float currentTime = Time.unscaledTime;

            if (lastPlayTimes.TryGetValue(type, out float lastTime))
            {
                if (currentTime - lastTime < cooldown)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Phát âm thanh 2D:
        /// UI, lựa chọn unit, thông báo...
        /// </summary>
        public bool Play(SFXType type)
        {
            return PlayInternal(type, 1f);
        }

        /// <summary>
        /// Phát âm thanh tại vị trí thế giới.
        /// Giảm âm lượng theo khoảng cách từ camera.
        /// Phù hợp với game RTS 2D.
        /// </summary>
        public bool PlayAtPosition(
            SFXType type,
            Vector3 worldPosition)
        {
            if (mainCamera == null)
                RefreshCamera();

            if (mainCamera == null)
                return Play(type);

            Vector3 cameraPosition =
                mainCamera.transform.position;

            // Chỉ tính khoảng cách trên mặt phẳng XY.
            float distance = Vector2.Distance(
                new Vector2(worldPosition.x, worldPosition.y),
                new Vector2(cameraPosition.x, cameraPosition.y)
            );

            float attenuation =
                Mathf.Clamp01(1f - distance / maxHearingDistance);

            if (attenuation <= 0f)
                return false;

            return PlayInternal(type, attenuation);
        }

        /// <summary>
        /// Xử lý phát SFX dùng chung.
        /// </summary>
        private bool PlayInternal(
            SFXType type,
            float attenuation)
        {
            if (!soundLibrary.TryGetValue(
                    type,
                    out SFXEntry entry))
            {
                return false;
            }

            if (!CanPlay(type, entry.cooldown))
                return false;

            AudioSource source = GetAvailableSource();

            if (source == null)
                return false;

            source.clip = entry.clip;
            source.volume =
                Mathf.Clamp01(entry.volume * attenuation);

            source.Play();

            lastPlayTimes[type] = Time.unscaledTime;

            return true;
        }

        /// <summary>
        /// Dừng toàn bộ SFX đang phát.
        /// Không ảnh hưởng nhạc nền.
        /// </summary>
        public void StopAllSFX()
        {
            if (sourcePool == null)
                return;

            foreach (AudioSource source in sourcePool)
            {
                source.Stop();
                source.clip = null;
            }
        }
    }
}
