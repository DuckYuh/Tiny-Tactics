using UnityEngine;
using TinyTactics.Animation;
using TinyTactics.Factions;
using TinyTactics.Units;

namespace TinyTactics.Combat
{
    public enum CombatOrderMode
    {
        IdleDefend,
        ForceMove,
        AttackMove,
        AttackSpecificTarget
    }

    /// <summary>
    /// Điều khiển hành vi quét địch bằng Physics2D.OverlapCircleAll, bám đuổi,
    /// Attack-Move (A + Click) và thực thi đòn đánh cho các Unit chiến đấu.
    /// </summary>
    [RequireComponent(typeof(UnitController))]
    [RequireComponent(typeof(HealthSystem))]
    public class UnitCombatController : MonoBehaviour
    {
        [Header("Data & Scanning")]
        [SerializeField] private UnitDataSO _unitData;

        [Tooltip("Bán kính quét địch xung quanh (tự động kẹp >= AttackRange)")]
        [SerializeField, Min(0.1f)] private float _aggroRange = 5.0f;

        [Tooltip("Chu kỳ quét địch (giây) để tối ưu hiệu năng")]
        [SerializeField, Min(0.05f)] private float _scanInterval = 0.15f;

        [Tooltip("Chu kỳ tính lại đường A* khi truy đuổi mục tiêu đang di chuyển")]
        [SerializeField, Min(0.05f)] private float _chaseRepathInterval = 0.25f;

        [Tooltip("LayerMask của đối tượng có thể nhận sát thương")]
        [SerializeField] private LayerMask _targetLayerMask = ~0;

        [Header("Ranged Combat")]
        [Tooltip("Bật cho đơn vị đánh xa (Archer) để bắn Projectile thay vì trừ máu cận chiến")]
        [SerializeField] private bool _isRanged;

        [SerializeField] private GameObject _projectilePrefab;

        [Tooltip("Điểm xuất phát mũi tên (nếu để trống sẽ lấy vị trí tâm Unit)")]
        [SerializeField] private Transform _firePoint;

        [Header("Runtime State - Read Only")]
        [SerializeField] private CombatOrderMode _orderMode = CombatOrderMode.IdleDefend;

        private UnitController _unitController;
        private HealthSystem _healthSystem;
        private UnitAnimationController _unitAnimation;
        private FactionVisual _factionVisual;

        private IDamageable _currentTarget;
        private Vector3 _attackMoveDestination;
        private float _scanTimer;
        private float _attackCooldownTimer;
        private float _chaseRepathTimer;
        private bool _isPausedForCombat;

        protected UnitDataSO UnitData => _unitData != null ? _unitData : (_healthSystem != null ? _healthSystem.UnitData : null);
        protected IDamageable CurrentTarget => _currentTarget;
        protected LayerMask TargetLayerMask => _targetLayerMask;

        public CombatOrderMode OrderMode => _orderMode;

        public int Damage => UnitData != null ? UnitData.Damage : 10;
        public float AttackRange => UnitData != null ? Mathf.Max(0.1f, UnitData.AttackRange) : 1.2f;
        public float AttackCooldown => UnitData != null ? Mathf.Max(0.1f, UnitData.AttackCooldown) : 1.2f;
        public float EffectiveAggroRange => Mathf.Max(_aggroRange, AttackRange);

        protected virtual void Awake()
        {
            _unitController = GetComponent<UnitController>();
            _healthSystem = GetComponent<HealthSystem>();
            _unitAnimation = GetComponent<UnitAnimationController>();
            _factionVisual = GetComponent<FactionVisual>();

            SyncStatsFromData();
        }

        protected virtual void OnEnable()
        {
            _orderMode = CombatOrderMode.IdleDefend;
            _currentTarget = null;
            _scanTimer = UnityEngine.Random.Range(0f, _scanInterval);
            _attackCooldownTimer = 0f;
            _chaseRepathTimer = 0f;
            _isPausedForCombat = false;
        }

        /// <summary>
        /// Đồng bộ tốc độ di chuyển và dữ liệu chiến đấu từ UnitDataSO.
        /// </summary>
        public void Initialize(UnitDataSO data)
        {
            if (data != null)
            {
                _unitData = data;
            }

            SyncStatsFromData();
        }

        private void SyncStatsFromData()
        {
            UnitDataSO data = UnitData;
            if (data != null && _unitController != null && data.MoveSpeed > 0f)
            {
                _unitController.SetMoveSpeed(data.MoveSpeed);
            }
        }

