using System.Collections.Generic;
using UnityEngine;

namespace StrandedRoguelike
{
    [RequireComponent(typeof(PlayerAttack))]
    public sealed class SlashElementEffects : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxActiveElements = 2;
        [SerializeField, Min(0f)] private float lightningChainRadius = 2.75f;
        [SerializeField, Min(0)] private int lightningChainDamage = 1;
        [SerializeField, Min(0)] private int lightningMaxChains = 3;
        [SerializeField, Min(0f)] private float fireDuration = 2.5f;
        [SerializeField, Min(0f)] private float poisonDuration = 6f;
        [SerializeField, Min(0f)] private float freezeDuration = 0.7f;
        [SerializeField, Min(0)] private int freezeShatterDamage = 1;
        [SerializeField, Min(0)] private int technoBonusDamage = 1;

        private readonly List<SlashElementKind> activeElements = new List<SlashElementKind>();
        private PlayerAttack playerAttack;
        private Sprite particleSprite;

        private void Awake()
        {
            playerAttack = GetComponent<PlayerAttack>();
            particleSprite = CreateParticleSprite();
        }

        private void OnEnable()
        {
            if (playerAttack != null)
            {
                playerAttack.EnemyHit += OnEnemyHit;
                playerAttack.AttackVisualStarted += OnAttackVisualStarted;
            }
        }

        private void OnDisable()
        {
            if (playerAttack != null)
            {
                playerAttack.EnemyHit -= OnEnemyHit;
                playerAttack.AttackVisualStarted -= OnAttackVisualStarted;
            }
        }

        public bool TryAddElement(SlashElementKind element)
        {
            if (activeElements.Contains(element))
            {
                return false;
            }

            if (activeElements.Count >= maxActiveElements)
            {
                activeElements.RemoveAt(0);
            }

            activeElements.Add(element);
            return true;
        }

