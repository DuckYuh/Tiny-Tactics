using System;
using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Units;
using TinyTactics.Economy;

namespace TinyTactics.Buildings
{
    /// <summary>
    /// Công trình Nhà Lính (Barracks) — Kế thừa BuildingBase.
    /// Quản lý việc huấn luyện quân đội (Warrior, Archer, Lancer) theo hàng đợi (Queue System tối đa 5 slot),
    /// tự động khấu trừ/hoàn trả tài nguyên, quản lý dân số và điều phối lính mới sinh di chuyển ra điểm tập kết (Rally Point).
    /// </summary>
    public class BarracksBuilding : BuildingBase
    {
        [Header("Trainable Units Configuration")]
        [Tooltip("Danh sách các loại quân có thể huấn luyện tại Nhà lính này (Binh sĩ, Cung thủ, Lính giáo)")]
        [SerializeField] private List<UnitDataSO> trainableUnits = new List<UnitDataSO>();

        [Header("Spawn & Rally Settings")]
        [Tooltip("Vị trí khởi tạo quân lính mới (nếu để trống sẽ tự tính cạnh nhà lính)")]
        [SerializeField] private Transform spawnPoint;

        [Tooltip("Khoảng cách lệch từ tâm công trình nếu chưa gán spawnPoint")]
        [SerializeField] private float spawnOffsetDistance = 1.8f;

        [Header("Queue Settings")]
        [Tooltip("Số lượng đơn vị tối đa có thể xếp hàng đợi huấn luyện")]
        [SerializeField] private int maxQueueSize = 5;

        // Hàng đợi huấn luyện runtime
        private readonly List<UnitDataSO> _trainingQueue = new List<UnitDataSO>();
        private float _currentTimer = 0f;

        // Public Properties
        public IReadOnlyList<UnitDataSO> TrainableUnits => trainableUnits;
        public IReadOnlyList<UnitDataSO> TrainingQueue => _trainingQueue;
        public int CurrentQueueCount => _trainingQueue.Count;
        public int MaxQueueSize => maxQueueSize;
        public UnitDataSO CurrentTrainingUnit => _trainingQueue.Count > 0 ? _trainingQueue[0] : null;
        public float CurrentTimer => _currentTimer;

        /// <summary>
        /// Tiến độ huấn luyện của đơn vị đầu hàng đợi (từ 0.0f đến 1.0f).
        /// </summary>
        public float CurrentProgress
        {
            get
            {
                if (CurrentTrainingUnit == null || CurrentTrainingUnit.TrainTime <= 0f) return 0f;
                return Mathf.Clamp01(1f - (_currentTimer / CurrentTrainingUnit.TrainTime));
            }
        }

        /// <summary>
        /// Sự kiện phát ra khi tiến độ huấn luyện của đơn vị đang đẻ thay đổi (Progress [0..1], UnitDataSO).
        /// Để HUD UI cập nhật thanh Fill / Progress Bar.
        /// </summary>
        public event Action<float, UnitDataSO> OnTrainingProgressChanged;

        /// <summary>
        /// Sự kiện phát ra mỗi khi danh sách hàng đợi biến động (thêm lính, hủy lính, đẻ xong).
        /// Để UI vẽ lại các slot trong Queue.
        /// </summary>
        public event Action<IReadOnlyList<UnitDataSO>> OnQueueUpdated;

        /// <summary>
        /// Sự kiện phát ra khi một đơn vị quân được huấn luyện xong và xuất hiện trên sân khấu.
        /// </summary>
        public static event Action<BarracksBuilding, GameObject> OnAnyUnitTrained;

        protected override void Awake()
        {
            buildingName = "Barracks";
            if (maxHP <= 500) maxHP = 1200;
            buildingGridSize = new Vector2Int(3, 3);
            cameraShakeDuration = 0.3f;
            cameraShakeMagnitude = 0.2f;

            base.Awake();
        }

        private void Update()
        {
            // Nếu công trình đã bị phá hủy thì dừng toàn bộ quá trình huấn luyện
            if (isDestroyed) return;

            ProcessTrainingQueue();
        }

