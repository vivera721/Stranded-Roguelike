using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace StrandedRoguelike
{
    public sealed class SurvivorEnemySpawner : MonoBehaviour
    {
        [Serializable]
        public sealed class SpawnEntry
        {
            public GameObject prefab;
            [Min(1)] public int maxHealth = 3;
            [Min(0f)] public float moveSpeed = 2.2f;
            [Min(0)] public int contactDamage = 1;
            [Min(0)] public int experienceValue = 1;
            [Min(1)] public int weight = 1;
            [Min(0)] public int prewarmCount = 24;
            [Min(1)] public int maxPoolSize = 200;

            [NonSerialized] public SurvivorEnemyPool pool;
        }

        [SerializeField] private Transform player;
        [SerializeField] private SpawnEntry[] enemies;

        [Header("Time Based Spawn")]
        [Tooltip("Controls enemy type rotation and spawn interval scaling. This is separate from the alive enemy limit.")]
        [InspectorName("Enemy Type Stage Seconds")]
        [SerializeField, Min(1f)] private float secondsPerSpawnStage = 30f;
        [SerializeField, Min(0.1f)] private float minSpawnInterval = 0.25f;
        [SerializeField, Min(0f)] private float spawnIntervalDecreasePerStage = 0.03f;
        [SerializeField, Min(0)] private int additionalEnemiesPerStage = 1;
        [SerializeField, Min(1)] private int spawnCountIncreaseEveryStages = 2;

        [Header("Alive Enemy Growth")]
        [SerializeField, Min(1)] private int startingAliveEnemyLimit = 10;
        [SerializeField, Min(0.1f)] private float aliveEnemyIncreaseInterval = 30f;
        [SerializeField, Min(1)] private int aliveEnemyIncreaseAmount = 10;
        [SerializeField, Min(1)] private int maximumAliveEnemyLimit = 50;

        [Header("Play Time UI")]
        [SerializeField] private TMP_Text playTimeText;
        [SerializeField] private string playTimeObjectName = "Time";
        [SerializeField] private bool autoFindPlayTimeText = true;

        [Header("Base Spawn")]
        [SerializeField, Min(0.1f)] private float spawnInterval = 0.65f;
        [SerializeField, Min(1)] private int enemiesPerSpawn = 2;
        [SerializeField, Min(1f)] private float spawnRadius = 12f;

        [Header("Stat Growth")]
        [SerializeField, Min(0f)] private float healthGrowthPerMinute = 1.5f;
        [SerializeField, Min(0f)] private float speedGrowthPerMinute = 0.08f;

        private readonly List<SurvivorEnemy> alive = new List<SurvivorEnemy>();
        private float timer;
        private float elapsed;
        private bool spawningEnabled = true;
        private bool playTimeDisplayEnabled = true;

        public float ElapsedTime => elapsed;
        public TMP_Text PlayTimeText => playTimeText;
        public int CurrentAliveEnemyLimit => GetCurrentAliveEnemyLimit();

        private void Awake()
        {
            if (player == null)
            {
                PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
                if (movement != null)
                {
                    player = movement.transform;
                }
            }

            CreatePools();
            FindPlayTimeTextIfNeeded();
            RefreshPlayTimeUI();
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (playTimeDisplayEnabled)
            {
                RefreshPlayTimeUI();
            }

            CleanupAliveList();

            if (!spawningEnabled || player == null || enemies == null || enemies.Length == 0)
            {
                return;
            }

            timer -= Time.deltaTime;
            int currentAliveEnemyLimit = GetCurrentAliveEnemyLimit();

            if (timer > 0f || alive.Count >= currentAliveEnemyLimit)
            {
                return;
            }

            int stage = GetCurrentSpawnStage();
            timer = GetCurrentSpawnInterval(stage);
            int scaledCount = GetCurrentEnemiesPerSpawn(stage);

            for (int i = 0; i < scaledCount && alive.Count < currentAliveEnemyLimit; i++)
            {
                SpawnOne(stage);
            }
        }

        private void CreatePools()
        {
            if (enemies == null)
            {
                return;
            }

            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == null || enemies[i].prefab == null)
                {
                    continue;
                }

                GameObject poolObject = new GameObject($"{enemies[i].prefab.name} Pool");
                poolObject.transform.SetParent(transform);
                SurvivorEnemyPool pool = poolObject.AddComponent<SurvivorEnemyPool>();
                pool.Configure(enemies[i].prefab, enemies[i].prewarmCount, enemies[i].maxPoolSize);
                enemies[i].pool = pool;
            }
        }

        public void SetSpawningEnabled(bool enabled)
        {
            spawningEnabled = enabled;
        }

        public void SetPlayTimeDisplayEnabled(bool enabled)
        {
            playTimeDisplayEnabled = enabled;

            if (enabled)
            {
                RefreshPlayTimeUI();
            }
        }

        private void SpawnOne(int stage)
        {
            SpawnEntry entry = PickEntry(stage);
            if (entry == null || entry.pool == null)
            {
                return;
            }

            Vector2 position = SurvivorTargeting.RandomPointOnRing(player.position, spawnRadius);
            int health = Mathf.Max(1, entry.maxHealth + Mathf.FloorToInt((elapsed / 60f) * healthGrowthPerMinute));
            float speed = entry.moveSpeed + (elapsed / 60f) * speedGrowthPerMinute;

            SurvivorEnemy enemy = entry.pool.Get(position, player, health, speed, entry.contactDamage, entry.experienceValue);
            if (enemy != null)
            {
                alive.Add(enemy);
            }
        }

        private SpawnEntry PickEntry(int stage)
        {
            GetActiveEnemyIndexRange(stage, out int startIndex, out int endIndex);

            int totalWeight = 0;
            for (int i = startIndex; i <= endIndex; i++)
            {
                if (enemies[i] != null && enemies[i].prefab != null && enemies[i].pool != null)
                {
                    totalWeight += Mathf.Max(1, enemies[i].weight);
                }
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            int roll = UnityEngine.Random.Range(0, totalWeight);
            for (int i = startIndex; i <= endIndex; i++)
            {
                if (enemies[i] == null || enemies[i].prefab == null || enemies[i].pool == null)
                {
                    continue;
                }

                roll -= Mathf.Max(1, enemies[i].weight);
                if (roll < 0)
                {
                    return enemies[i];
                }
            }

            return enemies[Mathf.Clamp(startIndex, 0, enemies.Length - 1)];
        }

        private int GetCurrentSpawnStage()
        {
            if (secondsPerSpawnStage <= 0f)
            {
                return 0;
            }

            return Mathf.FloorToInt(elapsed / secondsPerSpawnStage);
        }

        private float GetCurrentSpawnInterval(int stage)
        {
            return Mathf.Max(minSpawnInterval, spawnInterval - stage * spawnIntervalDecreasePerStage);
        }

        private int GetCurrentEnemiesPerSpawn(int stage)
        {
            int safeEveryStages = Mathf.Max(1, spawnCountIncreaseEveryStages);
            int bonusSteps = Mathf.FloorToInt(stage / safeEveryStages);
            return Mathf.Max(1, enemiesPerSpawn + bonusSteps * additionalEnemiesPerStage);
        }

        private int GetCurrentAliveEnemyLimit()
        {
            int startLimit = Mathf.Max(1, startingAliveEnemyLimit);
            int maxLimit = Mathf.Max(startLimit, maximumAliveEnemyLimit);
            float increaseInterval = Mathf.Max(0.1f, aliveEnemyIncreaseInterval);
            int increaseSteps = Mathf.FloorToInt(elapsed / increaseInterval);
            int currentLimit = startLimit + increaseSteps * Mathf.Max(1, aliveEnemyIncreaseAmount);

            return Mathf.Min(currentLimit, maxLimit);
        }

        private void GetActiveEnemyIndexRange(int stage, out int startIndex, out int endIndex)
        {
            if (enemies == null || enemies.Length == 0)
            {
                startIndex = 0;
                endIndex = 0;
                return;
            }

            if (stage <= 0)
            {
                startIndex = 0;
                endIndex = 0;
                return;
            }

            startIndex = Mathf.Clamp(stage - 1, 0, enemies.Length - 1);
            endIndex = Mathf.Clamp(stage, startIndex, enemies.Length - 1);
        }

        private void FindPlayTimeTextIfNeeded()
        {
            if (!autoFindPlayTimeText || playTimeText != null)
            {
                return;
            }

            TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].gameObject.name == playTimeObjectName)
                {
                    playTimeText = texts[i];
                    return;
                }
            }
        }

        private void RefreshPlayTimeUI()
        {
            if (playTimeText == null)
            {
                return;
            }

            int minutes = Mathf.FloorToInt(elapsed / 60f);
            int seconds = Mathf.FloorToInt(elapsed % 60f);
            playTimeText.text = $"{minutes:00}:{seconds:00}";
        }

        private void CleanupAliveList()
        {
            for (int i = alive.Count - 1; i >= 0; i--)
            {
                if (alive[i] == null || !alive[i].gameObject.activeInHierarchy)
                {
                    alive.RemoveAt(i);
                }
            }
        }
    }
}
