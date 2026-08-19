using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace StrandedRoguelike
{
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int DieHash = Animator.StringToHash("Die");
        private static readonly int DieStateHash = Animator.StringToHash("Base Layer.Die");

        [SerializeField, Min(1)] private int maxHealth = 6;
        [SerializeField, Min(1)] private int currentHealth;
        [SerializeField, Min(0f)] private float hitInvincibleTime = 0.45f;
        [SerializeField, Min(0f)] private float knockbackDistance = 0.65f;
        [SerializeField, Min(0.01f)] private float knockbackDuration = 0.12f;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsInvincible => invincibleTimer > 0f;

        private HitFlash HitFlash;
        private PlayerMovement movement;
        private float invincibleTimer;
        private Animator anim;
        private bool deathStarted;

        [SerializeField] private PlayerHPUI playerHPUI;

        [Header("Game Over")]
        [SerializeField] private GameObject gameoverUI;
        [SerializeField] private global::GameManager gameManager;
        [SerializeField, Min(0f)] private float gameOverDelay = 2f;

        private void Awake()
        {
            currentHealth = maxHealth;
            HitFlash = GetComponent<HitFlash>();
            movement = GetComponent<PlayerMovement>();
            anim = GetComponent<Animator>();
            if (anim == null)
            {
                anim = GetComponentInChildren<Animator>(true);
            }

            ResolveGameOverReferences();
        }

        private void Start()
        {
            UpdateHpUI();
        }

        private void Update()
        {
            if (invincibleTimer > 0f)
            {
                invincibleTimer -= Time.deltaTime;
            }
        }

        public void TakeDamage(int damage)
        {
            TakeDamage(damage, Vector2.zero, false);
        }

        public void TakeDamageFromAttacker(int damage, Vector2 attackerPosition)
        {
            Vector2 knockbackDirection = (Vector2)transform.position - attackerPosition;
            TakeDamage(damage, knockbackDirection, true);
        }

        public void TakeDamageFromProjectile(int damage, Vector2 projectileDirection)
        {
            TakeDamage(damage, projectileDirection, true);
        }

        public void SetInvincible(float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            invincibleTimer = Mathf.Max(invincibleTimer, duration);
        }

        public void IncreaseMaxAndCurrentHealth(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            maxHealth += amount;
            currentHealth += amount;
            UpdateHpUI();
        }

        public int IncreaseMaxHealthAndHealPercent(int amount, float healRatio)
        {
            if (amount <= 0)
            {
                return 0;
            }

            int previousHealth = currentHealth;
            maxHealth += amount;

            int healAmount = Mathf.CeilToInt(maxHealth * Mathf.Clamp01(healRatio));
            currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
            UpdateHpUI();

            return currentHealth - previousHealth;
        }

        private void TakeDamage(int damage, Vector2 knockbackDirection, bool useKnockback)
        {
            if (damage <= 0 || currentHealth <= 0)
            {
                return;
            }

            if (IsInvincible)
            {
                return;
            }

            currentHealth = Mathf.Max(0, currentHealth - damage);
            HitFlash?.Play();
            SetInvincible(hitInvincibleTime);
            UpdateHpUI();

            if (useKnockback && movement != null)
            {
                if (knockbackDirection == Vector2.zero)
                {
                    knockbackDirection = Vector2.down;
                }

                movement.ApplyKnockback(knockbackDirection, knockbackDistance, knockbackDuration);
            }

            if (currentHealth <= 0)
            {
                Die();
                Debug.Log("Player defeated");
            }
        }

        private void UpdateHpUI()
        {
            float ratio = Mathf.Clamp01((float)currentHealth / maxHealth);

            if (playerHPUI != null)
            {
                playerHPUI.SetHp(ratio);
            }
        }

        private void Die()
        {
            if (deathStarted)
            {
                return;
            }

            deathStarted = true;
            PlayDeathAnimation();

            DisableComponent();

            StartCoroutine(GameOverRoutine());
        }

        private void PlayDeathAnimation()
        {
            if (anim == null)
            {
                Debug.LogWarning($"{nameof(PlayerHealth)} could not find the player Animator.", this);
                return;
            }

            if (HasAnimatorParameter(anim, AttackHash, AnimatorControllerParameterType.Trigger))
            {
                anim.ResetTrigger(AttackHash);
            }

            if (HasAnimatorParameter(anim, DieHash, AnimatorControllerParameterType.Trigger))
            {
                anim.SetTrigger(DieHash);
            }

            if (anim.HasState(0, DieStateHash))
            {
                anim.Play(DieStateHash, 0, 0f);
                anim.Update(0f);
            }
        }

        private void DisableComponent()
        {
            if (movement != null)
            {
                movement.enabled = false;
            }

            PlayerAttack playerAttack = GetComponent<PlayerAttack>();
            if (playerAttack != null)
            {
                playerAttack.enabled = false;
            }

            SurvivorWeaponController weaponController = GetComponent<SurvivorWeaponController>();
            if (weaponController != null)
            {
                weaponController.enabled = false;
            }
        }

        private IEnumerator GameOverRoutine()
        {
            if (gameOverDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(gameOverDelay);
            }

            ResolveGameOverReferences();

            if (gameoverUI != null)
            {
                ShowPanelWithUnscaledTweens(gameoverUI);
            }
            else
            {
                Debug.LogWarning($"{nameof(PlayerHealth)} could not find the GameOver panel.", this);
            }

            if (gameManager != null)
            {
                gameManager.PauseGame();
            }
            else
            {
                Time.timeScale = 0f;
            }
        }

        private static void ShowPanelWithUnscaledTweens(GameObject panel)
        {
            ResultPanelTweenPlayer.Show(panel);
        }

        private void ResolveGameOverReferences()
        {
            if (gameoverUI == null)
            {
                GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
                for (int i = 0; i < objects.Length; i++)
                {
                    if (objects[i] == null || !objects[i].scene.IsValid())
                    {
                        continue;
                    }

                    string normalizedName = objects[i].name.Replace(" ", string.Empty)
                        .Replace("_", string.Empty)
                        .Replace("-", string.Empty)
                        .ToLowerInvariant();

                    if (normalizedName == "gameover" || normalizedName == "gameoverpanel")
                    {
                        gameoverUI = objects[i];
                        break;
                    }
                }
            }

            if (gameManager == null)
            {
                global::GameManager[] managers = Resources.FindObjectsOfTypeAll<global::GameManager>();
                for (int i = 0; i < managers.Length; i++)
                {
                    if (managers[i] != null && managers[i].gameObject.scene.IsValid())
                    {
                        gameManager = managers[i];
                        break;
                    }
                }
            }
        }

        private static bool HasAnimatorParameter(
            Animator animator,
            int parameterHash,
            AnimatorControllerParameterType parameterType)
        {
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].nameHash == parameterHash && parameters[i].type == parameterType)
                {
                    return true;
                }
            }

            return false;
        }

        public void DestroyObject()
        {
            if (!deathStarted)
            {
                return;
            }

            HideDeadPlayer();
        }

        private void HideDeadPlayer()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = false;
            }

            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            Rigidbody2D playerRigidbody = GetComponent<Rigidbody2D>();
            if (playerRigidbody != null)
            {
                playerRigidbody.linearVelocity = Vector2.zero;
                playerRigidbody.angularVelocity = 0f;
                playerRigidbody.simulated = false;
            }

            if (anim != null)
            {
                anim.enabled = false;
            }
        }
    }

    internal static class ResultPanelTweenPlayer
    {
        public static void Show(GameObject panel)
        {
            if (panel == null)
            {
                return;
            }

            DOTweenAnimation[] animations = panel.GetComponentsInChildren<DOTweenAnimation>(true);

            for (int i = 0; i < animations.Length; i++)
            {
                DOTweenAnimation animation = animations[i];
                if (animation.GetComponent<global::GoToMainMenu>() != null)
                {
                    continue;
                }

                PrepareFade(animation);
            }

            panel.SetActive(true);

            for (int i = 0; i < animations.Length; i++)
            {
                DOTweenAnimation animation = animations[i];
                if (animation.GetComponent<global::GoToMainMenu>() != null)
                {
                    continue;
                }

                PlayFade(animation);
            }
        }

        private static void PrepareFade(DOTweenAnimation animation)
        {
            animation.DOKill();
            animation.autoGenerate = false;
            animation.autoPlay = false;
            animation.isActive = false;
            animation.isIndependentUpdate = true;

            Graphic graphic = GetTarget<Graphic>(animation);
            if (graphic != null)
            {
                Color color = graphic.color;
                color.a = 0f;
                graphic.color = color;
                return;
            }

            CanvasGroup canvasGroup = GetTarget<CanvasGroup>(animation);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                return;
            }

            SpriteRenderer spriteRenderer = GetTarget<SpriteRenderer>(animation);
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = 0f;
                spriteRenderer.color = color;
            }
        }

        private static void PlayFade(DOTweenAnimation animation)
        {
            Tweener fadeTween = null;

            Graphic graphic = GetTarget<Graphic>(animation);
            if (graphic != null)
            {
                fadeTween = graphic.DOFade(animation.endValueFloat, animation.duration);
            }
            else
            {
                CanvasGroup canvasGroup = GetTarget<CanvasGroup>(animation);
                if (canvasGroup != null)
                {
                    fadeTween = canvasGroup.DOFade(animation.endValueFloat, animation.duration);
                }
                else
                {
                    SpriteRenderer spriteRenderer = GetTarget<SpriteRenderer>(animation);
                    if (spriteRenderer != null)
                    {
                        fadeTween = spriteRenderer.DOFade(animation.endValueFloat, animation.duration);
                    }
                }
            }

            if (fadeTween == null)
            {
                return;
            }

            fadeTween
                .SetDelay(animation.delay)
                .SetLoops(animation.loops, animation.loopType)
                .SetAutoKill(animation.autoKill)
                .SetUpdate(UpdateType.Normal, true);

            if (animation.easeType == Ease.INTERNAL_Custom)
            {
                fadeTween.SetEase(animation.easeCurve);
            }
            else
            {
                fadeTween.SetEase(animation.easeType);
            }
        }

        private static T GetTarget<T>(DOTweenAnimation animation) where T : Component
        {
            if (animation.target is T configuredTarget)
            {
                return configuredTarget;
            }

            return animation.GetComponent<T>();
        }
    }
}
