using System.Collections;
using TMPro;
using UnityEngine;

namespace StrandedRoguelike
{
    public sealed class SurvivorBossTimeAttack : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private SurvivorEnemySpawner enemySpawner;
        [SerializeField] private GameObject bossPrefab;
        [SerializeField] private TMP_Text timeText;

        [Header("Timing")]
        [SerializeField, Min(1f)] private float bossSpawnTime = 330f;
        [SerializeField, Min(1f)] private float bossTimeLimit = 30f;
        [SerializeField, Min(0f)] private float clearResultDelay = 1.1f;

        [Header("Boss")]
        [SerializeField, Min(1)] private int bossMaxHealth = 300;
        [SerializeField, Min(0)] private int bossContactDamage = 2;
        [SerializeField, Min(0.1f)] private float minimumSpawnRadius = 5f;
        [SerializeField, Min(0.1f)] private float maximumSpawnRadius = 7f;
        [SerializeField, Min(1)] private int spawnPositionAttempts = 16;
        [SerializeField, Min(0f)] private float enemyClearanceRadius = 3f;

        [Header("Result")]
        [SerializeField] private global::GameManager gameManager;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject gameCompletePanel;

        private EnemyHealth bossHealth;
        private GameObject bossInstance;
        private float fallbackElapsed;
        private float remainingBossTime;
        private bool bossFightStarted;
        private bool bossDying;
        private bool encounterEnding;
        private bool encounterEnded;

        public void Configure(Transform newPlayer, SurvivorEnemySpawner newEnemySpawner, GameObject newBossPrefab)
        {
            if (newPlayer != null)
            {
                player = newPlayer;
            }

            if (newEnemySpawner != null)
            {
                enemySpawner = newEnemySpawner;
            }

            if (newBossPrefab != null)
            {
                bossPrefab = newBossPrefab;
            }

            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
            HideResultPanels();
        }

        private void OnDisable()
        {
            UnsubscribeBossHealth();
        }

        private void Update()
        {
            if (encounterEnded || encounterEnding)
            {
                return;
            }

            if (!bossFightStarted)
            {
                fallbackElapsed += Time.deltaTime;
                float survivalTime = enemySpawner != null ? enemySpawner.ElapsedTime : fallbackElapsed;
                if (survivalTime >= bossSpawnTime)
                {
                    StartBossFight();
                }

                return;
            }

            if (bossDying)
            {
                return;
            }

            if (bossHealth == null)
            {
                BeginGameClear();
                return;
            }

            if (bossHealth.isDead)
            {
                bossDying = true;
                return;
            }

            remainingBossTime = Mathf.Max(0f, remainingBossTime - Time.deltaTime);
            RefreshCountdownText();

            if (remainingBossTime <= 0f)
            {
                FinishEncounter(false);
            }
        }

        [ContextMenu("Boss Test/01 Spawn Boss Now")]
        private void StartBossNow()
        {
            if (!CanRunBossTest())
            {
                return;
            }

            if (bossFightStarted)
            {
                Debug.LogWarning("[Boss Test] The boss fight has already started.", this);
                return;
            }

            StartBossFight();
        }

        [ContextMenu("Boss Test/02 Defeat Active Boss")]
        private void DefeatActiveBossNow()
        {
            if (!CanRunBossTest())
            {
                return;
            }

            if (!bossFightStarted || bossHealth == null || bossHealth.isDead)
            {
                Debug.LogWarning("[Boss Test] There is no living active boss to defeat.", this);
                return;
            }

            DefeatBossForTest();
        }

        [ContextMenu("Boss Test/03 Spawn And Defeat Boss")]
        private void SpawnAndDefeatBossNow()
        {
            if (!CanRunBossTest())
            {
                return;
            }

            if (!bossFightStarted)
            {
                StartBossFight();
            }

            if (bossHealth == null || bossHealth.isDead)
            {
                Debug.LogWarning("[Boss Test] The boss could not be prepared for the defeat test.", this);
                return;
            }

            DefeatBossForTest();
        }

        private bool CanRunBossTest()
        {
            if (Application.isPlaying)
            {
                return true;
            }

            Debug.LogWarning("[Boss Test] Boss tests can only be used in Play Mode.", this);
            return false;
        }

        private void DefeatBossForTest()
        {
            int lethalDamage = Mathf.Max(1, bossHealth.CurrentHealth);
            bossHealth.TakeDamage(lethalDamage, bossHealth.transform.position);
            Debug.Log("[Boss Test] Boss defeat sequence started.", bossHealth);
        }

