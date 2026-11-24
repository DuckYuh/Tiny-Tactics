using System;
using System.Collections;
using UnityEngine;
using TinyTactics.Animation;
using TinyTactics.Buildings;
using TinyTactics.Factions;
using TinyTactics.Units;

namespace TinyTactics.Combat
{
    /// <summary>
    /// Quản lý máu, giáp, hồi phục và vòng đời chết cho tất cả các Unit.
    /// Hỗ trợ khởi tạo lại khi tái sử dụng từ Object Pool.
    /// </summary>
    public class HealthSystem : MonoBehaviour, IDamageable
    {
        [Header("Unit Data")]
        [SerializeField] private UnitDataSO _unitData;

        [Header("Faction Fallback")]
        [Tooltip("Phe mặc định nếu Unit chưa gắn FactionVisual")]
        [SerializeField] private Faction _defaultFaction = Faction.Blue;

        [Header("Death Settings")]
        [Tooltip("Thời gian chờ animation Die trước khi thu hồi/hủy GameObject")]
        [SerializeField, Min(0f)] private float _deathCleanupDelay = 1.0f;

        [Header("Runtime Info - Read Only")]
        [SerializeField] private int _currentHP;
        [SerializeField] private int _maxHP = 100;
        [SerializeField] private int _armor;
        [SerializeField] private bool _isDead;

        private bool _isInitialized;
        private bool _isPopulationUnregistered;
        private Faction? _overrideFaction;
        private Coroutine _deathCoroutine;
        private Action<GameObject> _poolReleaseCallback;

        private UnitController _unitController;
        private UnitAnimationController _unitAnimation;
        private FactionVisual _factionVisual;
        private WorkerFSM _workerFSM;
        private Collider2D[] _colliders;

        public int CurrentHP => _currentHP;
        public int MaxHP => _maxHP;
        public int Armor => _armor;
        public bool IsDead => _isDead;
        public UnitDataSO UnitData => _unitData;

        public Faction TeamFaction
        {
            get
            {
                if (_factionVisual != null && !string.IsNullOrWhiteSpace(_factionVisual.FactionId))
                {
                    return FactionUtility.FromString(_factionVisual.FactionId);
                }

                if (_overrideFaction.HasValue)
                {
                    return _overrideFaction.Value;
                }

                return _defaultFaction;
            }
        }

        public event Action<int, int> OnHealthChanged;
        public event Action OnDied;

        private void Awake()
        {
            CacheComponents();

            if (_unitData != null && !_isInitialized)
            {
                Initialize(_unitData);
            }
            else if (!_isInitialized)
            {
                _maxHP = Mathf.Max(1, _maxHP);
                _currentHP = _maxHP;
                _isDead = false;
                _isPopulationUnregistered = false;
                _isInitialized = true;
            }
        }

        private void Start()
        {
            if (!_isInitialized && _unitData != null)
            {
                Initialize(_unitData);
            }
        }

        private void CacheComponents()
        {
            if (_unitController == null)
                _unitController = GetComponent<UnitController>();

            if (_unitAnimation == null)
                _unitAnimation = GetComponent<UnitAnimationController>();

            if (_factionVisual == null)
                _factionVisual = GetComponent<FactionVisual>();

            if (_workerFSM == null)
                _workerFSM = GetComponent<WorkerFSM>();

            if (_colliders == null || _colliders.Length == 0)
                _colliders = GetComponentsInChildren<Collider2D>(true);
        }

        /// <summary>
        /// Khởi tạo hoặc reset toàn bộ chỉ số máu/giáp từ UnitDataSO (dùng lúc Awake hoặc lấy từ Object Pool).
        /// </summary>
        public void Initialize(UnitDataSO data)
        {
            CacheComponents();

            if (_deathCoroutine != null)
            {
                StopCoroutine(_deathCoroutine);
                _deathCoroutine = null;
            }

            if (data != null)
            {
                _unitData = data;
            }

            _maxHP = _unitData != null ? Mathf.Max(1, _unitData.MaxHP) : Mathf.Max(1, _maxHP);
            _armor = _unitData != null ? Mathf.Max(0, _unitData.Armor) : Mathf.Max(0, _armor);
            _currentHP = _maxHP;
            _isDead = false;
            _isPopulationUnregistered = false;
            _isInitialized = true;

            SetComponentsActiveForAlive(true);

            if (_unitAnimation != null)
            {
                _unitAnimation.ResetAnimationState();
            }

            OnHealthChanged?.Invoke(_currentHP, _maxHP);
        }

