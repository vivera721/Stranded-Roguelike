using System;
using System.Collections.Generic;
using UnityEngine;

namespace StrandedRoguelike
{
    public sealed class SurvivorStraightProjectile : MonoBehaviour
    {
        private readonly HashSet<EnemyHealth> hitEnemies = new HashSet<EnemyHealth>();

        private Vector2 direction;
        private float speed;
        private float lifetime;
        private float hitRadius;
        private int damage;
        private int pierce;
        private LayerMask enemyLayers;
        private GameObject impactVFXPrefab;
        private float impactVFXLifetime = 1.2f;
        private bool applyBurn;
        private float burnDuration;
        private int burnDamage;
        private Action<Vector2> hitCallback;

        public static SurvivorStraightProjectile Spawn(
            Vector2 position,
            Vector2 direction,
            Sprite sprite,
            float speed,
            float lifetime,
            float hitRadius,
            int damage,
            int pierce,
            LayerMask enemyLayers,
            GameObject impactVFXPrefab = null,
            bool applyBurn = false,
            float burnDuration = 2f,
            int burnDamage = 1,
            Action<Vector2> hitCallback = null)
        {
            GameObject projectileObject = new GameObject("Survivor Straight Projectile");
            projectileObject.transform.position = position;

            SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = sprite != null ? Color.white : new Color(1f, 0.45f, 0.1f, 1f);
            renderer.sortingOrder = 30;

            SurvivorStraightProjectile projectile = projectileObject.AddComponent<SurvivorStraightProjectile>();
            projectile.Initialize(direction, speed, lifetime, hitRadius, damage, pierce, enemyLayers, impactVFXPrefab, applyBurn, burnDuration, burnDamage, hitCallback);
            return projectile;
        }

        private void Initialize(Vector2 newDirection, float newSpeed, float newLifetime, float newHitRadius, int newDamage, int newPierce, LayerMask newEnemyLayers, GameObject newImpactVFXPrefab, bool newApplyBurn, float newBurnDuration, int newBurnDamage, Action<Vector2> newHitCallback)
        {
            direction = newDirection.sqrMagnitude > 0.001f ? newDirection.normalized : Vector2.right;
            speed = Mathf.Max(0f, newSpeed);
            lifetime = Mathf.Max(0.05f, newLifetime);
            hitRadius = Mathf.Max(0.05f, newHitRadius);
            damage = Mathf.Max(0, newDamage);
            pierce = Mathf.Max(1, newPierce);
            enemyLayers = newEnemyLayers;
            impactVFXPrefab = newImpactVFXPrefab;
            applyBurn = newApplyBurn;
            burnDuration = newBurnDuration;
            burnDamage = newBurnDamage;
            hitCallback = newHitCallback;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Update()
        {
            lifetime -= Time.deltaTime;
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
            CheckHits();

            if (lifetime <= 0f)
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
                hitCallback?.Invoke(transform.position);

                if (applyBurn)
                {
                    EnemyStatusEffects status = enemy.GetComponent<EnemyStatusEffects>();
                    if (status == null)
                    {
                        status = enemy.gameObject.AddComponent<EnemyStatusEffects>();
                    }

                    status.ApplyBurn(burnDuration, 0.5f, burnDamage, 1.1f, burnDamage);
                }

                if (impactVFXPrefab != null)
                {
                    GameObject impactVFX = Instantiate(impactVFXPrefab, transform.position, Quaternion.identity);
                    RuntimeObjectLifetime.Attach(impactVFX, impactVFXLifetime);
                }

                if (hitEnemies.Count >= pierce)
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }
}
