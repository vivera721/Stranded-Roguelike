using System.Collections.Generic;
using UnityEngine;

namespace StrandedRoguelike
{
    public sealed class SurvivorAxeBoomerangProjectile : MonoBehaviour
    {
        private readonly HashSet<EnemyHealth> hitEnemies = new HashSet<EnemyHealth>();

        private Transform owner;
        private Vector2 startPosition;
        private Vector2 targetPosition;
        private float duration;
        private float arcHeight;
        private float elapsed;
        private float hitRadius;
        private int damage;
        private LayerMask enemyLayers;
        private bool launched;

        public static void Spawn(Transform owner, Vector2 targetPosition, Sprite sprite, float duration, float arcHeight, float hitRadius, int damage, LayerMask enemyLayers)
        {
            if (owner == null)
            {
                return;
            }

            GameObject axeObject = new GameObject("Survivor Axe Boomerang");
            axeObject.transform.position = owner.position;

            SpriteRenderer renderer = axeObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = sprite != null ? Color.white : new Color(0.85f, 0.85f, 0.9f, 1f);
            renderer.sortingOrder = 32;

            SurvivorAxeBoomerangProjectile projectile = axeObject.AddComponent<SurvivorAxeBoomerangProjectile>();
            projectile.Initialize(owner, targetPosition, duration, arcHeight, hitRadius, damage, enemyLayers);
        }

        private void Initialize(Transform newOwner, Vector2 newTargetPosition, float newDuration, float newArcHeight, float newHitRadius, int newDamage, LayerMask newEnemyLayers)
        {
            owner = newOwner;
            startPosition = owner.position;
            targetPosition = newTargetPosition;
            duration = Mathf.Max(0.1f, newDuration);
            arcHeight = Mathf.Max(0f, newArcHeight);
            hitRadius = Mathf.Max(0.05f, newHitRadius);
            damage = Mathf.Max(0, newDamage);
            enemyLayers = newEnemyLayers;
            launched = true;
        }

        private void Update()
        {
            if (!launched || owner == null)
            {
                Destroy(gameObject);
                return;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector2 returnPosition = owner.position;
            Vector2 groundPosition = t < 0.5f
                ? Vector2.Lerp(startPosition, targetPosition, t / 0.5f)
                : Vector2.Lerp(targetPosition, returnPosition, (t - 0.5f) / 0.5f);

            float height = Mathf.Sin(t * Mathf.PI) * arcHeight;
            transform.position = groundPosition + Vector2.up * height;
            transform.Rotate(0f, 0f, 720f * Time.deltaTime);

            CheckHits();

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }

        private void CheckHits()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hitRadius, enemyLayers);
            for (int i = 0; i < hits.Length; i++)
            {
                EnemyHealth enemy = hits[i].GetComponent<EnemyHealth>();
                if (enemy == null || enemy.isDead || hitEnemies.Contains(enemy))
                {
                    continue;
                }

                hitEnemies.Add(enemy);
                enemy.TakeDamage(damage, transform.position);
            }
        }
    }
}