        protected virtual void Update()
        {
            if (_healthSystem == null || _healthSystem.IsDead)
            {
                _currentTarget = null;
                return;
            }

            if (_attackCooldownTimer > 0f)
            {
                _attackCooldownTimer -= Time.deltaTime;
            }

            if (_chaseRepathTimer > 0f)
            {
                _chaseRepathTimer -= Time.deltaTime;
            }

            switch (_orderMode)
            {
                case CombatOrderMode.ForceMove:
                    UpdateForceMove();
                    break;

                case CombatOrderMode.AttackMove:
                    UpdateAttackMove();
                    break;

                case CombatOrderMode.AttackSpecificTarget:
                    UpdateSpecificTarget();
                    break;

                case CombatOrderMode.IdleDefend:
                    UpdateIdleDefend();
                    break;
            }
        }

        /// <summary>
        /// Di chuyển thường (Chuột phải mặt đất): bỏ qua địch trên đường đi, tới đích mới tự vệ.
        /// </summary>
        public void OrderMoveTo(Vector3 destination)
        {
            if (_healthSystem != null && _healthSystem.IsDead)
                return;

            _orderMode = CombatOrderMode.ForceMove;
            _currentTarget = null;
            _isPausedForCombat = false;
            _unitController.MoveTo(destination);
        }

        /// <summary>
        /// Di chuyển tấn công (Phím A + Click): tự động đánh địch gặp trên đường rồi đi tiếp tới đích.
        /// </summary>
        public void AttackMoveTo(Vector3 destination)
        {
            if (_healthSystem != null && _healthSystem.IsDead)
                return;

            _orderMode = CombatOrderMode.AttackMove;
            _attackMoveDestination = destination;
            _currentTarget = null;
            _isPausedForCombat = false;
            _scanTimer = 0f;
            _unitController.MoveTo(destination);
        }

        /// <summary>
        /// Tấn công mục tiêu chỉ định (Chuột phải trực tiếp vào Unit/Công trình phe địch).
        /// </summary>
        public void AttackTarget(IDamageable target)
        {
            if (_healthSystem != null && _healthSystem.IsDead)
                return;

            if (!IsValidTarget(target))
                return;

            _orderMode = CombatOrderMode.AttackSpecificTarget;
            _currentTarget = target;
            _isPausedForCombat = true;
            _chaseRepathTimer = 0f;
        }

        private void UpdateForceMove()
        {
            if (!_unitController.IsMoving)
            {
                _orderMode = CombatOrderMode.IdleDefend;
                _scanTimer = 0f;
            }
        }

        private void UpdateAttackMove()
        {
            ValidateCurrentTarget();

            if (_currentTarget == null)
            {
                if (_isPausedForCombat)
                {
                    _isPausedForCombat = false;
                    _unitController.MoveTo(_attackMoveDestination);
                }

                TickEnemyScan();

                if (_currentTarget != null)
                {
                    _isPausedForCombat = true;
                    _chaseRepathTimer = 0f;
                    EngageCurrentTarget();
                    return;
                }

                if (!_unitController.IsMoving)
                {
                    _orderMode = CombatOrderMode.IdleDefend;
                }

                return;
            }

            EngageCurrentTarget();
        }

        private void UpdateSpecificTarget()
        {
            ValidateCurrentTarget();

            if (_currentTarget == null)
            {
                _isPausedForCombat = false;
                _unitController.StopMovement();
                _orderMode = CombatOrderMode.IdleDefend;
                _scanTimer = 0f;
                return;
            }

            EngageCurrentTarget();
        }

        private void UpdateIdleDefend()
        {
            ValidateCurrentTarget();

            if (_currentTarget == null)
            {
                TickEnemyScan();
            }

            if (_currentTarget != null)
            {
                EngageCurrentTarget();
            }
        }

        private void TickEnemyScan()
        {
            _scanTimer -= Time.deltaTime;
            if (_scanTimer > 0f)
                return;

            _scanTimer = _scanInterval;
            _currentTarget = FindNearestValidTarget();
        }

        private void ValidateCurrentTarget()
        {
            if (_currentTarget == null)
                return;

            MonoBehaviour targetMono = _currentTarget as MonoBehaviour;
            if (targetMono == null || _currentTarget.IsDead || !IsValidTarget(_currentTarget))
            {
                _currentTarget = null;
            }
        }

