using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StrandedRoguelike
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class EnemyChargeController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 2.2f;
        [SerializeField, Min(0f)] private float aggroRange = 8f;
        [SerializeField, Min(0f)] private float chargeRange = 6f;
        [SerializeField, Min(0f)] private float keepDistance = 2.5f;

        [Header("Charge")]
        [SerializeField, Min(0f)] private float chargeDistance = 5.5f;
        [SerializeField, Min(0.01f)] private float chargeDuration = 0.35f;
        [SerializeField, Min(0f)] private float chargeWarningTime = 0.75f;
        [SerializeField, Min(0.01f)] private float chargeWarningWidth = 1.1f;
        [SerializeField, Min(0f)] private float chargeCooldown = 1.4f;
        [SerializeField, Min(0f)] private float chargeEndLag = 0.25f;
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField] private Sprite warningSprite;
        [SerializeField] private LayerMask obstacleLayers;

        [Header("Animation")]
        [SerializeField] private Animator animator;

        private static readonly int MoveX = Animator.StringToHash("MoveX");
        private static readonly int MoveY = Animator.StringToHash("MoveY");
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int Attack = Animator.StringToHash("Attack");

        private Rigidbody2D body;
        private SpriteRenderer spriteRenderer;
        private Transform target;
        private Vector2 moveDirection;
        private Vector2 facingDirection = Vector2.down;
        private float nextChargeTime;
        private bool isCharging;
        private bool isPreparingCharge;
        private readonly HashSet<PlayerHealth> damagedPlayers = new HashSet<PlayerHealth>();

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            body.gravityScale = 0f;
            body.freezeRotation = true;

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (TryGetComponent(out EnemyController enemyController))
            {
                enemyController.enabled = false;
            }
        }

        private void Update()
        {
            FindTargetIfNeeded();

            if (target == null || isPreparingCharge || isCharging)
            {
                moveDirection = Vector2.zero;
                UpdateAnimation();
                return;
            }

            Vector2 toTarget = target.position - transform.position;
            float distance = toTarget.magnitude;

            if (toTarget.sqrMagnitude > 0.01f)
            {
                facingDirection = ToEightDirections(toTarget);
            }

            if (distance <= chargeRange && distance <= aggroRange && Time.time >= nextChargeTime)
            {
                StartCoroutine(ChargeRoutine(toTarget.normalized));
                return;
            }

            if (distance > aggroRange || distance <= keepDistance)
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
            if (isPreparingCharge || isCharging)
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

        private IEnumerator ChargeRoutine(Vector2 direction)
        {
            if (direction == Vector2.zero)
            {
                direction = facingDirection == Vector2.zero ? Vector2.down : facingDirection;
            }

            direction.Normalize();
            isPreparingCharge = true;
            moveDirection = Vector2.zero;
            facingDirection = ToEightDirections(direction);
            damagedPlayers.Clear();
            UpdateAnimation();

            animator?.SetTrigger(Attack);

            float distance = ResolveChargeDistance(direction);
            ShowChargeWarning(direction, distance);

            if (chargeWarningTime > 0f)
            {
                yield return new WaitForSeconds(chargeWarningTime);
            }

            isPreparingCharge = false;
            isCharging = true;

            float elapsed = 0f;
            float chargeSpeed = distance / chargeDuration;

            while (elapsed < chargeDuration)
            {
                Vector2 step = direction * (chargeSpeed * Time.fixedDeltaTime);
                body.MovePosition(body.position + step);
                DamagePlayersInChargeBox(direction);

                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            isCharging = false;

            if (chargeEndLag > 0f)
            {
                yield return new WaitForSeconds(chargeEndLag);
            }

            nextChargeTime = Time.time + chargeCooldown;
        }

        private float ResolveChargeDistance(Vector2 direction)
        {
            if (obstacleLayers.value == 0)
            {
                int wallLayer = LayerMask.NameToLayer("Wall");
                if (wallLayer >= 0)
                {
                    obstacleLayers = 1 << wallLayer;
                }
            }

            if (obstacleLayers.value == 0)
            {
                return chargeDistance;
            }

            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, chargeDistance, obstacleLayers);
            return hit.collider == null ? chargeDistance : Mathf.Max(0.25f, hit.distance - 0.2f);
        }

        private void ShowChargeWarning(Vector2 direction, float distance)
        {
            Vector2 center = (Vector2)transform.position + direction * (distance * 0.5f);
            Vector2 size = new Vector2(distance, chargeWarningWidth);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            EnemyAttackWarning.ShowBox(center, size, angle, chargeWarningTime, warningSprite);
        }

        private void DamagePlayersInChargeBox(Vector2 direction)
        {
            Vector2 center = body.position + direction * 0.25f;
            Vector2 size = new Vector2(0.95f, chargeWarningWidth);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, angle);

            for (int i = 0; i < hits.Length; i++)
            {
                PlayerHealth playerHealth = hits[i].GetComponentInParent<PlayerHealth>();

                if (playerHealth == null || damagedPlayers.Contains(playerHealth))
                {
                    continue;
                }

                playerHealth.TakeDamageFromAttacker(damage, transform.position);
                damagedPlayers.Add(playerHealth);
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
                target = playerMovement.transform;
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

        private static Vector2 ToEightDirections(Vector2 input)
        {
            if (input == Vector2.zero)
            {
                return Vector2.down;
            }

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
