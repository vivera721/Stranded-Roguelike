using System.Collections;
using UnityEngine;

namespace StrandedRoguelike
{
    public enum SurvivorEnemySfxType
    {
        Humanoid,
        Insect
    }

    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class SurvivorEnemy : MonoBehaviour
    {
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        [SerializeField, Min(1)] private int maxHealth = 3;
        [SerializeField, Min(0f)] private float moveSpeed = 2.2f;
        [SerializeField, Min(0)] private int contactDamage = 1;
        [SerializeField, Min(0.05f)] private float contactCooldown = 0.7f;
        [Header("Animation")]
        [SerializeField] private bool useVerticalMoveAnimation;
        [SerializeField, Min(0f)] private float horizontalFacingDeadZone = 0.05f;
        [Header("Death")]
        [SerializeField] private bool waitForDeathAnimation = true;
        [SerializeField, Min(0f)] private float returnToPoolDelay = 0.1f;
        [SerializeField, Min(0.05f)] private float deathAnimationFallbackDelay = 0.8f;
        [SerializeField, Min(0.1f)] private float deathAnimationMaxWait = 2f;
        [SerializeField] private string deathStateName = "Die";
        [Header("Audio")]
        [SerializeField] private SurvivorEnemySfxType sfxType = SurvivorEnemySfxType.Humanoid;
        [Header("Experience")]
        [SerializeField, Min(0)] private int experienceValue = 1;
        [SerializeField] private Sprite experienceGemSprite;

        private SurvivorEnemyPool pool;
        private Transform target;
        private Rigidbody2D body;
        private EnemyHealth health;
        private EnemyStatusEffects statusEffects;
        private SpriteRenderer spriteRenderer;
        private Animator animator;
        private float nextContactDamageTime;
        private Coroutine returnRoutine;
        private bool returnedToPool;
        private bool hasMoveXParameter;
        private bool hasMoveYParameter;
        private bool hasSpeedParameter;
        private float lastHorizontalMoveDirection = 1f;

        public SurvivorEnemySfxType SfxType => sfxType;

        public void SetPool(SurvivorEnemyPool newPool)
        {
            pool = newPool;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<EnemyHealth>();
            statusEffects = GetComponent<EnemyStatusEffects>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            body.gravityScale = 0f;
            body.freezeRotation = true;
            DisableLegacyEnemyBehaviours();
            CacheAnimatorParameters();

            if (health != null)
            {
                health.SetDestroyObjectOnDeathAnimationEvent(false);
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += OnDied;
                health.DeathAnimationFinished += OnDeathAnimationFinished;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnDied;
                health.DeathAnimationFinished -= OnDeathAnimationFinished;
            }

            if (returnRoutine != null)
            {
                StopCoroutine(returnRoutine);
                returnRoutine = null;
            }
        }

        public void Configure(SurvivorEnemyPool ownerPool, Transform newTarget, int newMaxHealth, float newMoveSpeed, int newContactDamage, int newExperienceValue)
        {
            pool = ownerPool;
            target = newTarget;
            maxHealth = Mathf.Max(1, newMaxHealth);
            moveSpeed = Mathf.Max(0f, newMoveSpeed);
            contactDamage = Mathf.Max(0, newContactDamage);
            experienceValue = Mathf.Max(0, newExperienceValue);
            nextContactDamageTime = 0f;
            returnedToPool = false;
            lastHorizontalMoveDirection = spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f;

            if (health != null)
            {
                health.SetMaxHealth(maxHealth);
                health.SetDestroyObjectOnDeathAnimationEvent(false);
            }

            if (body != null)
            {
                body.simulated = true;
            }

            if (animator != null)
            {
                CacheAnimatorParameters();
                animator.Rebind();
                animator.Update(0f);
                UpdateMoveAnimation(Vector2.down, false);
            }
        }

        private void FixedUpdate()
        {
            if (target == null || health == null || health.isDead)
            {
                return;
            }

            Vector2 current = body.position;
            Vector2 direction = ((Vector2)target.position - current);

            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            float slowMultiplier = statusEffects != null ? statusEffects.MoveSpeedMultiplier : 1f;
            Vector2 next = current + direction.normalized * (moveSpeed * slowMultiplier * Time.fixedDeltaTime);
            body.MovePosition(next);
            UpdateMoveAnimation(direction.normalized, moveSpeed * slowMultiplier > 0.01f);

            if (spriteRenderer != null && Mathf.Abs(direction.x) > 0.05f)
            {
                spriteRenderer.flipX = direction.x < 0f;
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
            if (contactDamage <= 0 || Time.time < nextContactDamageTime)
            {
                return;
            }

            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                return;
            }

            nextContactDamageTime = Time.time + contactCooldown;
            playerHealth.TakeDamageFromAttacker(contactDamage, transform.position);
        }

        private void OnDied(EnemyHealth deadHealth)
        {
            DropExperience();

            if (body != null)
            {
                body.simulated = false;
            }

            if (returnRoutine == null)
            {
                returnRoutine = StartCoroutine(ReturnAfterDeathRoutine());
            }
        }

        private void DropExperience()
        {
            if (experienceValue <= 0)
            {
                return;
            }

            SurvivorExperienceGem.Spawn(transform.position, experienceValue, experienceGemSprite);
        }

        private void OnDeathAnimationFinished(EnemyHealth deadHealth)
        {
            ReturnToPool();
        }

        private IEnumerator ReturnAfterDeathRoutine()
        {
            yield return new WaitForSeconds(returnToPoolDelay);

            if (waitForDeathAnimation && animator != null && animator.isActiveAndEnabled)
            {
                yield return WaitForDeathAnimation();
            }
            else
            {
                yield return new WaitForSeconds(deathAnimationFallbackDelay);
            }

            ReturnToPool();
        }

        private IEnumerator WaitForDeathAnimation()
        {
            float elapsed = 0f;
            bool sawDeathState = false;

            yield return null;

            while (elapsed < deathAnimationMaxWait)
            {
                elapsed += Time.deltaTime;

                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                bool isDeathState = IsDeathState(stateInfo);
                sawDeathState |= isDeathState;

                if (sawDeathState && isDeathState && !animator.IsInTransition(0) && stateInfo.normalizedTime >= 0.95f)
                {
                    yield break;
                }

                if (!sawDeathState && elapsed >= deathAnimationFallbackDelay)
                {
                    yield break;
                }

                yield return null;
            }
        }

        private bool IsDeathState(AnimatorStateInfo stateInfo)
        {
            if (!string.IsNullOrWhiteSpace(deathStateName) && stateInfo.IsName(deathStateName))
            {
                return true;
            }

            return stateInfo.IsName("Die") || stateInfo.IsName("Death") || stateInfo.IsName("Dead");
        }

        private void ReturnToPool()
        {
            if (returnedToPool)
            {
                return;
            }

            returnedToPool = true;

            if (pool != null)
            {
                pool.Return(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void DisableLegacyEnemyBehaviours()
        {
            EnemyController[] oldControllers = GetComponentsInChildren<EnemyController>(true);
            for (int i = 0; i < oldControllers.Length; i++)
            {
                oldControllers[i].enabled = false;
            }

            EnemyChargeController[] oldChargeControllers = GetComponentsInChildren<EnemyChargeController>(true);
            for (int i = 0; i < oldChargeControllers.Length; i++)
            {
                oldChargeControllers[i].enabled = false;
            }

            EnemyProjectileSpawner[] oldProjectileSpawners = GetComponentsInChildren<EnemyProjectileSpawner>(true);
            for (int i = 0; i < oldProjectileSpawners.Length; i++)
            {
                oldProjectileSpawners[i].enabled = false;
            }
        }

        private void CacheAnimatorParameters()
        {
            hasMoveXParameter = false;
            hasMoveYParameter = false;
            hasSpeedParameter = false;

            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].type != AnimatorControllerParameterType.Float)
                {
                    continue;
                }

                int nameHash = parameters[i].nameHash;
                hasMoveXParameter |= nameHash == MoveXHash;
                hasMoveYParameter |= nameHash == MoveYHash;
                hasSpeedParameter |= nameHash == SpeedHash;
            }
        }

        private void UpdateMoveAnimation(Vector2 direction, bool isMoving)
        {
            if (animator == null || !animator.isActiveAndEnabled)
            {
                return;
            }

            if (direction.sqrMagnitude > 0.0001f)
            {
                direction.Normalize();
            }
            else
            {
                direction = Vector2.down;
            }

            if (!useVerticalMoveAnimation)
            {
                if (Mathf.Abs(direction.x) > horizontalFacingDeadZone)
                {
                    lastHorizontalMoveDirection = Mathf.Sign(direction.x);
                }

                direction = new Vector2(lastHorizontalMoveDirection, 0f);
            }

            if (hasMoveXParameter)
            {
                animator.SetFloat(MoveXHash, direction.x);
            }

            if (hasMoveYParameter)
            {
                animator.SetFloat(MoveYHash, direction.y);
            }

            if (hasSpeedParameter)
            {
                animator.SetFloat(SpeedHash, isMoving ? 1f : 0f);
            }
        }
    }
}