        /// <summary>
        /// Xử lý đếm ngược thời gian huấn luyện của đơn vị đầu tiên trong hàng đợi.
        /// </summary>
        private void ProcessTrainingQueue()
        {
            if (_trainingQueue.Count == 0)
            {
                _currentTimer = 0f;
                return;
            }

            UnitDataSO currentUnit = _trainingQueue[0];
            if (currentUnit == null)
            {
                _trainingQueue.RemoveAt(0);
                OnQueueUpdated?.Invoke(_trainingQueue);
                return;
            }

            // Đếm ngược thời gian
            _currentTimer -= Time.deltaTime;

            float progress = Mathf.Clamp01(1f - (_currentTimer / currentUnit.TrainTime));
            OnTrainingProgressChanged?.Invoke(progress, currentUnit);

            // Hoàn tất huấn luyện
            if (_currentTimer <= 0f)
            {
                CompleteTraining(currentUnit);
            }
        }

        /// <summary>
        /// Yêu cầu đưa một đơn vị quân vào hàng đợi huấn luyện.
        /// - Kiểm tra số lượng slot trống (tối đa 5).
        /// - Kiểm tra đủ tài nguyên từ ResourceManager (Thịt, Gỗ, Vàng).
        /// - Kiểm tra sức chứa từ PopulationManager.
        /// - Trừ tài nguyên, đăng ký tạm dân số và bắt đầu đếm Cooldown.
        /// </summary>
        /// <param name="unitData">Dữ liệu binh chủng cần huấn luyện.</param>
        /// <returns>True nếu đưa vào hàng đợi thành công, False nếu thất bại.</returns>
        public bool TryQueueUnit(UnitDataSO unitData)
        {
            if (isDestroyed)
            {
                Debug.LogWarning("[Barracks] Không thể mua lính: Công trình đã bị phá hủy.");
                return false;
            }

            if (unitData == null)
            {
                Debug.LogWarning("[Barracks] UnitDataSO không hợp lệ (null).");
                return false;
            }

            // 1. Kiểm tra hàng đợi đầy
            if (_trainingQueue.Count >= maxQueueSize)
            {
                Debug.LogWarning($"[Barracks] Hàng đợi huấn luyện đã đầy ({maxQueueSize}/{maxQueueSize})!");
                return false;
            }

            // 2. Kiểm tra tài nguyên người chơi
            ResourceManager res = ResourceManager.Instance;
            if (res == null || !res.HasEnough(unitData.MeatCost, unitData.WoodCost, unitData.GoldCost))
            {
                Debug.LogWarning($"[Barracks] Không đủ tài nguyên để huấn luyện {unitData.UnitName}! " +
                                 $"Cần: {unitData.MeatCost} Thịt, {unitData.WoodCost} Gỗ, {unitData.GoldCost} Vàng.");
                return false;
            }

            // 3. Kiểm tra dân số còn chỗ trống
            PopulationManager pop = PopulationManager.Instance;
            if (pop != null && !pop.CanSpawnUnit(unitData.PopulationCost))
            {
                Debug.LogWarning($"[Barracks] Đã chạm giới hạn dân số ({pop.CurrentPopulation}/{pop.MaxPopulation})! Hãy xây thêm Nhà dân (House).");
                return false;
            }

            // 4. Khấu trừ tài nguyên ngay lập tức
            if (unitData.MeatCost > 0) res.ConsumeResource(ResourceType.Meat, unitData.MeatCost);
            if (unitData.WoodCost > 0) res.ConsumeResource(ResourceType.Wood, unitData.WoodCost);
            if (unitData.GoldCost > 0) res.ConsumeResource(ResourceType.Gold, unitData.GoldCost);

            // 5. Đăng ký tạm dân số (giữ chỗ trong lúc huấn luyện)
            if (pop != null)
            {
                pop.RegisterUnit(unitData.PopulationCost);
            }

            // 6. Thêm vào hàng đợi
            _trainingQueue.Add(unitData);

            // Nếu đây là đơn vị đầu tiên trong hàng đợi -> thiết lập Cooldown ban đầu
            if (_trainingQueue.Count == 1)
            {
                _currentTimer = unitData.TrainTime;
                OnTrainingProgressChanged?.Invoke(0f, unitData);
            }

            OnQueueUpdated?.Invoke(_trainingQueue);

            Debug.Log($"<color=cyan>[Barracks] Đã đưa <b>{unitData.UnitName}</b> vào hàng đợi. " +
                      $"Vị trí: {_trainingQueue.Count}/{maxQueueSize}. Thời gian: {unitData.TrainTime}s.</color>");
            return true;
        }

