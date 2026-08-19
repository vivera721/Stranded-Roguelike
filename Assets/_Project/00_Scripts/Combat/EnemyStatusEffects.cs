using System.Collections;
using UnityEngine;

namespace StrandedRoguelike
{
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyStatusEffects : MonoBehaviour
    {
        private EnemyHealth health;
        private Coroutine poisonRoutine;
        private Coroutine burnRoutine;
        private Coroutine freezeRoutine;
        private bool poisonSpreadUsed;
        private bool burnExplosionUsed;
        private Rigidbody2D body;
        private Animator animator;
        private Coroutine slowRoutine;
        private float moveSpeedMultiplier = 1f;

        public float MoveSpeedMultiplier => moveSpeedMultiplier;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            body = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
        }

        public void ApplyPoison(float duration, float tickInterval, int tickDamage, float spreadRadius, int remainingSpreads)
        {
            if (poisonRoutine != null)
            {
                StopCoroutine(poisonRoutine);
            }

            poisonRoutine = StartCoroutine(PoisonRoutine(duration, tickInterval, tickDamage, spreadRadius, remainingSpreads));
        }

        public void ApplyBurn(float duration, float tickInterval, int tickDamage, float deathExplosionRadius, int deathExplosionDamage)
        {
            if (burnRoutine != null)
            {
                StopCoroutine(burnRoutine);
            }

            burnRoutine = StartCoroutine(BurnRoutine(duration, tickInterval, tickDamage, deathExplosionRadius, deathExplosionDamage));
        }

        public void ApplyFreeze(float duration, int shatterDamage)
        {
            if (freezeRoutine != null)
            {
                StopCoroutine(freezeRoutine);
            }

            freezeRoutine = StartCoroutine(FreezeRoutine(duration, shatterDamage));
        }

        public void ApplySlow(float duration, float multiplier)
        {
            if (slowRoutine != null)
            {
                StopCoroutine(slowRoutine);
            }

            slowRoutine = StartCoroutine(SlowRoutine(duration, multiplier));
        }

        private IEnumerator PoisonRoutine(float duration, float tickInterval, int tickDamage, float spreadRadius, int remainingSpreads)
        {
            poisonSpreadUsed = false;
            float elapsed = 0f;
            float nextTick = 0f;

            while (elapsed < duration && health != null && !health.isDead)
            {
                elapsed += Time.deltaTime;
                nextTick -= Time.deltaTime;

                if (nextTick <= 0f)
                {
                    nextTick = Mathf.Max(0.05f, tickInterval);
                    health.TakeDamage(tickDamage, transform.position, SurvivorDamageKind.StatusTick);
                }

                yield return null;
            }

            if (health != null && health.isDead)
            {
                TrySpreadPoison(duration, tickInterval, tickDamage, spreadRadius, remainingSpreads);
            }

            poisonRoutine = null;
        }

        private IEnumerator BurnRoutine(float duration, float tickInterval, int tickDamage, float deathExplosionRadius, int deathExplosionDamage)
        {
            burnExplosionUsed = false;
            float elapsed = 0f;
            float nextTick = 0f;

            while (elapsed < duration && health != null && !health.isDead)
            {
                elapsed += Time.deltaTime;
                nextTick -= Time.deltaTime;

                if (nextTick <= 0f)
                {
                    nextTick = Mathf.Max(0.05f, tickInterval);
                    health.TakeDamage(tickDamage, transform.position, SurvivorDamageKind.StatusTick);
                }

                yield return null;
            }

            if (health != null && health.isDead)
            {
                TryBurnDeathExplosion(deathExplosionRadius, deathExplosionDamage);
            }

            burnRoutine = null;
        }

        private IEnumerator FreezeRoutine(float duration, int shatterDamage)
        {
            float previousMultiplier = moveSpeedMultiplier;
            moveSpeedMultiplier = Mathf.Min(moveSpeedMultiplier, 0.35f);

            if (animator != null)
            {
                animator.speed = 0.15f;
            }

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }

            yield return new WaitForSeconds(duration);

            if (animator != null)
            {
                animator.speed = 1f;
            }

            if (health != null && !health.isDead && shatterDamage > 0)
            {
                health.TakeDamage(shatterDamage, transform.position, SurvivorDamageKind.StatusTick);
            }

            moveSpeedMultiplier = previousMultiplier;
            freezeRoutine = null;
        }

        private IEnumerator SlowRoutine(float duration, float multiplier)
        {
            float previousMultiplier = moveSpeedMultiplier;
            moveSpeedMultiplier = Mathf.Clamp(multiplier, 0.05f, 1f);

            yield return new WaitForSeconds(Mathf.Max(0.05f, duration));

            moveSpeedMultiplier = previousMultiplier;
            slowRoutine = null;
        }

        private void TrySpreadPoison(float duration, float tickInterval, int tickDamage, float spreadRadius, int remainingSpreads)
        {
            if (poisonSpreadUsed || remainingSpreads <= 0 || spreadRadius <= 0f)
            {
                return;
            }

            poisonSpreadUsed = true;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, spreadRadius);

            for (int i = 0; i < hits.Length; i++)
            {
                if (!hits[i].TryGetComponent(out EnemyHealth targetHealth) || targetHealth == health || targetHealth.isDead)
                {
                    continue;
                }

                EnemyStatusEffects targetStatus = targetHealth.GetComponent<EnemyStatusEffects>();
                if (targetStatus == null)
                {
                    targetStatus = targetHealth.gameObject.AddComponent<EnemyStatusEffects>();
                }

                targetStatus.ApplyPoison(duration * 0.75f, tickInterval, tickDamage, spreadRadius, remainingSpreads - 1);
            }
        }

        private void TryBurnDeathExplosion(float radius, int damage)
        {
            if (burnExplosionUsed || radius <= 0f || damage <= 0)
            {
                return;
            }

            burnExplosionUsed = true;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].TryGetComponent(out EnemyHealth targetHealth) && targetHealth != health && !targetHealth.isDead)
                {
                    targetHealth.TakeDamage(damage, transform.position);
                }
            }
        }
    }
}
