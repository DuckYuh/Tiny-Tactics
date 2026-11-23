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

        [Header("Combat Stats")]
        [Tooltip("Lượng máu tối đa của đơn vị")]
        [Min(1)]
        [SerializeField] private int _maxHP = 100;

        [Tooltip("Sát thương gây ra (hoặc lượng hồi máu đối với Monk)")]
        [Min(0)]
        [SerializeField] private int _damage = 10;

        [Tooltip("Tầm đánh hoặc tầm hồi máu")]
        [Min(0f)]
        [SerializeField] private float _attackRange = 1.2f;

        [Tooltip("Thời gian hồi chiêu giữa các đòn đánh (giây)")]
        [Min(0f)]
        [SerializeField] private float _attackCooldown = 1.2f;

        [Tooltip("Tốc độ di chuyển cơ bản")]
        [Min(0f)]
        [SerializeField] private float _moveSpeed = 3.5f;

        [Tooltip("Chỉ số giáp giảm sát thương nhận vào")]
        [Min(0)]
        [SerializeField] private int _armor = 0;

        [Tooltip("Tầm nhìn mở sương mù (Fog of War)")]
        [Min(0f)]
        [SerializeField] private float _visionRange = 7f;

        // Public Properties
        public string UnitName => unitName;
        public GameObject UnitPrefab => unitPrefab;
        public Sprite UnitIcon => unitIcon;
        public int MeatCost => meatCost;
        public int WoodCost => woodCost;
        public int GoldCost => goldCost;
        public float TrainTime => trainTime;
        public int PopulationCost => populationCost;

        public int MaxHP => _maxHP;
        public int Damage => _damage;
        public float AttackRange => _attackRange;
        public float AttackCooldown => _attackCooldown;
        public float MoveSpeed => _moveSpeed;
        public int Armor => _armor;
        public float VisionRange => _visionRange;

        /// <summary>
        /// Kiểm tra nhanh xem người chơi có đủ tài nguyên để huấn luyện đơn vị này hay không.
        /// </summary>
        public bool CanAfford(Economy.ResourceManager resourceManager)
        {
            if (resourceManager == null) return false;
            return resourceManager.HasEnough(meatCost, woodCost, goldCost);
        }

        /// <summary>
        /// Kiểm tra phe chỉ định có đủ tài nguyên để huấn luyện đơn vị này hay không.
        /// </summary>
        public bool CanAfford(Economy.ResourceManager resourceManager, Combat.Faction faction)
        {
            if (resourceManager == null) return false;
            return resourceManager.HasEnough(faction, meatCost, woodCost, goldCost);
        }
    }
}
