using System;
using UnityEngine;
using TinyTactics.Audio;

namespace TinyTactics.Combat
{
    /// <summary>
    /// Quản lý mũi tên bay của Cung thủ (Archer) với quỹ đạo vòng cung nhẹ,
    /// tự xoay theo hướng vận tốc và xử lý an toàn khi mục tiêu chết giữa đường.
    /// </summary>
    public class ArrowProjectile : MonoBehaviour
    {
        [Header("Trajectory Settings")]
        [SerializeField, Min(0.1f)] private float _speed = 12f;

        [Tooltip("Độ cao vòng cung theo tỷ lệ khoảng cách (0 = bay thẳng)")]
        [SerializeField, Min(0f)] private float _arcHeightFactor = 0.12f;

        [SerializeField, Min(0f)] private float _maxArcHeight = 1.5f;

        [SerializeField, Min(0.01f)] private float _arrivalThreshold = 0.15f;

        [Header("Audio")]
        [SerializeField] private bool _playSFX = true;

        private Vector3 _startPos;
        private Vector3 _lastKnownTargetPos;
        private IDamageable _target;
        private MonoBehaviour _targetMono;
        private int _damage;
        private GameObject _shooter;

        private bool _isLaunched;
        private float _elapsedTime;
        private float _flightDuration;
        private float _arcPeakHeight;
        private Action<GameObject> _poolReleaseCallback;

        private void OnDisable()
        {
            _isLaunched = false;
            _target = null;
            _targetMono = null;
            _shooter = null;
        }

        /// <summary>
        /// Đăng ký callback thu hồi về Object Pool thay cho Destroy.
        /// </summary>
        public void SetPoolReleaseCallback(Action<GameObject> releaseCallback)
        {
            _poolReleaseCallback = releaseCallback;
        }

        /// <summary>
        /// Khởi chạy mũi tên từ vị trí bắn tới mục tiêu IDamageable.
        /// </summary>
        public void Launch(Vector3 startPos, IDamageable target, int damage, GameObject shooter)
        {
            _startPos = startPos;
            transform.position = startPos;

            _target = target;
            _targetMono = target as MonoBehaviour;
            _damage = Mathf.Max(0, damage);
            _shooter = shooter;

            _lastKnownTargetPos = GetTargetCenterPosition();
            float distance = Vector2.Distance(_startPos, _lastKnownTargetPos);

            _flightDuration = Mathf.Max(0.02f, distance / Mathf.Max(0.1f, _speed));
            _arcPeakHeight = Mathf.Min(_maxArcHeight, distance * _arcHeightFactor);
            _elapsedTime = 0f;
            _isLaunched = true;

            RotateAlongDirection(_lastKnownTargetPos - _startPos);

            if (_playSFX && SFXManager.Instance != null)
            {
                SFXManager.Instance.PlayAtPosition(SFXType.ArrowShoot, startPos);
            }
        }

        private void Update()
        {
            if (!_isLaunched)
                return;

            // Chỉ cập nhật tọa độ mục tiêu khi mục tiêu còn sống; nếu chết giữa chừng thì bay tới vị trí cuối.
            if (IsTargetAlive())
            {
                _lastKnownTargetPos = GetTargetCenterPosition();
            }

            _elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsedTime / _flightDuration);

            Vector3 linearPos = Vector3.Lerp(_startPos, _lastKnownTargetPos, t);
            float parabolaOffset = 4f * _arcPeakHeight * t * (1f - t);
            Vector3 nextPos = new Vector3(linearPos.x, linearPos.y + parabolaOffset, _startPos.z);

            RotateAlongDirection(nextPos - transform.position);
            transform.position = nextPos;

            if (t >= 1f || Vector2.Distance(linearPos, _lastKnownTargetPos) <= _arrivalThreshold)
            {
                OnArrive();
            }
        }

        private bool IsTargetAlive()
        {
            return _target != null && _targetMono != null && !_target.IsDead;
        }

        private Vector3 GetTargetCenterPosition()
        {
            if (_targetMono != null)
            {
                return _targetMono.transform.position;
            }

            return _lastKnownTargetPos;
        }

        private void RotateAlongDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude <= 0.00001f)
                return;

            float angleDegrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angleDegrees);
        }

        private void OnArrive()
        {
            _isLaunched = false;

            if (IsTargetAlive() && _damage > 0)
            {
                _target.TakeDamage(_damage, _shooter);

                if (_playSFX && SFXManager.Instance != null)
                {
                    SFXManager.Instance.PlayAtPosition(SFXType.ArrowHit, _lastKnownTargetPos);
                }
            }

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
