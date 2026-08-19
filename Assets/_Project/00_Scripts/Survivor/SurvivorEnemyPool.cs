using System.Collections.Generic;
using UnityEngine;

namespace StrandedRoguelike
{
    public sealed class SurvivorEnemyPool : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField, Min(0)] private int initialSize = 24;
        [SerializeField, Min(1)] private int maxSize = 200;

        private readonly Queue<SurvivorEnemy> available = new Queue<SurvivorEnemy>();
        private readonly List<SurvivorEnemy> allEnemies = new List<SurvivorEnemy>();

        public int AliveCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < allEnemies.Count; i++)
                {
                    if (allEnemies[i] != null && allEnemies[i].gameObject.activeInHierarchy)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void Configure(GameObject prefab, int prewarm, int capacity)
        {
            enemyPrefab = prefab;
            initialSize = Mathf.Max(0, prewarm);
            maxSize = Mathf.Max(1, capacity);
            Prewarm();
        }

        private void Awake()
        {
            Prewarm();
        }

        private void Prewarm()
        {
            if (enemyPrefab == null)
            {
                return;
            }

            while (allEnemies.Count < initialSize && allEnemies.Count < maxSize)
            {
                SurvivorEnemy enemy = CreateEnemy();
                Return(enemy);
            }
        }

        public SurvivorEnemy Get(Vector2 position, Transform target, int maxHealth, float moveSpeed, int contactDamage, int experienceValue)
        {
            SurvivorEnemy enemy = available.Count > 0 ? available.Dequeue() : CreateEnemy();
            if (enemy == null)
            {
                return null;
            }

            enemy.transform.position = position;
            enemy.gameObject.SetActive(true);
            enemy.Configure(this, target, maxHealth, moveSpeed, contactDamage, experienceValue);
            return enemy;
        }

        public void Return(SurvivorEnemy enemy)
        {
            if (enemy == null)
            {
                return;
            }

            enemy.gameObject.SetActive(false);

            if (!available.Contains(enemy))
            {
                available.Enqueue(enemy);
            }
        }

        private SurvivorEnemy CreateEnemy()
        {
            if (enemyPrefab == null || allEnemies.Count >= maxSize)
            {
                return null;
            }

            GameObject instance = Instantiate(enemyPrefab, transform);
            SurvivorEnemy enemy = instance.GetComponent<SurvivorEnemy>();
            if (enemy == null)
            {
                enemy = instance.AddComponent<SurvivorEnemy>();
            }

            enemy.SetPool(this);
            allEnemies.Add(enemy);
            return enemy;
        }
    }
}