        /// <summary>
        /// Hoàn tất thời gian Cooldown huấn luyện đơn vị đầu hàng đợi và tiến hành sinh quân.
        /// </summary>
        private void CompleteTraining(UnitDataSO unitData)
        {
            // Sinh quân trên thế giới
            SpawnUnit(unitData);

            // Xóa khỏi hàng đợi
            _trainingQueue.RemoveAt(0);

            // Chuẩn bị cho đơn vị tiếp theo trong hàng đợi (nếu có)
            if (_trainingQueue.Count > 0)
            {
                UnitDataSO nextUnit = _trainingQueue[0];
                _currentTimer = nextUnit != null ? nextUnit.TrainTime : 0f;
                OnTrainingProgressChanged?.Invoke(0f, nextUnit);
            }
            else
            {
                _currentTimer = 0f;
                OnTrainingProgressChanged?.Invoke(0f, null);
            }

            OnQueueUpdated?.Invoke(_trainingQueue);
        }

        /// <summary>
        /// Khởi tạo Prefab của lính và ra lệnh di chuyển ra điểm tập kết (Rally Point).
        /// </summary>
        private void SpawnUnit(UnitDataSO unitData)
        {
            if (unitData == null || unitData.UnitPrefab == null)
            {
                Debug.LogError($"[Barracks] Không thể sinh quân: Prefab của {unitData?.UnitName} bị null!");
                return;
            }

            Vector3 spawnPos = GetSpawnPosition();

            // 1. Instantiate lính tại điểm Spawn
            GameObject spawnedUnit = Instantiate(unitData.UnitPrefab, spawnPos, Quaternion.identity);
            spawnedUnit.name = $"{unitData.UnitName}_{spawnedUnit.GetInstanceID()}";

            // 2. Ra lệnh cho lính tự động bước ra điểm tập kết (Rally Point)
            UnitController controller = spawnedUnit.GetComponent<UnitController>();
            Vector3 targetRallyPoint = GetRallyPointPosition();

            if (controller != null)
            {
                controller.MoveTo(targetRallyPoint);
                Debug.Log($"<color=green>[Barracks] {unitData.UnitName} đã sinh ra tại {spawnPos} " +
                          $"và tự động di chuyển ra Rally Point: {targetRallyPoint}.</color>");
            }
            else
            {
                Debug.LogWarning($"[Barracks] Unit {spawnedUnit.name} không có component UnitController!");
            }

            OnAnyUnitTrained?.Invoke(this, spawnedUnit);
        }

