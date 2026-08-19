using UnityEngine;
using System;

namespace StrandedRoguelike
{
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private int maxHealth = 3;
        [SerializeField, Min(1)] private int currentHealth = 3;
        [SerializeField, Min(0f)] private float knockbackDistance = 0.25f;
        [SerializeField, Min(0.01f)] private float knockbackDuration = 0.08f;


        private HitFlash HitFlash;
        private Rigidbody2D body;
        private Coroutine knockbackRoutine;
        private Animator animator;
        private bool deathNotified;
        private bool destroyObjectOnDeathAnimationEvent = true;

        public event Action<EnemyHealth> Died;
        public event Action<EnemyHealth> DeathAnimationFinished;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public float HealthRatio => maxHealth <= 0 ? 0f : currentHealth / (float)maxHealth;

        public bool isDead => currentHealth <= 0;

        private void Awake()
        {
            ResetHealth();
            HitFlash = GetComponent<HitFlash>();
            body = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
        }

        public void SetMaxHealth(int value, bool healToFull = true)
        {
            maxHealth = Mathf.Max(1, value);

            if (healToFull)
            {
                ResetHealth();
            }
        }

        public void ResetHealth()
        {
            currentHealth = Mathf.Max(1, maxHealth);
            deathNotified = false;

            if (body != null)
            {
                body.simulated = true;
            }
        }

        public void SetDestroyObjectOnDeathAnimationEvent(bool enabled)
        {
            destroyObjectOnDeathAnimationEvent = enabled;
        }

        public void TakeDamage(int damage)
        {
            TakeDamage(damage, Vector2.zero);
        }

        public void TakeDamage(int damage, Vector2 attackerPosition)
        {
            TakeDamage(damage, attackerPosition, SurvivorDamageKind.Direct);
        }

        public void TakeDamage(int damage, Vector2 attackerPosition, SurvivorDamageKind damageKind)
        {
            if (damage <= 0 || currentHealth <= 0)
            {
                return;
            }

            currentHealth = Mathf.Max(0, currentHealth - damage);
            HitFlash?.Play();
            SurvivorDamageEvents.RaiseEnemyDamaged(this, damage, attackerPosition, damageKind);

            Vector2 knockbackDirection = (Vector2)transform.position - attackerPosition;
            ApplyKnockback(knockbackDirection);
            if(currentHealth <= 0)
            {
                NotifyDied();

                if (this.tag != "Boss")
                { 
                    TrySetTrigger(animator, "Die");
                }
            }
        }

        public void DisableComponents()
        {
            if (body != null)
            {
                body.simulated = false;
            }

        }

        public void DestroyObject()
        {
            DeathAnimationFinished?.Invoke(this);

            if (destroyObjectOnDeathAnimationEvent)
            {
                Destroy(gameObject);
            }
        }

        private void NotifyDied()
        {
            if (deathNotified)
            {
                return;
            }

            deathNotified = true;
            Died?.Invoke(this);
            SurvivorDamageEvents.RaiseEnemyDied(this);
        }

        private static void TrySetTrigger(Animator targetAnimator, string triggerName)
        {
            if (targetAnimator == null || string.IsNullOrEmpty(triggerName))
            {
                return;
            }

            AnimatorControllerParameter[] parameters = targetAnimator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].type == AnimatorControllerParameterType.Trigger && parameters[i].name == triggerName)
                {
                    targetAnimator.SetTrigger(triggerName);
                    return;
                }
            }
        }

        private void ApplyKnockback(Vector2 direction)
        {
            if (body == null || direction == Vector2.zero || knockbackDistance <= 0f)
            {
                return;
            }

            if (knockbackRoutine != null)
            {
                StopCoroutine(knockbackRoutine);
            }

            knockbackRoutine = StartCoroutine(KnockbackRoutine(direction.normalized));
        }

        private System.Collections.IEnumerator KnockbackRoutine(Vector2 direction)
        {
            float elapsed = 0f;
            float knockbackSpeed = knockbackDistance / knockbackDuration;

            while (elapsed < knockbackDuration)
            {
                body.MovePosition(body.position + direction * (knockbackSpeed * Time.fixedDeltaTime));
                elapsed += Time.fixedDeltaTime;

                yield return new WaitForFixedUpdate();
            }

            knockbackRoutine = null;
        }
    }
}
