using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace StrandedRoguelike
{
    public enum SurvivorAreaHitKind
    {
        IceSpike,
        LightningBolt
    }

    public sealed class SurvivorDelayedAreaHit : MonoBehaviour
    {
        private static Sprite circleSprite;

        private SurvivorAreaHitKind kind;
        private float delay;
        private float radius;
        private int damage;
        private float slowDuration;
        private float slowMultiplier;
        private LayerMask enemyLayers;
        private GameObject impactVFXPrefab;
        private SpriteRenderer markerRenderer;
        private float impactVFXLifetime = 1.4f;
        private float visualRotationDegrees;

        public static void Spawn(
            SurvivorAreaHitKind kind,
            Vector2 position,
            float delay,
            float radius,
            int damage,
            LayerMask enemyLayers,
            GameObject impactVFXPrefab = null,
            float slowDuration = 0f,
            float slowMultiplier = 0.45f,
            float impactVFXLifetime = 1.4f,
            float visualRotationDegrees = 0f)
        {
            GameObject hitObject = new GameObject($"Survivor {kind}");
            hitObject.transform.position = position;
            SurvivorDelayedAreaHit areaHit = hitObject.AddComponent<SurvivorDelayedAreaHit>();
            areaHit.Initialize(kind, delay, radius, damage, enemyLayers, impactVFXPrefab, slowDuration, slowMultiplier, impactVFXLifetime, visualRotationDegrees);
        }

        private void Initialize(SurvivorAreaHitKind newKind, float newDelay, float newRadius, int newDamage, LayerMask newEnemyLayers, GameObject newImpactVFXPrefab, float newSlowDuration, float newSlowMultiplier, float newImpactVFXLifetime, float newVisualRotationDegrees)
        {
            kind = newKind;
            delay = Mathf.Max(0f, newDelay);
            radius = Mathf.Max(0.05f, newRadius);
            damage = Mathf.Max(0, newDamage);
            enemyLayers = newEnemyLayers;
            impactVFXPrefab = newImpactVFXPrefab;
            slowDuration = Mathf.Max(0f, newSlowDuration);
            slowMultiplier = Mathf.Clamp(newSlowMultiplier, 0.05f, 1f);
            impactVFXLifetime = Mathf.Max(0.05f, newImpactVFXLifetime);
            visualRotationDegrees = newVisualRotationDegrees;

            markerRenderer = gameObject.AddComponent<SpriteRenderer>();
            markerRenderer.sprite = GetCircleSprite();
            markerRenderer.color = kind == SurvivorAreaHitKind.IceSpike
                ? new Color(0.35f, 0.8f, 1f, 0.28f)
                : new Color(0.75f, 0.95f, 1f, 0.32f);
            markerRenderer.sortingOrder = 6;
            transform.localScale = Vector3.one * radius * 2f;
        }

        private void Update()
        {
            delay -= Time.deltaTime;

            if (markerRenderer != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 18f) * 0.08f;
                markerRenderer.transform.localScale = Vector3.one * pulse;
            }

            if (delay <= 0f)
            {
                Impact();
            }
        }

        private void Impact()
        {
            if (impactVFXPrefab != null)
            {
                GameObject impactVFX = Instantiate(
                    impactVFXPrefab,
                    transform.position,
                    Quaternion.Euler(0f, 0f, visualRotationDegrees));
                RuntimeObjectLifetime.Attach(impactVFX, impactVFXLifetime);
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, enemyLayers);
            for (int i = 0; i < hits.Length; i++)
            {
                EnemyHealth enemy = hits[i].GetComponent<EnemyHealth>();
                if (enemy == null || enemy.isDead)
                {
                    continue;
                }

                enemy.TakeDamage(damage, transform.position);

                if (kind == SurvivorAreaHitKind.IceSpike && slowDuration > 0f)
                {
                    EnemyStatusEffects status = enemy.GetComponent<EnemyStatusEffects>();
                    if (status == null)
                    {
                        status = enemy.gameObject.AddComponent<EnemyStatusEffects>();
                    }

                    status.ApplySlow(slowDuration, slowMultiplier);
                }
            }

            Destroy(gameObject);
        }

        private static Sprite GetCircleSprite()
        {
            if (circleSprite != null)
            {
                return circleSprite;
            }

            Texture2D texture = new Texture2D(48, 48);
            texture.filterMode = FilterMode.Point;
            Vector2 center = new Vector2(23.5f, 23.5f);

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    texture.SetPixel(x, y, distance <= 22f ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            circleSprite = Sprite.Create(texture, new Rect(0f, 0f, 48f, 48f), new Vector2(0.5f, 0.5f), 48f);
            return circleSprite;
        }
    }

    public sealed class SurvivorBindingField : MonoBehaviour
    {
        private readonly HashSet<EnemyHealth> boundEnemies = new HashSet<EnemyHealth>();

        private Sprite[] animationFrames;
        private float radius;
        private float remainingDuration;
        private LayerMask enemyLayers;
        private SpriteRenderer spriteRenderer;
        private float scanTimer;
        private float frameTimer;
        private int frameIndex;
        private Vector3 baseScale;

        public static void Spawn(
            Vector2 position,
            Sprite[] animationFrames,
            float radius,
            float duration,
            LayerMask enemyLayers)
        {
            GameObject fieldObject = new GameObject("Missile Binding Rune");
            fieldObject.transform.position = position;

            SurvivorBindingField field = fieldObject.AddComponent<SurvivorBindingField>();
            field.Initialize(animationFrames, radius, duration, enemyLayers);
        }

        private void Initialize(
            Sprite[] newAnimationFrames,
            float newRadius,
            float newDuration,
            LayerMask newEnemyLayers)
        {
            animationFrames = newAnimationFrames;
            radius = Mathf.Max(0.1f, newRadius);
            remainingDuration = Mathf.Max(0.1f, newDuration);
            enemyLayers = newEnemyLayers;

            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetFirstValidFrame();
            spriteRenderer.color = new Color(0.45f, 0.9f, 1f, 0.82f);
            spriteRenderer.sortingOrder = 7;

            float spriteWidth = spriteRenderer.sprite != null
                ? Mathf.Max(0.01f, spriteRenderer.sprite.bounds.size.x)
                : 1f;
            float scale = radius * 2f / spriteWidth;
            baseScale = Vector3.one * scale;
            transform.localScale = baseScale;

            ScanForEnemies();
        }

        private void Update()
        {
            remainingDuration -= Time.deltaTime;
            if (remainingDuration <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            frameTimer -= Time.deltaTime;
            if (frameTimer <= 0f)
            {
                frameTimer = 1f / 12f;
                AdvanceFrame();
            }

            float pulse = 1f + Mathf.Sin(Time.time * 7f) * 0.06f;
            transform.localScale = baseScale * pulse;

            scanTimer -= Time.deltaTime;
            if (scanTimer <= 0f)
            {
                scanTimer = 0.15f;
                ScanForEnemies();
            }
        }

        private void ScanForEnemies()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, enemyLayers);
            for (int i = 0; i < hits.Length; i++)
            {
                EnemyHealth enemy = hits[i].GetComponentInParent<EnemyHealth>();
                if (enemy == null || enemy.isDead || !boundEnemies.Add(enemy))
                {
                    continue;
                }

                EnemyStatusEffects status = enemy.GetComponent<EnemyStatusEffects>();
                if (status == null)
                {
                    status = enemy.gameObject.AddComponent<EnemyStatusEffects>();
                }

                status.ApplySlow(remainingDuration + 0.1f, 0.05f);
            }
        }

        private void AdvanceFrame()
        {
            if (spriteRenderer == null || animationFrames == null || animationFrames.Length == 0)
            {
                return;
            }

            for (int i = 0; i < animationFrames.Length; i++)
            {
                frameIndex = (frameIndex + 1) % animationFrames.Length;
                if (animationFrames[frameIndex] != null)
                {
                    spriteRenderer.sprite = animationFrames[frameIndex];
                    return;
                }
            }
        }

        private Sprite GetFirstValidFrame()
        {
            if (animationFrames == null)
            {
                return null;
            }

            for (int i = 0; i < animationFrames.Length; i++)
            {
                if (animationFrames[i] != null)
                {
                    frameIndex = i;
                    return animationFrames[i];
                }
            }

            return null;
        }
    }

    public sealed class SurvivorFlameGround : MonoBehaviour
    {
        private static Sprite fallbackSprite;

        private float radius;
        private float remainingDuration;
        private float tickInterval;
        private float tickTimer;
        private int tickDamage;
        private LayerMask enemyLayers;
        private SpriteRenderer spriteRenderer;
        private AnimationClip animationClip;
        private PlayableGraph playableGraph;
        private AnimationClipPlayable clipPlayable;
        private bool hasPlayableGraph;
        private bool visualScaleApplied;

        public static void Spawn(
            Vector2 position,
            AnimationClip animationClip,
            float radius,
            float duration,
            float tickInterval,
            int tickDamage,
            LayerMask enemyLayers)
        {
            GameObject groundObject = new GameObject("Flame Ground");
            groundObject.transform.position = position;

            SurvivorFlameGround flameGround = groundObject.AddComponent<SurvivorFlameGround>();
            flameGround.Initialize(animationClip, radius, duration, tickInterval, tickDamage, enemyLayers);
        }

        private void Initialize(
            AnimationClip newAnimationClip,
            float newRadius,
            float newDuration,
            float newTickInterval,
            int newTickDamage,
            LayerMask newEnemyLayers)
        {
            animationClip = newAnimationClip;
            radius = Mathf.Max(0.1f, newRadius);
            remainingDuration = Mathf.Max(0.1f, newDuration);
            tickInterval = Mathf.Max(0.1f, newTickInterval);
            tickDamage = Mathf.Max(0, newTickDamage);
            enemyLayers = newEnemyLayers;

            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = 9;

            if (animationClip != null)
            {
                Animator animator = gameObject.AddComponent<Animator>();
                playableGraph = PlayableGraph.Create("Flame Ground Animation");
                AnimationPlayableOutput output = AnimationPlayableOutput.Create(playableGraph, "Flame Ground", animator);
                clipPlayable = AnimationClipPlayable.Create(playableGraph, animationClip);
                output.SetSourcePlayable(clipPlayable);
                playableGraph.Play();
                playableGraph.Evaluate(0f);
                hasPlayableGraph = true;
            }
            else
            {
                spriteRenderer.sprite = GetFallbackSprite();
                spriteRenderer.color = new Color(1f, 0.35f, 0.05f, 0.8f);
            }

            ApplyVisualScale();
            DamageEnemies();
            tickTimer = tickInterval;
        }

        private void Update()
        {
            remainingDuration -= Time.deltaTime;
            if (remainingDuration <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (!visualScaleApplied)
            {
                ApplyVisualScale();
            }

            if (hasPlayableGraph
                && animationClip != null
                && animationClip.length > 0f
                && clipPlayable.GetTime() >= animationClip.length)
            {
                clipPlayable.SetTime(0d);
                clipPlayable.SetDone(false);
            }

            tickTimer -= Time.deltaTime;
            if (tickTimer <= 0f)
            {
                tickTimer = tickInterval;
                DamageEnemies();
            }
        }

        private void DamageEnemies()
        {
            if (tickDamage <= 0)
            {
                return;
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, enemyLayers);
            HashSet<EnemyHealth> damagedEnemies = new HashSet<EnemyHealth>();

            for (int i = 0; i < hits.Length; i++)
            {
                EnemyHealth enemy = hits[i].GetComponentInParent<EnemyHealth>();
                if (enemy == null || enemy.isDead || !damagedEnemies.Add(enemy))
                {
                    continue;
                }

                enemy.TakeDamage(tickDamage, transform.position, SurvivorDamageKind.StatusTick);
            }
        }

        private void ApplyVisualScale()
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            float spriteWidth = Mathf.Max(0.01f, spriteRenderer.sprite.bounds.size.x);
            float scale = radius * 2f / spriteWidth;
            transform.localScale = Vector3.one * scale;
            visualScaleApplied = true;
        }

        private void OnDestroy()
        {
            if (hasPlayableGraph && playableGraph.IsValid())
            {
                playableGraph.Destroy();
            }
        }

        private static Sprite GetFallbackSprite()
        {
            if (fallbackSprite != null)
            {
                return fallbackSprite;
            }

            const int size = 48;
            Texture2D texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;
            Vector2 center = Vector2.one * (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(1f - distance / (size * 0.48f));
                    texture.SetPixel(x, y, new Color(1f, 0.25f, 0.02f, alpha));
                }
            }

            texture.Apply();
            fallbackSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);
            return fallbackSprite;
        }
    }
}
