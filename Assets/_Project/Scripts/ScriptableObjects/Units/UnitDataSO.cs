using UnityEngine;

namespace TinyTactics.Units
{
    /// <summary>
    /// ScriptableObject định nghĩa thông số dữ liệu cho từng loại đơn vị quân (Binh sĩ, Cung thủ, Lính giáo,...).
    /// Chứa thông tin chi phí tài nguyên, thời gian huấn luyện (Cooldown), dân số tiêu tốn và Prefab tương ứng.
    /// </summary>
    [CreateAssetMenu(fileName = "NewUnitData", menuName = "Tiny Tactics/Unit Data")]
    public class UnitDataSO : ScriptableObject
    {
        [Header("General Info")]
        [Tooltip("Tên định danh binh chủng (vd: Warrior, Archer, Lancer)")]
        [SerializeField] private string unitName = "Warrior";

        [Tooltip("Prefab của đơn vị (chứa UnitController, Collider2D, Animator,...)")]
        [SerializeField] private GameObject unitPrefab;

        [Tooltip("Icon đại diện hiển thị trên nút bấm UI và hàng đợi huấn luyện")]
        [SerializeField] private Sprite unitIcon;

        [Header("Resource Costs")]
        [Tooltip("Chi phí Thịt (Meat)")]
        [Min(0)]
        [SerializeField] private int meatCost = 60;

        [Tooltip("Chi phí Gỗ (Wood)")]
        [Min(0)]
        [SerializeField] private int woodCost = 0;

        [Tooltip("Chi phí Vàng (Gold)")]
        [Min(0)]
        [SerializeField] private int goldCost = 0;

        [Header("Training & Population")]
        [Tooltip("Thời gian huấn luyện / Cooldown tính bằng giây")]
        [Min(0.1f)]
        [SerializeField] private float trainTime = 5.0f;

        [Tooltip("Số dân số tiêu tốn khi huấn luyện đơn vị này (mặc định = 1)")]
        [Min(1)]
        [SerializeField] private int populationCost = 1;

        // Public Properties
        public string UnitName => unitName;
        public GameObject UnitPrefab => unitPrefab;
        public Sprite UnitIcon => unitIcon;
        public int MeatCost => meatCost;
        public int WoodCost => woodCost;
        public int GoldCost => goldCost;
        public float TrainTime => trainTime;
        public int PopulationCost => populationCost;

        /// <summary>
        /// Kiểm tra nhanh xem người chơi có đủ tài nguyên để huấn luyện đơn vị này hay không.
        /// </summary>
        public bool CanAfford(Economy.ResourceManager resourceManager)
        {
            if (resourceManager == null) return false;
            return resourceManager.HasEnough(meatCost, woodCost, goldCost);
        }
    }
}
