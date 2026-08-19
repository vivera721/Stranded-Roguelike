using System.Collections.Generic;
using UnityEngine;

namespace StrandedRoguelike
{
    public static class SurvivorTargeting
    {
        public static EnemyHealth FindNearestEnemy(Vector2 origin, float radius, LayerMask enemyLayers)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, radius, enemyLayers);
            EnemyHealth best = null;
            float bestDistance = float.MaxValue;
            float radiusSquared = radius * radius;

            for (int i = 0; i < hits.Length; i++)
            {
                EnemyHealth enemy = hits[i].GetComponentInParent<EnemyHealth>();
                if (enemy == null || enemy.isDead)
                {
                    continue;
                }

                float distance = ((Vector2)enemy.transform.position - origin).sqrMagnitude;
                if (distance > radiusSquared)
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = enemy;
                }
            }

            return best;
        }

        public static EnemyHealth FindNearestEnemyExcept(Vector2 origin, float radius, LayerMask enemyLayers, List<EnemyHealth> ignored)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, radius, enemyLayers);
            EnemyHealth best = null;
            float bestDistance = float.MaxValue;
            float radiusSquared = radius * radius;

            for (int i = 0; i < hits.Length; i++)
            {
                EnemyHealth enemy = hits[i].GetComponentInParent<EnemyHealth>();
                if (enemy == null || enemy.isDead || ignored.Contains(enemy))
                {
                    continue;
                }

                float distance = ((Vector2)enemy.transform.position - origin).sqrMagnitude;
                if (distance > radiusSquared)
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = enemy;
                }
            }

            return best;
        }

        public static Vector2 RandomPointOnRing(Vector2 center, float radius)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        public static Vector2 RandomPointInCircle(Vector2 center, float radius)
        {
            return center + Random.insideUnitCircle * radius;
        }
    }
}
