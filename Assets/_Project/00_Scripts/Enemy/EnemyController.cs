using System.Collections;
using UnityEngine;

namespace StrandedRoguelike
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class EnemyController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 2f;
        [SerializeField, Min(0f)] private float stopDistance = 1.2f;
        [SerializeField, Min(0f)] private float aggroRange = 8f;

        [Header("Attack")]
        [SerializeField] private EnemyAttackKind attackKind = EnemyAttackKind.Melee;
        [SerializeField, Min(0.1f)] private float attackRange = 1.2f;
        [SerializeField, Min(0f)] private float attackCooldown = 1.2f;
        [SerializeField, Min(0f)] private float attackWarningTime = 0.45f;
        [SerializeField, Min(1)] private int damage = 1;
        
        [Header("Attack Timing")]
        [SerializeField, Min(0f)] private float attackEndLagTime = 0.2f;
        [SerializeField, Min(0f)] private float attackRecoilDistance = 0.18f;
        [SerializeField, Min(0.01f)] private float attackRecoilDuration = 0.08f;
        [SerializeField] private bool lockPhysicsWhileAttacking = true;

        [Header("Melee")]
        [SerializeField] private Vector2 meleeBoxSize = new Vector2(0.9f, 0.7f);
        [SerializeField, Min(0f)] private float meleeBoxDistance = 0.65f;
        [SerializeField, Min(0f)] private float meleeAreaRadius = 1.1f;

        [Header("Ranged")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField, Min(0f)] private float projectileSpeed = 5f;
        [SerializeField, Min(0.01f)] private float projectileLifeTime = 4f;
        [SerializeField, Min(1)] private int projectileCount = 1;
        [SerializeField, Range(0f, 360f)] private float spreadAngle = 25f;
        [SerializeField] private bool radialProjectilePattern;
        [SerializeField, Min(0f)] private float rangedAreaRadius = 1.2f;
        [SerializeField] private bool useRangedProjectileRecoil = true;
        [SerializeField] private EnemyProjectileSpawner projectileSpawner;
        [Header("Animation")]
        [SerializeField] private Animator animator;

        private static readonly int MoveX = Animator.StringToHash("MoveX");
        private static readonly int MoveY = Animator.StringToHash("MoveY");
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int Attack = Animator.StringToHash("Attack");

        private Rigidbody2D body;
        private RigidbodyType2D defaultBodyType;
        private SpriteRenderer spriteRenderer;
        private Transform target;
        private Vector2 moveDirection;
        private Vector2 facingDirection = Vector2.down;
        private float nextAttackTime;
        private bool isAttacking;

        private Vector2 lockedAttackDirection;
        private Vector2 lockedAttackPosition;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            defaultBodyType = body.bodyType;
            spriteRenderer = GetComponent<SpriteRenderer>();

            body.gravityScale = 0f;
            body.freezeRotation = true;

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        private void Update()
        {
            FindTargetIfNeeded();

            if (target == null)
            {
                moveDirection = Vector2.zero;
                UpdateAnimation();
                return;
            }

            if (isAttacking)
            {
                moveDirection = Vector2.zero;
                facingDirection = lockedAttackDirection;
                UpdateAnimation();
                return;
            }

            Vector2 toTarget = target.position - transform.position;
            float distanceToTarget = toTarget.magnitude;

            if (toTarget.sqrMagnitude > 0.01f)
            {
                facingDirection = ToEightDirections(toTarget);
            }

            bool targetInAggroRange = distanceToTarget <= aggroRange;
            bool targetInAttackRange = distanceToTarget <= attackRange;

            if (targetInAggroRange && targetInAttackRange && Time.time >= nextAttackTime)
            {
                StartCoroutine(AttackRoutine());
            }

            if (!targetInAggroRange || distanceToTarget <= stopDistance)
            {
                moveDirection = Vector2.zero;
            }
            else
            {
                moveDirection = toTarget.normalized;
            }

            UpdateAnimation();
        }

        private void FixedUpdate()
        {
            if (isAttacking)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            if (moveDirection == Vector2.zero)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            body.MovePosition(body.position + moveDirection * (moveSpeed * Time.fixedDeltaTime));
        }

        private void OnDisable()
        {
            if (body != null)
            {
                body.bodyType = defaultBodyType;
            }
        }

        private IEnumerator AttackRoutine()
        {
            isAttacking = true;
            SetAttackPhysicsLocked(true);

            lockedAttackDirection = facingDirection == Vector2.zero ? Vector2.down : facingDirection;

            lockedAttackPosition = body.position;

            moveDirection = Vector2.zero;
            body.linearVelocity = Vector2.zero;

            facingDirection = lockedAttackDirection;
            UpdateAnimation();

            animator?.SetTrigger(Attack);

            switch (attackKind)
            {
                case EnemyAttackKind.Melee:
                    yield return MeleeAttackRoutine();
                    break;

                case EnemyAttackKind.MeleeArea:
                    yield return MeleeAreaAttackRoutine();
                    break;

                case EnemyAttackKind.RangedProjectile:
                    yield return RangedProjectileAttackRoutine();
                    break;

                case EnemyAttackKind.RangedArea:
                    yield return RangedAreaAttackRoutine();
                    break;
            }

            if(attackEndLagTime > 0f)
            {
                yield return new WaitForSeconds(attackEndLagTime);
            }
            
            nextAttackTime = Time.time + attackCooldown;
            SetAttackPhysicsLocked(false);
            isAttacking = false;
        }

        private IEnumerator MeleeAttackRoutine()
        {
            Vector2 attackDirection = lockedAttackDirection == Vector2.zero ? Vector2.down : lockedAttackDirection;
            Vector2 center = lockedAttackPosition + attackDirection * meleeBoxDistance;
            float angle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;

            EnemyAttackWarning.ShowBox(center, meleeBoxSize, angle, attackWarningTime);
            yield return new WaitForSeconds(attackWarningTime);

            Collider2D[] hits = Physics2D.OverlapBoxAll(center, meleeBoxSize, angle);
            DamagePlayers(hits);
            ApplyAttackRecoil(attackDirection);
        }

        private IEnumerator MeleeAreaAttackRoutine()
        {
            Vector2 center = transform.position;

            EnemyAttackWarning.ShowCircle(center, meleeAreaRadius, attackWarningTime);
            yield return new WaitForSeconds(attackWarningTime);

            Collider2D[] hits = Physics2D.OverlapCircleAll(center, meleeAreaRadius);
            DamagePlayers(hits);
            ApplyAttackRecoil(lockedAttackDirection);
        }

        private IEnumerator RangedProjectileAttackRoutine()
        {
            Vector2 baseDirection = lockedAttackDirection == Vector2.zero ? Vector2.down : lockedAttackDirection;
            
            if (attackWarningTime > 0f)
            {
                yield return new WaitForSeconds(attackWarningTime);
            }

            if (radialProjectilePattern)
            {
                projectileSpawner.FireRadial(
                    projectileCount,
                    projectileSpeed,
                    damage,
                    projectileLifeTime
                );
            }
            else
            {
                projectileSpawner.FireSpread(
                    baseDirection,
                    projectileCount,
                    spreadAngle,
                    projectileSpeed,
                    damage,
                    projectileLifeTime
                );
            }

            if (useRangedProjectileRecoil)
            {
                ApplyAttackRecoil(baseDirection);
            }
        }

        private IEnumerator RangedAreaAttackRoutine()
        {
            if (target == null)
            {
                yield break;
            }

            Vector2 center = target.position;

            EnemyAttackWarning.ShowCircle(center, rangedAreaRadius, attackWarningTime);
            yield return new WaitForSeconds(attackWarningTime);

            Collider2D[] hits = Physics2D.OverlapCircleAll(center, rangedAreaRadius);
            DamagePlayers(hits);
            ApplyAttackRecoil(lockedAttackDirection);
        }

        private void FireSpreadProjectiles(Vector2 baseDirection)
        {
            int count = Mathf.Max(1, projectileCount);
            float startAngle = count == 1 ? 0f : -spreadAngle * 0.5f;
            float angleStep = count == 1 ? 0f : spreadAngle / (count - 1);

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + angleStep * i;
                Vector2 direction = Rotate(baseDirection, angle);
                SpawnProjectile(direction);
            }
        }

        private void FireRadialProjectiles()
        {
            int count = Mathf.Max(1, projectileCount);

            for (int i = 0; i < count; i++)
            {
                float angle = 360f / count * i;
                Vector2 direction = Rotate(Vector2.right, angle);
                SpawnProjectile(direction);
            }
        }

        private void SpawnProjectile(Vector2 direction)
        {
            GameObject projectileObject = projectilePrefab != null
                ? Instantiate(projectilePrefab, transform.position, Quaternion.identity)
                : CreateDefaultProjectile();

            if (!projectileObject.TryGetComponent(out EnemyProjectile projectile))
            {
                projectile = projectileObject.AddComponent<EnemyProjectile>();
            }

            projectile.Launch(direction, projectileSpeed, damage, projectileLifeTime);
        }

        private GameObject CreateDefaultProjectile()
        {
            GameObject projectileObject = new GameObject("Enemy Projectile");
            projectileObject.transform.position = transform.position;

            SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateProjectileSprite();
            renderer.color = new Color(1f, 0.2f, 0.05f, 1f);
            renderer.sortingOrder = 25;

            CircleCollider2D collider = projectileObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.12f;
            collider.isTrigger = true;

            return projectileObject;
        }

        private Sprite CreateProjectileSprite()
        {
            const int size = 16;
            Texture2D texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;

            Vector2 center = new Vector2(size - 1, size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    texture.SetPixel(x, y, distance <= 6f ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 32f);
        }

        private void DamagePlayers(Collider2D[] hits)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                PlayerHealth playerHealth = hits[i].GetComponent<PlayerHealth>();

                if (playerHealth == null && hits[i].GetComponent<PlayerMovement>() != null)
                {
                    playerHealth = hits[i].gameObject.AddComponent<PlayerHealth>();
                }

                if (playerHealth != null)
                {
                    playerHealth.TakeDamageFromAttacker(damage, transform.position);
                }
            }
        }

        private void ApplyAttackRecoil(Vector2 attackDirection)
        {
            if (attackRecoilDistance <= 0f || attackDirection == Vector2.zero)
            {
                return;
            }

            StartCoroutine(AttackRecoilRoutine(-attackDirection.normalized));
        }

        private void SetAttackPhysicsLocked(bool locked)
        {
            if (!lockPhysicsWhileAttacking)
            {
                return;
            }

            body.linearVelocity = Vector2.zero;
            body.bodyType = locked ? RigidbodyType2D.Kinematic : defaultBodyType;
        }

        private IEnumerator AttackRecoilRoutine(Vector2 recoilDirection)
        {
            float elapsed = 0f;
            float recoilSpeed = attackRecoilDistance / attackRecoilDuration;

            while (elapsed < attackRecoilDuration)
            {
                body.MovePosition(body.position + recoilDirection * (recoilSpeed * Time.fixedDeltaTime));
                elapsed += Time.fixedDeltaTime;

                yield return new WaitForFixedUpdate();
            }
        }

        private void FindTargetIfNeeded()
        {
            if (target != null)
            {
                return;
            }

            PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth != null)
            {
                target = playerHealth.transform;
                return;
            }

            PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
            if (playerMovement != null)
            {
                if (playerMovement.GetComponent<PlayerHealth>() == null)
                {
                    playerMovement.gameObject.AddComponent<PlayerHealth>();
                }

                target = playerMovement.transform;
                return;
            }

            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                if (player.GetComponent<PlayerHealth>() == null)
                {
                    player.AddComponent<PlayerHealth>();
                }

                target = player.transform;
            }
        }

        private void UpdateAnimation()
        {
            spriteRenderer.flipX = facingDirection.x < 0f;

            if (animator == null)
            {
                return;
            }

            animator.SetFloat(MoveX, facingDirection.x);
            animator.SetFloat(MoveY, facingDirection.y);
            animator.SetFloat(Speed, moveDirection.sqrMagnitude);
        }

        private static Vector2 Rotate(Vector2 direction, float angle)
        {
            float radians = angle * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);

            return new Vector2(
                direction.x * cos - direction.y * sin,
                direction.x * sin + direction.y * cos).normalized;
        }

        private static Vector2 ToEightDirections(Vector2 input)
        {
            float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
            float snappedAngle = Mathf.Round(angle / 45f) * 45f * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(snappedAngle), Mathf.Sin(snappedAngle));

            if (Mathf.Abs(direction.x) < 0.01f)
            {
                direction.x = 0f;
            }

            if (Mathf.Abs(direction.y) < 0.01f)
            {
                direction.y = 0f;
            }

            return direction.normalized;
        }
    }
}
