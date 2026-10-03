using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinyTactics.Economy
{
    /// <summary>
    /// Singleton quản lý kho dự trữ tài nguyên của người chơi.
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        private Dictionary<ResourceType, int> resources;

        // Sự kiện báo cho HUD UI khi tài nguyên thay đổi
        public event Action<ResourceType, int> OnResourceChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // Khởi tạo kho mặc định
            resources = new Dictionary<ResourceType, int>
            {
                { ResourceType.Meat, 0 },
                { ResourceType.Wood, 0 },
                { ResourceType.Gold, 0 }
            };
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
    }
}
