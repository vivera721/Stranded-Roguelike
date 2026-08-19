using System;
using UnityEngine;

namespace StrandedRoguelike
{
    public sealed class ArcProjectile : MonoBehaviour
    {
        [SerializeField] private ArcProjectileSettings settings = new ArcProjectileSettings();
        [SerializeField] private bool rotateWhileFlying = true;
        [SerializeField] private float rotationSpeed = 720f;
        [SerializeField, Min(0f)] private float destroyDelayAfterImpact = 0.03f;
        [SerializeField, Min(0.05f)] private float impactVFXLifetime = 1.5f;

        private SpriteRenderer bodyRenderer;
        private SpriteRenderer shadowRenderer;
        private Sprite shadowSprite;
        private Vector2 startPosition;
        private Vector2 targetPosition;
        private float elapsed;
        private bool launched;
        private Action<Vector2> impactCallback;

        private void Awake()
        {
            bodyRenderer = GetComponent<SpriteRenderer>();
            shadowSprite = bodyRenderer != null ? bodyRenderer.sprite : null;
            EnsureShadow();
        }

        public void Configure(ArcProjectileSettings newSettings, Action<Vector2> newImpactCallback = null)
        {
            if (newSettings != null)
            {
                settings = newSettings;
            }

            impactCallback = newImpactCallback;
        }

        public void Launch(Vector2 start, Vector2 target)
        {
            startPosition = start;
            targetPosition = target;
            elapsed = 0f;
            launched = true;
            transform.position = start;
            EnsureShadow();
            UpdateShadow(start, 0f);
        }

        private void Update()
        {
            if (!launched)
            {
                return;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, settings.flightTime));
            Vector2 groundPosition = Vector2.Lerp(startPosition, targetPosition, t);
            float height = Mathf.Sin(t * Mathf.PI) * settings.arcHeight;

            transform.position = groundPosition + Vector2.up * height;

            if (rotateWhileFlying)
            {
                transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
            }

            if (shadowRenderer != null)
            {
                float heightRatio = Mathf.Clamp01(height / Mathf.Max(0.01f, settings.arcHeight));
                UpdateShadow(groundPosition, heightRatio);
            }

            if (t >= 1f)
            {
                Impact();
            }
        }

        private void Impact()
        {
            launched = false;
            transform.position = targetPosition;

            if (settings.impactVFXPrefab != null)
            {
                GameObject impactVFX = Instantiate(settings.impactVFXPrefab, targetPosition, Quaternion.identity);
                RuntimeObjectLifetime.Attach(impactVFX, impactVFXLifetime);
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(targetPosition, settings.impactRadius, settings.enemyLayers);

            for (int i = 0; i < hits.Length; i++)
            {
                if (!hits[i].TryGetComponent(out EnemyHealth enemyHealth) || enemyHealth.isDead)
                {
                    continue;
                }

                if (settings.impactDamage > 0)
                {
                    enemyHealth.TakeDamage(settings.impactDamage, targetPosition);
                }

                EnemyStatusEffects status = enemyHealth.GetComponent<EnemyStatusEffects>();
                if (status == null)
                {
                    status = enemyHealth.gameObject.AddComponent<EnemyStatusEffects>();
                }

                switch (settings.kind)
                {
                    case BotProjectileKind.PoisonBottle:
                        status.ApplyPoison(settings.poisonDuration, settings.poisonTickInterval, settings.poisonTickDamage, settings.poisonSpreadRadius, settings.poisonSpreadCount);
                        break;

                    case BotProjectileKind.FlameBottle:
                        status.ApplyBurn(settings.burnDuration, settings.burnTickInterval, settings.burnTickDamage, settings.burnDeathExplosionRadius, settings.burnDeathExplosionDamage);
                        break;
                }
            }

            if (shadowRenderer != null)
            {
                DestroyShadow();
            }

            impactCallback?.Invoke(targetPosition);
            Destroy(gameObject, destroyDelayAfterImpact);
        }

        private void EnsureShadow()
        {
            if (!settings.useShadow)
            {
                DestroyShadow();
                return;
            }

            if (shadowRenderer != null)
            {
                shadowRenderer.enabled = true;
                return;
            }

            if (shadowSprite == null)
            {
                return;
            }

            GameObject shadow = new GameObject("Fake Shadow");
            shadow.transform.SetParent(null);
            shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = shadowSprite;

            if (bodyRenderer != null)
            {
                shadowRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
                shadowRenderer.sortingOrder = bodyRenderer.sortingOrder - 1;
            }
        }

        private void OnDestroy()
        {
            DestroyShadow();
        }

        private void UpdateShadow(Vector2 groundPosition, float heightRatio)
        {
            if (!settings.useShadow || shadowRenderer == null)
            {
                return;
            }

            float scale = Mathf.Lerp(1f, settings.shadowScaleAtPeak, heightRatio);
            float alpha = Mathf.Lerp(settings.shadowColor.a, settings.shadowAlphaAtPeak, heightRatio);
            Color color = settings.shadowColor;
            color.a = alpha;

            shadowRenderer.color = color;
            shadowRenderer.transform.position = groundPosition;
            shadowRenderer.transform.localScale = new Vector3(settings.shadowGroundScale.x * scale, settings.shadowGroundScale.y * scale, 1f);
        }

        private void DestroyShadow()
        {
            if (shadowRenderer == null)
            {
                return;
            }

            Destroy(shadowRenderer.gameObject);
            shadowRenderer = null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = settings.kind == BotProjectileKind.PoisonBottle ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, settings.impactRadius);
        }
    }

    internal sealed class RuntimeObjectLifetime : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float lifetime = 1.5f;

        private float elapsed;

        public static void Attach(GameObject target, float seconds)
        {
            if (target == null)
            {
                return;
            }

            RuntimeObjectLifetime lifetimeComponent = target.GetComponent<RuntimeObjectLifetime>();
            if (lifetimeComponent == null)
            {
                lifetimeComponent = target.AddComponent<RuntimeObjectLifetime>();
            }

            lifetimeComponent.SetLifetime(seconds);
        }

        public void SetLifetime(float seconds)
        {
            lifetime = Mathf.Max(0.01f, seconds);
            elapsed = 0f;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