        private void StartBossFight()
        {
            ResolveReferences();

            if (player == null || bossPrefab == null)
            {
                Debug.LogWarning($"{nameof(SurvivorBossTimeAttack)} could not find the player or boss prefab.", this);
                return;
            }

            bossFightStarted = true;
            bossDying = false;
            enemySpawner?.SetSpawningEnabled(false);
            enemySpawner?.SetPlayTimeDisplayEnabled(false);

            bossInstance = Instantiate(bossPrefab, FindBossSpawnPosition(), Quaternion.identity);
            bossInstance.name = bossPrefab.name;

            SurvivorStationaryBoss stationaryBoss = bossInstance.GetComponent<SurvivorStationaryBoss>();
            if (stationaryBoss == null)
            {
                stationaryBoss = bossInstance.AddComponent<SurvivorStationaryBoss>();
            }

            stationaryBoss.Configure(bossContactDamage);
            bossHealth = bossInstance.GetComponent<EnemyHealth>();
            if (bossHealth != null)
            {
                bossHealth.SetMaxHealth(bossMaxHealth, true);
                bossHealth.Died -= OnBossDied;
                bossHealth.Died += OnBossDied;
                bossHealth.DeathAnimationFinished -= OnBossDeathAnimationFinished;
                bossHealth.DeathAnimationFinished += OnBossDeathAnimationFinished;

                SurvivorBossHealthBar healthBar = bossInstance.GetComponent<SurvivorBossHealthBar>();
                if (healthBar == null)
                {
                    healthBar = bossInstance.AddComponent<SurvivorBossHealthBar>();
                }

                healthBar.Configure(bossHealth);
            }
            else
            {
                Debug.LogError($"{bossPrefab.name} needs an {nameof(EnemyHealth)} component.", bossInstance);
            }

            remainingBossTime = bossTimeLimit;
            RefreshCountdownText();
        }

        private Vector2 FindBossSpawnPosition()
        {
            float safeMinimumRadius = Mathf.Min(minimumSpawnRadius, maximumSpawnRadius);
            float safeMaximumRadius = Mathf.Max(minimumSpawnRadius, maximumSpawnRadius);
            EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            Vector2 bestPosition = (Vector2)player.position + Vector2.right * safeMinimumRadius;
            float bestClearance = -1f;

            for (int attempt = 0; attempt < Mathf.Max(1, spawnPositionAttempts); attempt++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float radius = Random.Range(safeMinimumRadius, safeMaximumRadius);
                Vector2 candidate = (Vector2)player.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                float nearestEnemyDistance = float.MaxValue;

                for (int i = 0; i < enemies.Length; i++)
                {
                    EnemyHealth enemy = enemies[i];
                    if (enemy == null || enemy == bossHealth || enemy.isDead || !enemy.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    float distance = Vector2.Distance(candidate, enemy.transform.position);
                    nearestEnemyDistance = Mathf.Min(nearestEnemyDistance, distance);
                }

                if (nearestEnemyDistance > bestClearance)
                {
                    bestClearance = nearestEnemyDistance;
                    bestPosition = candidate;
                }

                if (nearestEnemyDistance >= enemyClearanceRadius)
                {
                    return candidate;
                }
            }

            return bestPosition;
        }

        private void OnBossDied(EnemyHealth deadBoss)
        {
            if (deadBoss != bossHealth || bossDying)
            {
                return;
            }

            bossDying = true;
        }

        private void OnBossDeathAnimationFinished(EnemyHealth deadBoss)
        {
            if (deadBoss != bossHealth || encounterEnding || encounterEnded)
            {
                return;
            }

            StartCoroutine(BeginGameClearAfterBossDestroyed());
        }

        private IEnumerator BeginGameClearAfterBossDestroyed()
        {
            yield return null;
            BeginGameClear();
        }

        private void BeginGameClear()
        {
            if (encounterEnding || encounterEnded)
            {
                return;
            }

            encounterEnding = true;
            SetTimeText("CLEAR");
            StartCoroutine(ShowClearAfterDelay());
        }

        private IEnumerator ShowClearAfterDelay()
        {
            if (clearResultDelay > 0f)
            {
                yield return new WaitForSeconds(clearResultDelay);
            }

            FinishEncounter(true);
        }

        private void FinishEncounter(bool cleared)
        {
            if (encounterEnded)
            {
                return;
            }

            encounterEnding = false;
            encounterEnded = true;
            enemySpawner?.SetSpawningEnabled(false);
            UnsubscribeBossHealth();

            if (!cleared)
            {
                SetTimeText("00:00");
            }

            ShowResultPanel(cleared);
            PauseGame();
        }

        private void RefreshCountdownText()
        {
            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(remainingBossTime));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            SetTimeText($"{minutes:00}:{seconds:00}");
        }