        private void EngageCurrentTarget()
        {
            MonoBehaviour targetMono = _currentTarget as MonoBehaviour;
            if (targetMono == null || _currentTarget.IsDead)
            {
                _currentTarget = null;
                return;
            }

            Vector3 targetPoint = GetClosestPointOnTarget(targetMono);
            float distance = Vector2.Distance(transform.position, targetPoint);

            if (distance <= AttackRange)
            {
                _unitController.StopMovement();
                _unitController.FacePosition(targetMono.transform.position);

                if (_attackCooldownTimer <= 0f)
                {
                    _attackCooldownTimer = AttackCooldown;
                    TriggerAttackAnimation(targetMono.transform.position);
                    PerformAction();
                }
            }
            else if (_chaseRepathTimer <= 0f || !_unitController.IsMoving)
            {
                _chaseRepathTimer = _chaseRepathInterval;
                _unitController.MoveTo(targetPoint);
            }
        }

        private void TriggerAttackAnimation(Vector3 targetWorldPosition)
        {
            if (_unitAnimation == null)
                return;

            if (!_unitAnimation.PlayDirectionalAttack(targetWorldPosition))
            {
                _unitAnimation.PlayAttack();
            }
        }

        /// <summary>
        /// Thực thi đòn đánh khi đã trong tầm (cận chiến hoặc bắn ArrowProjectile với Archer; Monk override để hồi máu).
        /// </summary>
        protected virtual void PerformAction()
        {
            if (_currentTarget == null || _currentTarget.IsDead)
                return;

            int damage = Damage;
            if (damage <= 0)
                return;

            if (_isRanged)
            {
                FireProjectile(damage);
                return;
            }

            _currentTarget.TakeDamage(damage, gameObject);
        }

        private void FireProjectile(int damage)
        {
            if (_projectilePrefab == null)
            {
                Debug.LogWarning($"[UnitCombatController] {name} bật isRanged nhưng chưa gán Projectile Prefab.", this);
                return;
            }

            Vector3 spawnPos = _firePoint != null ? _firePoint.position : transform.position;
            GameObject projectileObj = Instantiate(_projectilePrefab, spawnPos, Quaternion.identity);
            ArrowProjectile arrow = projectileObj.GetComponent<ArrowProjectile>();

            if (arrow != null)
            {
                arrow.Launch(spawnPos, _currentTarget, damage, gameObject);
            }
            else
            {
                Debug.LogWarning($"[UnitCombatController] Prefab {_projectilePrefab.name} thiếu component ArrowProjectile.", projectileObj);
                Destroy(projectileObj);
            }
        }

        protected virtual IDamageable FindNearestValidTarget()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                transform.position,
                EffectiveAggroRange,
                _targetLayerMask);

            IDamageable closestTarget = null;
            float closestSqrDist = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i];
                if (hit == null || hit.gameObject == gameObject)
                    continue;

                IDamageable candidate = hit.GetComponentInParent<IDamageable>();
                if (!IsValidTarget(candidate))
                    continue;

                MonoBehaviour candidateMono = candidate as MonoBehaviour;
                if (candidateMono == null || candidateMono.gameObject == gameObject)
                    continue;

                Vector3 closestPoint = hit.ClosestPoint(transform.position);
                float sqrDist = ((Vector2)closestPoint - (Vector2)transform.position).sqrMagnitude;

                if (sqrDist < closestSqrDist)
                {
                    closestSqrDist = sqrDist;
                    closestTarget = candidate;
                }
            }

            return closestTarget;
        }

        protected virtual bool IsValidTarget(IDamageable candidate)
        {
            if (candidate == null || candidate.IsDead)
                return false;

            Faction selfFaction = GetSelfFaction();
            if (candidate.TeamFaction == Faction.Neutral || candidate.TeamFaction == selfFaction)
                return false;

            return true;
        }

        protected Faction GetSelfFaction()
        {
            if (_healthSystem != null)
                return _healthSystem.TeamFaction;

            if (_factionVisual != null)
                return FactionUtility.FromString(_factionVisual.FactionId);

            return Faction.Blue;
        }

        private Vector3 GetClosestPointOnTarget(MonoBehaviour targetMono)
        {
            Collider2D targetCollider = targetMono.GetComponentInChildren<Collider2D>();
            if (targetCollider != null && targetCollider.enabled)
            {
                return targetCollider.ClosestPoint(transform.position);
            }

            return targetMono.transform.position;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.35f, 0.1f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, AttackRange);

            Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, EffectiveAggroRange);
        }
#endif
    }
}
