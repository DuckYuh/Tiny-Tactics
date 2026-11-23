using System;
using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Combat;

namespace TinyTactics.Economy
{
    /// <summary>
    /// Singleton quản lý kho dự trữ tài nguyên cho phe người chơi (Blue) và các phe AI (Red,...).
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        [Header("AI Starting Resources")]
        [SerializeField] private int _aiStartingMeat = 100;
        [SerializeField] private int _aiStartingWood = 150;
        [SerializeField] private int _aiStartingGold = 50;

        private Dictionary<ResourceType, int> resources;
        private readonly Dictionary<Faction, Dictionary<ResourceType, int>> _factionResources =
            new Dictionary<Faction, Dictionary<ResourceType, int>>();

        // Sự kiện báo cho HUD UI khi tài nguyên của người chơi Blue thay đổi
        public event Action<ResourceType, int> OnResourceChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Khởi tạo kho mặc định cho người chơi Blue
            resources = new Dictionary<ResourceType, int>
            {
                { ResourceType.Meat, 100 },
                { ResourceType.Wood, 150 },
                { ResourceType.Gold, 50 }
            };
        }

        private Dictionary<ResourceType, int> GetOrCreateFactionStock(Faction faction)
        {
            if (faction == Faction.Blue)
                return resources;

            if (!_factionResources.TryGetValue(faction, out Dictionary<ResourceType, int> stock))
            {
                stock = new Dictionary<ResourceType, int>
                {
                    { ResourceType.Meat, _aiStartingMeat },
                    { ResourceType.Wood, _aiStartingWood },
                    { ResourceType.Gold, _aiStartingGold }
                };
                _factionResources[faction] = stock;
            }

            return stock;
        }

        public void AddResource(Faction faction, ResourceType type, int amount)
        {
            if (faction == Faction.Blue)
            {
                AddResource(type, amount);
                return;
            }

            if (amount <= 0) return;

            Dictionary<ResourceType, int> stock = GetOrCreateFactionStock(faction);
            stock[type] = stock.TryGetValue(type, out int current) ? current + amount : amount;
        }

        public int GetResourceAmount(Faction faction, ResourceType type)
        {
            if (faction == Faction.Blue)
                return GetResourceAmount(type);

            Dictionary<ResourceType, int> stock = GetOrCreateFactionStock(faction);
            return stock.TryGetValue(type, out int amount) ? amount : 0;
        }

        public bool ConsumeResource(Faction faction, ResourceType type, int amount)
        {
            if (faction == Faction.Blue)
                return ConsumeResource(type, amount);

            if (amount <= 0) return true;

            Dictionary<ResourceType, int> stock = GetOrCreateFactionStock(faction);
            if (GetResourceAmount(faction, type) >= amount)
            {
                stock[type] -= amount;
                return true;
            }

            return false;
        }

        public bool HasEnough(Faction faction, int meat, int wood, int gold)
        {
            return GetResourceAmount(faction, ResourceType.Meat) >= meat &&
                   GetResourceAmount(faction, ResourceType.Wood) >= wood &&
                   GetResourceAmount(faction, ResourceType.Gold) >= gold;
        }

        /// <summary>
        /// Nông dân gọi để nộp tài nguyên.
        /// </summary>
        public void AddResource(ResourceType type, int amount)
        {
            if (amount <= 0) return;

            if (resources.ContainsKey(type))
            {
                resources[type] += amount;
            }
            else
            {
                resources[type] = amount;
            }

            OnResourceChanged?.Invoke(type, resources[type]);
        }

        /// <summary>
        /// Lấy số lượng tài nguyên hiện tại.
        /// </summary>
        public int GetResourceAmount(ResourceType type)
        {
            if (resources.TryGetValue(type, out int amount))
            {
                return amount;
            }
            return 0;
        }

        /// <summary>
        /// Sử dụng khi xây dựng hoặc mua lính.
        /// </summary>
        public bool ConsumeResource(ResourceType type, int amount)
        {
            if (GetResourceAmount(type) >= amount)
            {
                resources[type] -= amount;
                OnResourceChanged?.Invoke(type, resources[type]);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Kiểm tra xem người chơi có đủ đồng thời 3 loại tài nguyên hay không.
        /// Thường dùng trước khi mua quân hoặc công trình.
        /// </summary>
        public bool HasEnough(int meat, int wood, int gold)
        {
            return GetResourceAmount(ResourceType.Meat) >= meat &&
                   GetResourceAmount(ResourceType.Wood) >= wood &&
                   GetResourceAmount(ResourceType.Gold) >= gold;
        }
    }
}