        private void SetTimeText(string value)
        {
            if (timeText != null)
            {
                timeText.text = value;
            }
        }

        private void ShowResultPanel(bool cleared)
        {
            ResolveResultPanels();

            if (gameCompletePanel != null)
            {
                if (cleared)
                {
                    ShowPanelWithUnscaledTweens(gameCompletePanel);
                }
                else
                {
                    gameCompletePanel.SetActive(false);
                }
            }

            if (gameOverPanel != null)
            {
                if (cleared)
                {
                    gameOverPanel.SetActive(false);
                }
                else
                {
                    ShowPanelWithUnscaledTweens(gameOverPanel);
                }
            }

            GameObject expectedPanel = cleared ? gameCompletePanel : gameOverPanel;
            if (expectedPanel == null)
            {
                string panelName = cleared ? "GameComplete" : "GameOver";
                Debug.LogWarning($"{nameof(SurvivorBossTimeAttack)} could not find the {panelName} panel.", this);
            }
        }

        private static void ShowPanelWithUnscaledTweens(GameObject panel)
        {
            ResultPanelTweenPlayer.Show(panel);
        }

        private void PauseGame()
        {
            if (gameManager == null)
            {
                ResolveGameManager();
            }

            if (gameManager != null)
            {
                gameManager.PauseGame();
                return;
            }

            Time.timeScale = 0f;
        }

        private void ResolveReferences()
        {
            if (player == null)
            {
                PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
                if (movement != null)
                {
                    player = movement.transform;
                }
            }

            if (enemySpawner == null)
            {
                enemySpawner = FindFirstObjectByType<SurvivorEnemySpawner>();
            }

            if (timeText == null)
            {
                timeText = enemySpawner != null ? enemySpawner.PlayTimeText : null;
            }

            if (timeText == null)
            {
                TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < texts.Length; i++)
                {
                    if (texts[i] != null && texts[i].gameObject.name == "Time")
                    {
                        timeText = texts[i];
                        break;
                    }
                }
            }

            ResolveGameManager();
            ResolveResultPanels();
        }

        private void ResolveGameManager()
        {
            if (gameManager != null)
            {
                return;
            }

            global::GameManager[] managers = Resources.FindObjectsOfTypeAll<global::GameManager>();
            for (int i = 0; i < managers.Length; i++)
            {
                if (managers[i] != null && managers[i].gameObject.scene.IsValid())
                {
                    gameManager = managers[i];
                    return;
                }
            }
        }

        private void ResolveResultPanels()
        {
            if (gameOverPanel == null)
            {
                gameOverPanel = FindSceneObjectByNormalizedName("gameover");
            }

            if (gameCompletePanel == null)
            {
                gameCompletePanel = FindSceneObjectByNormalizedName("gamecomplete");
            }
        }

        private void HideResultPanels()
        {
            ResolveResultPanels();
            gameOverPanel?.SetActive(false);
            gameCompletePanel?.SetActive(false);
        }

        private static GameObject FindSceneObjectByNormalizedName(string expectedName)
        {
            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject candidate = objects[i];
                if (candidate == null || !candidate.scene.IsValid())
                {
                    continue;
                }

                string normalizedName = NormalizeObjectName(candidate.name);
                if (normalizedName == expectedName || normalizedName == expectedName + "panel")
                {
                    return candidate;
                }
            }

            return null;
        }

        private static string NormalizeObjectName(string objectName)
        {
            return string.IsNullOrWhiteSpace(objectName)
                ? string.Empty
                : objectName.Replace(" ", string.Empty)
                    .Replace("_", string.Empty)
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
        }

        private void UnsubscribeBossHealth()
        {
            if (bossHealth != null)
            {
                bossHealth.Died -= OnBossDied;
                bossHealth.DeathAnimationFinished -= OnBossDeathAnimationFinished;
            }
        }
    }
}
