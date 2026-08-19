using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StrandedRoguelike
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float moveSpeed = 4f;
        [SerializeField] private bool faceMouse = true;
        [Header("Dash / Dodge")]
        [SerializeField] private bool dodgeEnabled = true;
        [SerializeField, Min(0f)] private float dodgeDistance = 2.1f;
        [SerializeField, Min(0.01f)] private float dodgeDuration = 0.12f;
        [SerializeField, Min(0f)] private float dodgeCooldown = 0.22f;
        [SerializeField, Min(0f)] private float dodgeInvincibleExtraTime = 0.08f;
        [SerializeField] private AnimationCurve dodgeSpeedCurve = new AnimationCurve(
            new Keyframe(0f, 1.8f),
            new Keyframe(0.25f, 1.25f),
            new Keyframe(1f, 0.35f));
        [SerializeField, Min(0f)] private float afterDodgeSlashWindow = 0.45f;
        [SerializeField] private Animator animator;

        private bool isDodgeCoolingDown;
        private Vector2 dodgeDirection;
        private float dodgeTimer;
        private float dodgeSpeed;
        private float dodgeElapsed;
        private float lastDodgeTime = -999f;

        private static readonly int MoveX = Animator.StringToHash("MoveX");
        private static readonly int MoveY = Animator.StringToHash("MoveY");
        private static readonly int Speed = Animator.StringToHash("Speed");

        private Rigidbody2D body;
        private SpriteRenderer spriteRenderer;
        private PlayerHealth health;
        private InputAction moveAction;
        private InputAction dodgeAction;
        private Vector2 moveInput;
        private Vector2 moveDirection;
        private Vector2 facingDirection = Vector2.down;
        private bool movementLocked;
        private bool isDodging;
        private bool isKnockbacking;

        public Vector2 FacingDirection => facingDirection;
        public Vector2 MoveDirection => moveDirection;
        public bool IsMovementLocked => movementLocked;
        public bool IsDodging => isDodging;
        public float LastDodgeTime => lastDodgeTime;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            health = GetComponent<PlayerHealth>();

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick");
            moveAction.AddBinding("<Gamepad>/dpad");

            dodgeAction = new InputAction("Dodge", InputActionType.Button);
            dodgeAction.AddBinding("<Keyboard>/leftShift");
            dodgeAction.AddBinding("<Keyboard>/rightShift");
            dodgeAction.AddBinding("<Gamepad>/buttonEast");
        }

        private void OnEnable()
        {
            moveAction.Enable();

            if (dodgeEnabled)
            {
                dodgeAction.performed += OnDodge;
                dodgeAction.Enable();
            }
        }

        private void OnDisable()
        {
            dodgeAction.performed -= OnDodge;
            if (dodgeAction.enabled)
            {
                dodgeAction.Disable();
            }
            moveAction.Disable();
            StopAllCoroutines();
            isDodging = false;
            isKnockbacking = false;
            movementLocked = false;
        }

        private void OnDestroy()
        {
            dodgeAction.Dispose();
            moveAction.Dispose();
        }

        private void Update()
        {
            moveInput = movementLocked || isDodging || isKnockbacking
                ? Vector2.zero
                : ReadEightDirectionInput(moveAction.ReadValue<Vector2>());
            moveDirection = moveInput == Vector2.zero
                ? Vector2.zero
                : ToEightDirections(moveInput);

            Vector2 mouseDirection = faceMouse ? ReadMouseDirection() : Vector2.zero;
            if (mouseDirection != Vector2.zero)
            {
                facingDirection = ToEightDirections(mouseDirection);
            }
            else if (moveDirection != Vector2.zero)
            {
                facingDirection = moveDirection;
            }

            spriteRenderer.flipX = facingDirection.x < 0f;

            if (animator != null)
            {
                animator.SetFloat(MoveX, facingDirection.x);
                animator.SetFloat(MoveY, facingDirection.y);
                animator.SetFloat(Speed, moveInput.sqrMagnitude);
            }
        }

        private void FixedUpdate()
        {
            if (isDodging)
            {
                dodgeElapsed += Time.fixedDeltaTime;
                float t = Mathf.Clamp01(dodgeElapsed / dodgeDuration);
                float speedMultiplier = Mathf.Max(0f, dodgeSpeedCurve.Evaluate(t));
                body.MovePosition(body.position + dodgeDirection * (dodgeSpeed * speedMultiplier * Time.fixedDeltaTime));
                return;
            }

            if (isKnockbacking)
            {
                return;
            }

            body.MovePosition(body.position + moveInput * (moveSpeed * Time.fixedDeltaTime));
        }

        public void SetMovementLocked(bool locked)
        {
            movementLocked = locked;

            if (locked)
            {
                moveInput = Vector2.zero;

                if (animator != null)
                {
                    animator.SetFloat(Speed, 0f);
                }
            }
        }

        private void OnDodge(InputAction.CallbackContext context)
        {
            if (!dodgeEnabled)
                return;

            if (movementLocked || isDodging || isDodgeCoolingDown)
                return;

            Vector2 direction = moveDirection;

            if(direction == Vector2.zero)
            {
                direction = facingDirection;
            }

            if (direction == Vector2.zero) 
                return;

            StartCoroutine(DodgeRoutine(direction));
        }

        private IEnumerator DodgeRoutine(Vector2 direction)
        {
            isDodging = true;
            health?.SetInvincible(dodgeDuration + dodgeInvincibleExtraTime);

            dodgeDirection = direction.normalized;
            dodgeSpeed = dodgeDistance / dodgeDuration;
            dodgeElapsed = 0f;
            lastDodgeTime = Time.time;
            
            float elapsed = 0f;

            while (elapsed < dodgeDuration)
            {
                elapsed += Time.fixedDeltaTime;

                yield return new WaitForFixedUpdate();
            }

            isDodging = false;

            if (dodgeCooldown > 0f)
            {
                isDodgeCoolingDown = true;
                yield return new WaitForSeconds(dodgeCooldown);
                isDodgeCoolingDown = false;
            }

            isDodging = false;
        }

        public bool IsInAfterDodgeSlashWindow(float customWindow = -1f)
        {
            float window = customWindow >= 0f ? customWindow : afterDodgeSlashWindow;
            return Time.time - lastDodgeTime <= window;
        }

        public void ReduceDodgeCooldown(float amount)
        {
            dodgeCooldown = Mathf.Max(0.05f, dodgeCooldown - Mathf.Abs(amount));
        }

        public void SetDodgeEnabled(bool enabled)
        {
            if (dodgeEnabled == enabled)
            {
                return;
            }

            dodgeEnabled = enabled;

            if (enabled && isActiveAndEnabled)
            {
                dodgeAction.performed += OnDodge;
                dodgeAction.Enable();
            }
            else
            {
                dodgeAction.performed -= OnDodge;
                if (dodgeAction.enabled)
                {
                    dodgeAction.Disable();
                }

                isDodging = false;
                isDodgeCoolingDown = false;
            }
        }

        public void SetFaceMouse(bool enabled)
        {
            faceMouse = enabled;
        }

        public void AddDodgeDistance(float amount)
        {
            dodgeDistance = Mathf.Max(0.5f, dodgeDistance + amount);
        }

        public void ApplyKnockback(Vector2 direction, float distance, float duration)
        {
            if (direction == Vector2.zero || distance <= 0f || duration <= 0f)
            {
                return;
            }

            StartCoroutine(KnockbackRoutine(direction.normalized, distance, duration));
        }

        private IEnumerator KnockbackRoutine(Vector2 direction, float distance, float duration)
        {
            isKnockbacking = true;

            float elapsed = 0f;
            float knockbackSpeed = distance / duration;

            while (elapsed < duration)
            {
                body.MovePosition(body.position + direction * (knockbackSpeed * Time.fixedDeltaTime));
                elapsed += Time.fixedDeltaTime;

                yield return new WaitForFixedUpdate();
            }

            isKnockbacking = false;
        }

        public void MoveBy(Vector2 direction, float distance)
        {
            if (direction == Vector2.zero || distance <= 0f)
            {
                return;
            }

            body.MovePosition(body.position + direction.normalized * distance);
        }

        private Vector2 ReadMouseDirection()
        {
            if (Mouse.current == null || Camera.main == null)
            {
                return Vector2.zero;
            }

            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);
            Vector2 direction = mouseWorldPosition - transform.position;

            return direction.sqrMagnitude < 0.01f
                ? Vector2.zero
                : direction.normalized;
        }

        private static Vector2 ReadEightDirectionInput(Vector2 input)
        {
            if (input.sqrMagnitude < 0.01f)
            {
                return Vector2.zero;
            }

            return input.normalized;
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