        /// <summary>
        /// Gán trực tiếp phe cho Unit (hữu ích khi spawn không thông qua FactionVisual).
        /// </summary>
        public void SetFaction(Faction faction)
        {
            _overrideFaction = faction;
            _defaultFaction = faction;

            if (_factionVisual != null)
            {
                _factionVisual.SetFaction(FactionUtility.ToFactionId(faction));
            }
        }

        /// <summary>
        /// Đăng ký callback thu hồi về Object Pool thay cho Destroy.
        /// </summary>
        public void SetPoolReleaseCallback(Action<GameObject> releaseCallback)
        {
            _poolReleaseCallback = releaseCallback;
        }

        /// <summary>
        /// Nhận sát thương từ đòn đánh của kẻ địch, có tính Giáp.
        /// </summary>
        public void TakeDamage(int rawDamage, GameObject attacker = null)
        {
            if (_isDead || rawDamage <= 0)
                return;

            int finalDamage = Mathf.Max(1, rawDamage - _armor);
            _currentHP = Mathf.Max(0, _currentHP - finalDamage);

            OnHealthChanged?.Invoke(_currentHP, _maxHP);

            if (_currentHP <= 0)
            {
                HandleDeath();
            }
        }

        /// <summary>
        /// Hồi máu cho Unit (dùng cho Tu sĩ Monk hoặc hiệu ứng hồi phục).
        /// </summary>
        public void Heal(int amount)
        {
            if (_isDead || amount <= 0)
                return;

            int newHP = Mathf.Min(_maxHP, _currentHP + amount);
            if (newHP == _currentHP)
                return;

            _currentHP = newHP;
            OnHealthChanged?.Invoke(_currentHP, _maxHP);
        }

        private void HandleDeath()
        {
            if (_isDead)
                return;

            _isDead = true;

            SetComponentsActiveForAlive(false);
            UnregisterPopulationOnce();

            if (_unitAnimation != null)
            {
                _unitAnimation.PlayDie();
            }

            OnDied?.Invoke();

            _deathCoroutine = StartCoroutine(DespawnAfterDeathAnimation());
        }

        private void SetComponentsActiveForAlive(bool isAlive)
        {
            if (_colliders != null)
            {
                for (int i = 0; i < _colliders.Length; i++)
                {
                    if (_colliders[i] != null)
                    {
                        _colliders[i].enabled = isAlive;
                    }
                }
            }

            if (_unitController != null)
            {
                if (!isAlive)
                {
                    _unitController.Stop();
                }

                _unitController.enabled = isAlive;
            }

            if (_workerFSM != null)
            {
                _workerFSM.enabled = isAlive;
            }
        }

        private void UnregisterPopulationOnce()
        {
            if (_isPopulationUnregistered)
                return;

            _isPopulationUnregistered = true;

            if (TeamFaction != Faction.Blue)
                return;

            PopulationManager populationManager = PopulationManager.Instance;
            if (populationManager == null)
                return;

            int popCost = _unitData != null ? _unitData.PopulationCost : 1;
            populationManager.UnregisterUnit(popCost);
        }

        private IEnumerator DespawnAfterDeathAnimation()
        {
            float waitDuration = _deathCleanupDelay;

            if (_unitAnimation != null)
            {
                float clipDuration = _unitAnimation.GetDeathAnimationDuration();
                if (clipDuration > 0f)
                {
                    waitDuration = clipDuration;
                }
            }

            if (waitDuration > 0f)
            {
                yield return new WaitForSeconds(waitDuration);
            }

            _deathCoroutine = null;
            DespawnOrDestroy();
        }

        private void DespawnOrDestroy()
        {
            if (_poolReleaseCallback != null)
            {
                _poolReleaseCallback.Invoke(gameObject);
                return;
            }

            Destroy(gameObject);
        }
    }
}
