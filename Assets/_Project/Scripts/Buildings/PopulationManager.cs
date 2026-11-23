using System;
using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Combat;

namespace TinyTactics.Buildings
{
    /// <summary>
    /// Hệ thống Quản lý Dân số (Population System) theo từng phe.
    /// Phe người chơi (Blue) đồng bộ trực tiếp với HUD qua OnPopulationChanged.
    /// </summary>
    public class PopulationManager : MonoBehaviour
    {
        private class FactionPopulationData
        {
            public int Current;
            public int Max;
        }

        private static PopulationManager _instance;

        private readonly Dictionary<Faction, FactionPopulationData> _factionPopulations =
            new Dictionary<Faction, FactionPopulationData>();

        public static PopulationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<PopulationManager>();
                    if (_instance == null)
                    {
                        GameObject managerObj = new GameObject("[PopulationManager]");
                        _instance = managerObj.AddComponent<PopulationManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Population Configuration")]
        [Tooltip("Giới hạn tối đa trần của game (Hard Cap)")]
        [SerializeField] private int hardCap = 200;

        [Header("Runtime State")]
        [SerializeField] private int currentPopulation = 0;
        [SerializeField] private int maxPopulation = 0;

        /// <summary>
        /// Số lượng dân/quân hiện có của người chơi.
        /// </summary>
        public int CurrentPopulation
        {
            get => currentPopulation;
            private set => currentPopulation = value;
        }

        /// <summary>
        /// Sức chứa tối đa hiện tại (Lâu đài cấp +5, mỗi Nhà dân cấp +5).
        /// </summary>
        public int MaxPopulation
        {
            get => maxPopulation;
            private set => maxPopulation = value;
        }

        /// <summary>
        /// Sự kiện phát ra khi dân số hoặc sức chứa thay đổi (CurrentPopulation, MaxPopulation).
        /// Để UI HUD đăng ký hiển thị dạng "3/10".
        /// </summary>
        public static event Action<int, int> OnPopulationChanged;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void Start()
        {
            // Phát sự kiện ban đầu để UI đồng bộ dữ liệu
            NotifyPopulationChanged();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Tăng sức chứa tối đa khi có công trình hoàn thành (Castle +5, House +5).
        /// </summary>
        /// <param name="amount">Số lượng sức chứa tăng thêm.</param>
        public void AddMaxPopulation(int amount)
        {
            if (amount <= 0) return;

            MaxPopulation = Mathf.Min(MaxPopulation + amount, hardCap);
            Debug.Log($"[PopulationManager] +{amount} Max Population. Hiện tại: {CurrentPopulation}/{MaxPopulation}");
            NotifyPopulationChanged();
        }

        /// <summary>
        /// Giảm sức chứa tối đa khi công trình bị phá hủy (House bị phá hủy -5).
        /// </summary>
        /// <param name="amount">Số lượng sức chứa bị trừ đi.</param>
        public void RemoveMaxPopulation(int amount)
        {
            if (amount <= 0) return;

            MaxPopulation = Mathf.Max(0, MaxPopulation - amount);
            Debug.Log($"[PopulationManager] -{amount} Max Population. Hiện tại: {CurrentPopulation}/{MaxPopulation}");
            NotifyPopulationChanged();
        }

        /// <summary>
        /// Kiểm tra xem có đủ sức chứa để sinh thêm quân hay không.
        /// </summary>
        /// <param name="populationCost">Số dân số tiêu tốn của unit (mặc định = 1).</param>
        /// <returns>True nếu còn chỗ trống, False nếu đã chạm ngưỡng sức chứa.</returns>
        public bool CanSpawnUnit(int populationCost = 1)
        {
            return (CurrentPopulation + populationCost) <= MaxPopulation;
        }

        /// <summary>
        /// Đăng ký tăng dân số khi có unit mới được sinh ra.
        /// </summary>
        /// <param name="cost">Số dân số tiêu hao (mặc định = 1).</param>
        public void RegisterUnit(int cost = 1)
        {
            if (cost <= 0) return;

            CurrentPopulation += cost;
            Debug.Log($"[PopulationManager] Đăng ký Unit (+{cost}). Dân số: {CurrentPopulation}/{MaxPopulation}");
            NotifyPopulationChanged();
        }

        /// <summary>
        /// Hủy đăng ký giảm dân số khi có unit bị tiêu diệt.
        /// </summary>
        /// <param name="cost">Số dân số hoàn lại (mặc định = 1).</param>
        public void UnregisterUnit(int cost = 1)
        {
            if (cost <= 0) return;

            CurrentPopulation = Mathf.Max(0, CurrentPopulation - cost);
            Debug.Log($"[PopulationManager] Hủy đăng ký Unit (-{cost}). Dân số: {CurrentPopulation}/{MaxPopulation}");
            NotifyPopulationChanged();
        }

        /// <summary>
        /// Thiết lập trực tiếp dân số hiện tại (dùng khi khởi tạo scene hoặc load game).
        /// </summary>
        public void SetCurrentPopulation(int amount)
        {
            CurrentPopulation = Mathf.Max(0, amount);
            NotifyPopulationChanged();
        }

        /// <summary>
        /// Phát thông báo cập nhật dân số tới tất cả UI đang lắng nghe.
        /// </summary>
        private void NotifyPopulationChanged()
        {
            OnPopulationChanged?.Invoke(CurrentPopulation, MaxPopulation);
        }

        private FactionPopulationData GetOrCreateFactionData(Faction faction)
        {
            if (!_factionPopulations.TryGetValue(faction, out FactionPopulationData data))
            {
                data = new FactionPopulationData();
                _factionPopulations[faction] = data;
            }

            return data;
        }

        public int GetCurrentPopulation(Faction faction)
        {
            return faction == Faction.Blue
                ? CurrentPopulation
                : GetOrCreateFactionData(faction).Current;
        }

        public int GetMaxPopulation(Faction faction)
        {
            return faction == Faction.Blue
                ? MaxPopulation
                : GetOrCreateFactionData(faction).Max;
        }

        public void AddMaxPopulation(Faction faction, int amount)
        {
            if (faction == Faction.Blue)
            {
                AddMaxPopulation(amount);
                return;
            }

            if (amount <= 0) return;

            FactionPopulationData data = GetOrCreateFactionData(faction);
            data.Max = Mathf.Min(data.Max + amount, hardCap);
        }

        public void RemoveMaxPopulation(Faction faction, int amount)
        {
            if (faction == Faction.Blue)
            {
                RemoveMaxPopulation(amount);
                return;
            }

            if (amount <= 0) return;

            FactionPopulationData data = GetOrCreateFactionData(faction);
            data.Max = Mathf.Max(0, data.Max - amount);
        }

        public bool CanSpawnUnit(Faction faction, int populationCost = 1)
        {
            if (faction == Faction.Blue)
                return CanSpawnUnit(populationCost);

            FactionPopulationData data = GetOrCreateFactionData(faction);
            return (data.Current + populationCost) <= data.Max;
        }

        public void RegisterUnit(Faction faction, int cost = 1)
        {
            if (faction == Faction.Blue)
            {
                RegisterUnit(cost);
                return;
            }

            if (cost <= 0) return;
            GetOrCreateFactionData(faction).Current += cost;
        }

        public void UnregisterUnit(Faction faction, int cost = 1)
        {
            if (faction == Faction.Blue)
            {
                UnregisterUnit(cost);
                return;
            }

            if (cost <= 0) return;
            FactionPopulationData data = GetOrCreateFactionData(faction);
            data.Current = Mathf.Max(0, data.Current - cost);
        }
    }
}
