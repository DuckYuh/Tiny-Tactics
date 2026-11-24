using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Audio;

namespace TinyTactics.Combat
{
    /// <summary>
    /// Điều khiển hành vi hồi máu diện rộng (AoE Heal) cho Tu sĩ (Monk).
    /// Kế thừa UnitCombatController để dùng chung hệ thống chọn quân, Attack-Move (A + Click) và di chuyển A*.
    /// </summary>
    public class MonkHealerController : UnitCombatController
    {
        [Header("Monk Healing Settings")]
        [Tooltip("Bán kính hồi máu diện rộng quanh mục tiêu được chọn")]
        [SerializeField, Min(0.1f)] private float _aoeHealRadius = 1.5f;

        [Header("Visual & Audio Effects")]
        [Tooltip("Prefab hiệu ứng hồi máu (tùy chọn, ví dụ Heal.controller từ Tiny Swords)")]
        [SerializeField] private GameObject _healEffectPrefab;

        [SerializeField, Min(0.1f)] private float _healEffectLifetime = 1.0f;
        [SerializeField] private Color _healFlashColor = new Color(0.45f, 1f, 0.55f, 1f);
        [SerializeField, Min(0.05f)] private float _healFlashDuration = 0.25f;
        [SerializeField] private bool _playSFX = true;

        private readonly Dictionary<SpriteRenderer, Coroutine> _activeFlashes =
            new Dictionary<SpriteRenderer, Coroutine>();

        protected override void OnEnable()
        {
            base.OnEnable();
            _activeFlashes.Clear();
        }

        private void OnDisable()
        {
            foreach (KeyValuePair<SpriteRenderer, Coroutine> pair in _activeFlashes)
            {
                if (pair.Key != null)
                {
                    pair.Key.color = Color.white;
                }
            }

            _activeFlashes.Clear();
        }

        protected override bool IsValidTarget(IDamageable candidate)
        {
            if (candidate == null || candidate.IsDead)
                return false;

            if (candidate.TeamFaction != GetSelfFaction())
                return false;

            if (candidate.CurrentHP >= candidate.MaxHP)
                return false;

            MonoBehaviour candidateMono = candidate as MonoBehaviour;
            if (candidateMono == null || candidateMono.gameObject == gameObject)
                return false;

            // Chỉ hồi máu cho Unit đồng minh (có HealthSystem), không hồi cho công trình.
            return candidateMono.GetComponent<HealthSystem>() != null;
        }

        protected override IDamageable FindNearestValidTarget()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                transform.position,
                EffectiveAggroRange,
                TargetLayerMask);

            IDamageable bestTarget = null;
            float lowestHpRatio = float.MaxValue;
            float closestSqrDist = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i];
                if (hit == null || hit.gameObject == gameObject)
                    continue;

                IDamageable candidate = hit.GetComponentInParent<IDamageable>();
                if (!IsValidTarget(candidate))
                    continue;

                float hpRatio = candidate.MaxHP > 0
                    ? (float)candidate.CurrentHP / candidate.MaxHP
                    : 1f;

                float sqrDist = ((Vector2)hit.transform.position - (Vector2)transform.position).sqrMagnitude;

                if (hpRatio < lowestHpRatio - 0.01f ||
                    (Mathf.Abs(hpRatio - lowestHpRatio) <= 0.01f && sqrDist < closestSqrDist))
                {
                    lowestHpRatio = hpRatio;
                    closestSqrDist = sqrDist;
                    bestTarget = candidate;
                }
            }

            return bestTarget;
        }

        protected override void PerformAction()
        {
            if (CurrentTarget == null || CurrentTarget.IsDead)
                return;

            MonoBehaviour targetMono = CurrentTarget as MonoBehaviour;
            if (targetMono == null)
                return;

            int healAmount = Damage;
            if (healAmount <= 0)
                return;

            Vector3 healCenter = targetMono.transform.position;
            Faction selfFaction = GetSelfFaction();

            Collider2D[] hits = Physics2D.OverlapCircleAll(
                healCenter,
                _aoeHealRadius,
                TargetLayerMask);

            HashSet<HealthSystem> healedUnits = new HashSet<HealthSystem>();

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null)
                    continue;

                HealthSystem allyHealth = hits[i].GetComponentInParent<HealthSystem>();
                if (allyHealth == null || allyHealth.IsDead)
                    continue;

                if (allyHealth.TeamFaction != selfFaction || allyHealth.CurrentHP >= allyHealth.MaxHP)
                    continue;

                if (!healedUnits.Add(allyHealth))
                    continue;

                allyHealth.Heal(healAmount);
                TriggerHealFlash(allyHealth.gameObject);
            }

            // Đảm bảo mục tiêu chính luôn nhận hồi máu nếu đứng ngay tâm AoE.
            HealthSystem primaryHealth = targetMono.GetComponent<HealthSystem>();
            if (primaryHealth != null && !primaryHealth.IsDead &&
                primaryHealth.CurrentHP < primaryHealth.MaxHP &&
                healedUnits.Add(primaryHealth))
            {
                primaryHealth.Heal(healAmount);
                TriggerHealFlash(primaryHealth.gameObject);
            }

            SpawnHealVisual(healCenter);

            if (_playSFX && SFXManager.Instance != null)
            {
                SFXManager.Instance.PlayAtPosition(SFXType.MonkHeal, healCenter);
            }
        }

        private void SpawnHealVisual(Vector3 position)
        {
            if (_healEffectPrefab == null)
                return;

            GameObject fx = Instantiate(_healEffectPrefab, position, Quaternion.identity);
            Destroy(fx, _healEffectLifetime);
        }

        private void TriggerHealFlash(GameObject unitObject)
        {
            if (unitObject == null)
                return;

            SpriteRenderer sr = unitObject.GetComponentInChildren<SpriteRenderer>();
            if (sr == null)
                return;

            if (_activeFlashes.TryGetValue(sr, out Coroutine running) && running != null)
            {
                StopCoroutine(running);
            }

            _activeFlashes[sr] = StartCoroutine(HealFlashCoroutine(sr));
        }

        private IEnumerator HealFlashCoroutine(SpriteRenderer spriteRenderer)
        {
            spriteRenderer.color = _healFlashColor;
            yield return new WaitForSeconds(_healFlashDuration);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
                _activeFlashes.Remove(spriteRenderer);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.45f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, AttackRange);

            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, EffectiveAggroRange);
        }
#endif
    }
}
