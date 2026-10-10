using UnityEngine;
using TinyTactics.Pathfinding;
using System.Collections.Generic;

namespace TinyTactics.Economy
{
    public class SheepSpawner : MonoBehaviour
    {
        [Header("Spawner Configuration")]
        [Tooltip("Prefab cừu (chứa script ResourceDeposit)")]
        [SerializeField] private GameObject sheepPrefab;
        
        [Tooltip("Thời gian giữa mỗi chu kỳ spawn (giây)")]
        [SerializeField] private float spawnInterval = 30f;
        
        [Tooltip("Giới hạn tối đa số cừu tồn tại cùng lúc")]
        [SerializeField] private int maxActiveSheep = 15;
        
        [Tooltip("Bán kính spawn xung quanh điểm đặt Spawner")]
        [SerializeField] private float spawnRadius = 5f;

        private int currentActiveSheep = 0;
        private float nextSpawnTime;

        private void Start()
        {
            // Có thể spawn ngay khi bắt đầu hoặc chờ hết khoảng thời gian đầu tiên
            nextSpawnTime = Time.time + spawnInterval;
        }

        private void Update()
        {
            if (Time.time >= nextSpawnTime)
            {
                if (currentActiveSheep < maxActiveSheep)
                {
                    TrySpawnSheep();
                }
                nextSpawnTime = Time.time + spawnInterval;
            }
        }

        private void TrySpawnSheep()
        {
            if (sheepPrefab == null)
            {
                Debug.LogWarning("SheepSpawner: Chưa gán sheepPrefab!");
                return;
            }

            // Thử ngẫu nhiên tối đa 10 lần để tìm vị trí hợp lệ
            for (int i = 0; i < 10; i++)
            {
                Vector2 randomPoint = Random.insideUnitCircle * spawnRadius;
                Vector3 spawnPos = transform.position + new Vector3(randomPoint.x, randomPoint.y, 0);

                if (PathfindingGrid.Instance != null)
                {
                    PathNode node = PathfindingGrid.Instance.GetNodeFromWorldPosition(spawnPos);
                    
                    // Nếu node hợp lệ và không có vật cản (isWalkable == true)
                    if (node != null && node.isWalkable)
                    {
                        SpawnSheepAt(node.worldPosition);
                        return; // Spawn thành công thì dừng lại
                    }
                }
            }
            
            // Nếu sau 10 lần không tìm được chỗ trống, bỏ qua chu kỳ này
        }

        private void SpawnSheepAt(Vector3 position)
        {
            GameObject newSheep = Instantiate(sheepPrefab, position, Quaternion.identity, transform);
            ResourceDeposit deposit = newSheep.GetComponent<ResourceDeposit>();
            
            if (deposit != null)
            {
                currentActiveSheep++;
                // Lắng nghe sự kiện cạn kiệt để giảm số lượng cừu hiện tại
                deposit.OnDepleted += HandleSheepDepleted;
                
                // Chủ động đăng ký với MapResourceManager để Nông dân có thể nhận diện ngay
                if (MapResourceManager.Instance != null)
                {
                    MapResourceManager.Instance.RegisterResource(deposit);
                }
            }
            else
            {
                Debug.LogError("SheepSpawner: Prefab cừu không có script ResourceDeposit!");
            }
        }

        private void HandleSheepDepleted(ResourceDeposit deposit)
        {
            currentActiveSheep--;
            deposit.OnDepleted -= HandleSheepDepleted;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }
    }
}
