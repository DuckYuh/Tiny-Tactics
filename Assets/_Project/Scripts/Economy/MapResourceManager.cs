using System.Collections.Generic;
using UnityEngine;

namespace TinyTactics.Economy
{
    public class MapResourceManager : MonoBehaviour
    {
        public static MapResourceManager Instance { get; private set; }

        private List<ResourceDeposit> allResources = new List<ResourceDeposit>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // Tự động tìm tất cả tài nguyên có sẵn trên bản đồ (Gỗ, Vàng, Cừu ban đầu)
            ResourceDeposit[] deposits = FindObjectsOfType<ResourceDeposit>();
            foreach (var deposit in deposits)
            {
                RegisterResource(deposit);
            }
        }

        public void RegisterResource(ResourceDeposit deposit)
        {
            if (!allResources.Contains(deposit))
            {
                allResources.Add(deposit);
                deposit.OnDepleted += HandleResourceDepleted;
            }
        }

        public void UnregisterResource(ResourceDeposit deposit)
        {
            if (allResources.Contains(deposit))
            {
                allResources.Remove(deposit);
                deposit.OnDepleted -= HandleResourceDepleted;
            }
        }

        private void HandleResourceDepleted(ResourceDeposit deposit)
        {
            UnregisterResource(deposit);
        }

        /// <summary>
        /// Tìm tài nguyên gần nhất theo loại và bán kính tối đa
        /// </summary>
        public ResourceDeposit GetClosestResource(Vector3 position, ResourceType type, float maxSearchRadius)
        {
            ResourceDeposit closest = null;
            float minDistanceSqr = maxSearchRadius * maxSearchRadius;

            foreach (var deposit in allResources)
            {
                if (deposit.resourceType != type || deposit.currentAmount <= 0)
                {
                    continue;
                }

                float distanceSqr = (deposit.transform.position - position).sqrMagnitude;
                if (distanceSqr < minDistanceSqr)
                {
                    minDistanceSqr = distanceSqr;
                    closest = deposit;
                }
            }

            return closest;
        }
    }
}
