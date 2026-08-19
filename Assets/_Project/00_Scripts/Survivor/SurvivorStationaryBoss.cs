using UnityEngine;

namespace StrandedRoguelike
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class SurvivorStationaryBoss : MonoBehaviour
    {
        private static readonly int DieHash = Animator.StringToHash("Die");

        [SerializeField, Min(0)] private int contactDamage = 2;
        [SerializeField, Min(0.05f)] private float contactDamageCooldown = 0.7f;

        private EnemyHealth health;
        private float nextContactDamageTime;
        private bool dying;

        public void Configure(int newContactDamage)
        {
            contactDamage = Mathf.Max(0, newContactDamage);
            nextContactDamageTime = 0f;

            if (health == null)
            {
                health = GetComponent<EnemyHealth>();
            }

            health?.SetDestroyObjectOnDeathAnimationEvent(true);
        }

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
        }

        private void OnEnable()
        {
            nextContactDamageTime = 0f;
            dying = false;

            if (health == null)
            {
                health = GetComponent<EnemyHealth>();
            }

            if (health != null)
            {
                health.Died -= OnDied;
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnDied;
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryDamagePlayer(collision.collider);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryDamagePlayer(other);
        }

        private void TryDamagePlayer(Collider2D other)
        {
            if (dying
                || (health != null && health.isDead)
                || contactDamage <= 0
                || Time.time < nextContactDamageTime
                || other == null)
            {
                return;
            }

            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                return;
            }

            nextContactDamageTime = Time.time + contactDamageCooldown;
            playerHealth.TakeDamageFromAttacker(contactDamage, transform.position);
        }

        private void OnDied(EnemyHealth deadHealth)
        {
            if (dying)
            {
                return;
            }

            dying = true;
            TriggerDeathAnimation();
        }

        [ContextMenu("Boss Test/Defeat This Boss")]
        private void DefeatThisBossForTest()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Boss Test] Boss tests can only be used in Play Mode.", this);
                return;
            }

            if (health == null)
            {
                health = GetComponent<EnemyHealth>();
            }

            if (health == null || health.isDead)
            {
                Debug.LogWarning("[Boss Test] This boss is already dead or has no EnemyHealth.", this);
                return;
            }

            health.TakeDamage(Mathf.Max(1, health.CurrentHealth), transform.position);
            Debug.Log("[Boss Test] Boss defeat sequence started.", this);
        }

        private void TriggerDeathAnimation()
        {
            Animator[] animators = GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                Animator targetAnimator = animators[i];
                if (targetAnimator == null || targetAnimator.runtimeAnimatorController == null)
                {
                    continue;
                }

                AnimatorControllerParameter[] parameters = targetAnimator.parameters;
                for (int parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
                {
                    AnimatorControllerParameter parameter = parameters[parameterIndex];
                    if (parameter.type == AnimatorControllerParameterType.Trigger
                        && parameter.nameHash == DieHash)
                    {
                        targetAnimator.SetTrigger(DieHash);
                        break;
                    }
                }
            }
        }
    }
}