        /// <summary>
        /// Hủy lệnh huấn luyện tại vị trí chỉ định trong hàng đợi (0-indexed).
        /// Hoàn trả 100% tài nguyên đã trừ và giải phóng dân số đã giữ chỗ.
        /// </summary>
        /// <param name="index">Chỉ số của slot trong hàng đợi cần hủy.</param>
        public void CancelQueue(int index)
        {
            if (index < 0 || index >= _trainingQueue.Count)
            {
                Debug.LogWarning($"[Barracks] Chỉ số hủy {index} nằm ngoài giới hạn hàng đợi ({_trainingQueue.Count})!");
                return;
            }

            UnitDataSO cancelledUnit = _trainingQueue[index];
            if (cancelledUnit == null)
            {
                _trainingQueue.RemoveAt(index);
                OnQueueUpdated?.Invoke(_trainingQueue);
                return;
            }

            // 1. Hoàn lại 100% tài nguyên đã khấu trừ
            ResourceManager res = ResourceManager.Instance;
            if (res != null)
            {
                if (cancelledUnit.MeatCost > 0) res.AddResource(ResourceType.Meat, cancelledUnit.MeatCost);
                if (cancelledUnit.WoodCost > 0) res.AddResource(ResourceType.Wood, cancelledUnit.WoodCost);
                if (cancelledUnit.GoldCost > 0) res.AddResource(ResourceType.Gold, cancelledUnit.GoldCost);
            }

            // 2. Hoàn trả dân số đã giữ chỗ
            PopulationManager pop = PopulationManager.Instance;
            if (pop != null)
            {
                pop.UnregisterUnit(cancelledUnit.PopulationCost);
            }

            // 3. Xóa khỏi hàng đợi
            _trainingQueue.RemoveAt(index);

            // 4. Nếu hủy chính đơn vị đang huấn luyện ở slot đầu tiên -> chuyển sang đơn vị tiếp theo
            if (index == 0)
            {
                if (_trainingQueue.Count > 0)
                {
                    UnitDataSO nextUnit = _trainingQueue[0];
                    _currentTimer = nextUnit != null ? nextUnit.TrainTime : 0f;
                    OnTrainingProgressChanged?.Invoke(0f, nextUnit);
                }
                else
                {
                    _currentTimer = 0f;
                    OnTrainingProgressChanged?.Invoke(0f, null);
                }
            }

            OnQueueUpdated?.Invoke(_trainingQueue);

            Debug.Log($"<color=yellow>[Barracks] Đã hủy huấn luyện <b>{cancelledUnit.UnitName}</b> tại slot {index}. " +
                      $"Hoàn trả 100% chi phí ({cancelledUnit.MeatCost} Thịt, {cancelledUnit.WoodCost} Gỗ, {cancelledUnit.GoldCost} Vàng).</color>");
        }

        /// <summary>
        /// Xác định tọa độ vị trí sinh quân.
        /// </summary>
        public Vector3 GetSpawnPosition()
        {
            if (spawnPoint != null)
            {
                return spawnPoint.position;
            }

            // Vị trí mặc định: Mép phải của Nhà lính
            return transform.position + new Vector3(buildingGridSize.x * 0.5f + spawnOffsetDistance, 0f, 0f);
        }

        /// <summary>
        /// Thiết lập vị trí Spawn point trực tiếp.
        /// </summary>
        public void SetSpawnPoint(Vector3 newWorldPosition)
        {
            if (spawnPoint == null)
            {
                GameObject spawnObj = new GameObject($"{buildingName}_SpawnPoint");
                spawnObj.transform.SetParent(transform);
                spawnPoint = spawnObj.transform;
            }

            spawnPoint.position = newWorldPosition;
        }

        /// <summary>
        /// Hủy toàn bộ hàng đợi và hoàn lại toàn bộ chi phí khi nhà lính bị phá hủy.
        /// </summary>
        private void ClearAndRefundAllQueue()
        {
            while (_trainingQueue.Count > 0)
            {
                CancelQueue(0);
            }
        }

        protected override void OnDestroyed()
        {
            // Hoàn lại tài nguyên và dân số cho toàn bộ quân đang chờ trong queue
            ClearAndRefundAllQueue();

            base.OnDestroyed();
        }

        protected override void OnDestroy()
        {
            ClearAndRefundAllQueue();

            base.OnDestroy();
        }

        private void OnDrawGizmosSelected()
        {
            // Vẽ điểm Spawn
            Gizmos.color = Color.green;
            Vector3 spawnPos = GetSpawnPosition();
            Gizmos.DrawWireSphere(spawnPos, 0.4f);

            // Vẽ điểm Rally Point
            Gizmos.color = Color.yellow;
            Vector3 rallyPos = GetRallyPointPosition();
            Gizmos.DrawWireSphere(rallyPos, 0.4f);

            // Đường nối giữa Spawn và Rally Point
            Gizmos.DrawLine(spawnPos, rallyPos);
        }
    }
}