        private void OnAttackVisualStarted(Vector2 direction, GameObject slashVFXObject)
        {
            if (slashVFXObject == null || activeElements.Count <= 0)
            {
                return;
            }

            Color color = GetCombinedColor();
            SpriteRenderer[] renderers = slashVFXObject.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].color = color;
            }

            for (int i = 0; i < activeElements.Count; i++)
            {
                SpawnElementParticles(slashVFXObject.transform.position, direction, activeElements[i]);
            }
        }

        private void OnEnemyHit(EnemyHealth enemy, Vector2 attackDirection)
        {
            if (enemy == null || enemy.isDead)
            {
                return;
            }

            EnemyStatusEffects status = enemy.GetComponent<EnemyStatusEffects>();
            if (status == null)
            {
                status = enemy.gameObject.AddComponent<EnemyStatusEffects>();
            }

            for (int i = 0; i < activeElements.Count; i++)
            {
                switch (activeElements[i])
                {
                    case SlashElementKind.Lightning:
                        ChainLightning(enemy);
                        break;
                    case SlashElementKind.Fire:
                        status.ApplyBurn(fireDuration, 0.5f, 1, 1.15f, 1);
                        break;
                    case SlashElementKind.Ice:
                        status.ApplyFreeze(freezeDuration, freezeShatterDamage);
                        break;
                    case SlashElementKind.Poison:
                        status.ApplyPoison(poisonDuration, 1f, 1, 2.1f, 2);
                        break;
                    case SlashElementKind.TechnoBlade:
                        enemy.TakeDamage(technoBonusDamage, transform.position);
                        break;
                }
            }
        }

        private void ChainLightning(EnemyHealth firstTarget)
        {
            List<EnemyHealth> chained = new List<EnemyHealth> { firstTarget };
            Vector2 current = firstTarget.transform.position;

            for (int i = 0; i < lightningMaxChains; i++)
            {
                EnemyHealth next = FindNearestEnemy(current, chained);
                if (next == null)
                {
                    break;
                }

                DrawLine(current, next.transform.position, new Color(0.35f, 0.9f, 1f, 0.95f), 0.08f);
                next.TakeDamage(lightningChainDamage, current);
                chained.Add(next);
                current = next.transform.position;
            }
        }

        private EnemyHealth FindNearestEnemy(Vector2 origin, List<EnemyHealth> excluded)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, lightningChainRadius);
            EnemyHealth nearest = null;
            float nearestSqr = lightningChainRadius * lightningChainRadius;

            for (int i = 0; i < hits.Length; i++)
            {
                if (!hits[i].TryGetComponent(out EnemyHealth enemy) || enemy.isDead || excluded.Contains(enemy))
                {
                    continue;
                }

                float sqr = ((Vector2)enemy.transform.position - origin).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        private void SpawnElementParticles(Vector2 origin, Vector2 direction, SlashElementKind element)
        {
            Color color = GetElementColor(element);
            int count = element == SlashElementKind.Fire ? 8 : 6;

            for (int i = 0; i < count; i++)
            {
                GameObject particle = new GameObject($"{element} Slash Particle");
                particle.transform.position = origin + Random.insideUnitCircle * 0.25f;
                particle.transform.localScale = Vector3.one * Random.Range(0.04f, 0.11f);

                SpriteRenderer renderer = particle.AddComponent<SpriteRenderer>();
                renderer.sprite = particleSprite;
                renderer.color = color;
                renderer.sortingOrder = 40;

                ElementParticleMover mover = particle.AddComponent<ElementParticleMover>();
                Vector2 drift = direction.normalized * Random.Range(0.3f, 0.9f) + Random.insideUnitCircle * 0.7f;
                mover.Initialize(drift, Random.Range(0.16f, 0.32f), color);
            }
        }

        private Color GetCombinedColor()
        {
            if (activeElements.Count == 1)
            {
                return GetElementColor(activeElements[0]);
            }

            Color color = Color.white;
            for (int i = 0; i < activeElements.Count; i++)
            {
                color = Color.Lerp(color, GetElementColor(activeElements[i]), 0.55f);
            }

            color.a = 1f;
            return color;
        }

        private static Color GetElementColor(SlashElementKind element)
        {
            switch (element)
            {
                case SlashElementKind.Lightning:
                    return new Color(0.35f, 0.9f, 1f, 1f);
                case SlashElementKind.Fire:
                    return new Color(1f, 0.35f, 0.08f, 1f);
                case SlashElementKind.Ice:
                    return new Color(0.35f, 0.75f, 1f, 1f);
                case SlashElementKind.Poison:
                    return new Color(0.25f, 1f, 0.25f, 1f);
                case SlashElementKind.TechnoBlade:
                    return new Color(0.8f, 0.15f, 1f, 1f);
                default:
                    return Color.white;
            }
        }

        private static void DrawLine(Vector2 start, Vector2 end, Color color, float duration)
        {
            GameObject lineObject = new GameObject("Slash Element Line");
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 3;
            line.startWidth = 0.05f;
            line.endWidth = 0.02f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = color;
            line.endColor = Color.white;

            Vector2 mid = Vector2.Lerp(start, end, 0.5f) + Random.insideUnitCircle * 0.15f;
            line.SetPosition(0, start);
            line.SetPosition(1, mid);
            line.SetPosition(2, end);

            Destroy(lineObject, duration);
        }

        private static Sprite CreateParticleSprite()
        {
            const int size = 8;
            Texture2D texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;
            Vector2 center = new Vector2(size - 1, size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    texture.SetPixel(x, y, distance <= 3f ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 16f);
        }

        private sealed class ElementParticleMover : MonoBehaviour
        {
            private Vector2 velocity;
            private float lifeTime;
            private float elapsed;
            private Color color;
            private SpriteRenderer renderer;

            public void Initialize(Vector2 newVelocity, float newLifeTime, Color newColor)
            {
                velocity = newVelocity;
                lifeTime = newLifeTime;
                color = newColor;
                renderer = GetComponent<SpriteRenderer>();
            }

            private void Update()
            {
                elapsed += Time.deltaTime;
                transform.position += (Vector3)(velocity * Time.deltaTime);

                if (renderer != null)
                {
                    Color current = color;
                    current.a = Mathf.Lerp(color.a, 0f, elapsed / Mathf.Max(0.01f, lifeTime));
                    renderer.color = current;
                }

                if (elapsed >= lifeTime)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
