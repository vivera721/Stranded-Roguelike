using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace StrandedRoguelike
{
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerAttack : MonoBehaviour
    {
        public event System.Action<EnemyHealth, Vector2> EnemyHit;
        public event System.Action<Vector2, GameObject> AttackVisualStarted;

        [SerializeField, Min(0f)] private float attackCooldown = 0.1f;
        [SerializeField, Min(0f)] private float attackRecoveryTime = 0.08f;
        [SerializeField, Min(0f)] private float movementLockTime = 0.14f;
        [SerializeField, Min(0f)] private float slashDistance = 0.55f;
        [SerializeField] private Vector2 slashOffset = Vector2.zero;
        [SerializeField] private float slashRotationOffset = 0f;
        [SerializeField, Min(0.01f)] private float slashActiveTime = 0.25f;
        [SerializeField, FormerlySerializedAs("whiteSlash_VFX")] private GameObject slashVFXObject;
        [SerializeField, Min(1)] private int attackDamage = 1;
        [SerializeField, Min(0f)] private float attackDistance = 0.6f;
        [SerializeField] private Vector2 attackBoxSize = new Vector2(0.85f, 0.65f);
        [Header("Upgrade Runtime")]
        [SerializeField] private bool thirdSlashUpgradeEnabled;
        [SerializeField, Min(0)] private int thirdSlashBonusDamage = 2;
        [SerializeField, Min(1f)] private float thirdSlashRangeMultiplier = 1.35f;
        [SerializeField] private bool afterDodgeSlashUpgradeEnabled;
        [SerializeField, Min(0)] private int afterDodgeSlashBonusDamage = 2;
        [SerializeField, Min(0f)] private float afterDodgeSlashWindow = 0.45f;

        private static readonly int Attack = Animator.StringToHash("Attack");
        private const string AttackState = "Attack";

        private PlayerMovement movement;
        private Animator animator;
        private InputAction attackAction;
        private bool isAttacking;
        private Coroutine slashRoutine;
        private int comboCount;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            animator = GetComponent<Animator>();

            if (slashVFXObject == null)
            {
                slashVFXObject = FindSlashVFXObject();
            }

            if (slashVFXObject != null)
            {
                slashVFXObject.SetActive(false);
            }

            attackAction = new InputAction("Attack", InputActionType.Button);
            attackAction.AddBinding("<Keyboard>/space");
            attackAction.AddBinding("<Mouse>/leftButton");
            attackAction.AddBinding("<Gamepad>/buttonSouth");
        }

        private void OnEnable()
        {
            attackAction.performed += OnAttack;
            attackAction.Enable();
        }

        private void OnDisable()
        {
            attackAction.performed -= OnAttack;
            attackAction.Disable();
            StopAllCoroutines();
            isAttacking = false;
            slashRoutine = null;

            if (slashVFXObject != null)
            {
                slashVFXObject.SetActive(false);
            }

            if (movement != null)
            {
                movement.SetMovementLocked(false);
            }
        }

        private void OnDestroy()
        {
            attackAction.Dispose();
        }

        private void OnAttack(InputAction.CallbackContext context)
        {
            if (!isAttacking)
            {
                StartCoroutine(AttackRoutine());
            }
        }

        private IEnumerator AttackRoutine()
        {
            isAttacking = true;
            comboCount++;
            movement.SetMovementLocked(true);
            animator.SetTrigger(Attack);
            Vector2 attackDirection = movement.FacingDirection;
            bool isThirdSlash = thirdSlashUpgradeEnabled && comboCount % 3 == 0;
            bool isAfterDodgeSlash = afterDodgeSlashUpgradeEnabled && movement.IsInAfterDodgeSlashWindow(afterDodgeSlashWindow);

            PlaySlashVFX(attackDirection, isThirdSlash);
            DealDamage(attackDirection, isThirdSlash, isAfterDodgeSlash);

            if (movementLockTime > 0f)
            {
                yield return new WaitForSeconds(movementLockTime);
            }


            movement.SetMovementLocked(false);

            if (attackRecoveryTime > 0f)
            {
                yield return new WaitForSeconds(attackRecoveryTime);
            }

            if (attackCooldown > 0f)
            {
                yield return new WaitForSeconds(attackCooldown);
            }

            isAttacking = false;
        }

        private void DealDamage(Vector2 direction, bool isThirdSlash, bool isAfterDodgeSlash)
        {
            if (direction == Vector2.zero)
            {
                direction = Vector2.down;
            }

            direction.Normalize();

            int damage = attackDamage;
            float rangeMultiplier = 1f;

            if (isThirdSlash)
            {
                damage += thirdSlashBonusDamage;
                rangeMultiplier *= thirdSlashRangeMultiplier;
            }

            if (isAfterDodgeSlash)
            {
                damage += afterDodgeSlashBonusDamage;
            }

            Vector2 currentBoxSize = attackBoxSize * rangeMultiplier;
            Vector2 center = (Vector2)transform.position + direction * (attackDistance * rangeMultiplier);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, currentBoxSize, angle);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].TryGetComponent(out EnemyHealth enemyHealth))
                {
                    enemyHealth.TakeDamage(damage, transform.position);
                    EnemyHit?.Invoke(enemyHealth, direction);
                }
            }
        }

        private void PlaySlashVFX(Vector2 direction, bool isThirdSlash)
        {
            if (slashVFXObject == null)
            {
                return;
            }

            if (direction == Vector2.zero)
            {
                direction = Vector2.down;
            }

            direction.Normalize();

            Transform effectTransform = slashVFXObject.transform;
            effectTransform.localPosition = slashOffset + direction * slashDistance;
            effectTransform.localScale = Vector3.one * (isThirdSlash ? 1.25f : 1f);

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            effectTransform.localRotation = Quaternion.Euler(0f, 0f, angle + slashRotationOffset);

            if (slashRoutine != null)
            {
                StopCoroutine(slashRoutine);
            }

            slashRoutine = StartCoroutine(PlaySlashRoutine());
            AttackVisualStarted?.Invoke(direction, slashVFXObject);
        }

        private IEnumerator PlaySlashRoutine()
        {
            slashVFXObject.SetActive(false);
            yield return null;
            slashVFXObject.SetActive(true);

            yield return new WaitForSeconds(slashActiveTime);

            slashVFXObject.SetActive(false);
            slashRoutine = null;
        }

        private GameObject FindSlashVFXObject()
        {
            foreach (Transform child in transform)
            {
                string childName = child.name.ToLowerInvariant();

                if (childName.Contains("slash") || childName.Contains("vfx"))
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        public void AddAttackDamage(int amount)
        {
            attackDamage = Mathf.Max(1, attackDamage + amount);
        }

        public void MultiplySlashRange(float multiplier)
        {
            multiplier = Mathf.Max(0.1f, multiplier);
            attackDistance *= multiplier;
            attackBoxSize *= multiplier;
            slashDistance *= multiplier;
        }

        public void ReduceAttackCooldown(float amount)
        {
            attackCooldown = Mathf.Max(0.02f, attackCooldown - Mathf.Abs(amount));
        }

        public void ReduceAttackRecovery(float amount)
        {
            attackRecoveryTime = Mathf.Max(0f, attackRecoveryTime - Mathf.Abs(amount));
            movementLockTime = Mathf.Max(0.04f, movementLockTime - Mathf.Abs(amount) * 0.5f);
        }

        public void EnableThirdSlashUpgrade()
        {
            thirdSlashUpgradeEnabled = true;
        }

        public void EnableAfterDodgeSlashUpgrade()
        {
            afterDodgeSlashUpgradeEnabled = true;
        }
    }
}
