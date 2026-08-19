using UnityEngine;

namespace StrandedRoguelike
{
    public sealed class SurvivorModeBootstrap : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private bool disableManualSlashAttack = true;
        [SerializeField] private bool disableDodge = true;
        [SerializeField] private bool faceMovementDirection = true;
        [SerializeField] private bool ensureWeaponController = true;
        [SerializeField] private bool ensureExperience = true;
        [Header("Start Weapon Choice")]
        [SerializeField] private bool showStartWeaponChoice = true;
        [SerializeField] private GameObject startWeaponPanelRoot;
        [SerializeField] private SurvivorStartWeaponChoicePanel startWeaponChoicePanel;
        [Header("Level Up")]
        [SerializeField] private bool enableLevelUpUpgrades = true;
        [SerializeField] private SurvivorLevelUpUpgradePanel levelUpUpgradePanel;
        [SerializeField] private global::UpgradeUI upgradeUI;
        [Header("Infinite Grid Background")]
        [SerializeField] private bool autoSetupInfiniteGridBackground = true;
        [SerializeField] private SurvivorInfiniteParallaxBackground infiniteGridBackground;
        [Header("Boss Time Attack")]
        [SerializeField] private bool autoSetupBossTimeAttack = true;
        [SerializeField] private GameObject bossPrefab;
        [SerializeField] private SurvivorBossTimeAttack bossTimeAttack;

        private SurvivorWeaponController weaponController;
        private SurvivorExperience experience;
        private bool useCustomUpgradeUI;

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

            if (player == null)
            {
                Debug.LogWarning($"{nameof(SurvivorModeBootstrap)} could not find player.");
                return;
            }

            if (disableManualSlashAttack)
            {
                PlayerAttack attack = player.GetComponent<PlayerAttack>();
                if (attack != null)
                {
                    attack.enabled = false;
                }
            }

            PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                if (disableDodge)
                {
                    playerMovement.SetDodgeEnabled(false);
                }

                if (faceMovementDirection)
                {
                    playerMovement.SetFaceMouse(false);
                }
            }

            weaponController = player.GetComponent<SurvivorWeaponController>();
            if (ensureWeaponController && weaponController == null)
            {
                weaponController = player.gameObject.AddComponent<SurvivorWeaponController>();
            }

            experience = player.GetComponent<SurvivorExperience>();
            if (ensureExperience && experience == null)
            {
                experience = player.gameObject.AddComponent<SurvivorExperience>();
            }

            SetupCustomUpgradeUI();
            SetupStartWeaponChoicePanel();
            SetupLevelUpUpgradePanel();
            SetupInfiniteGridBackground();
            SetupBossTimeAttack();
        }

        private void Start()
        {
            if (!useCustomUpgradeUI && showStartWeaponChoice && startWeaponChoicePanel != null)
            {
                startWeaponChoicePanel.Show();
            }
        }

        private void SetupCustomUpgradeUI()
        {
            if (upgradeUI == null)
            {
                global::UpgradeUI[] upgradeUIs = FindObjectsByType<global::UpgradeUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < upgradeUIs.Length; i++)
                {
                    if (upgradeUIs[i] != null && upgradeUIs[i].HasPanelReferences())
                    {
                        upgradeUI = upgradeUIs[i];
                        break;
                    }
                }

                if (upgradeUI == null && upgradeUIs.Length > 0)
                {
                    upgradeUI = upgradeUIs[0];
                }
            }

            useCustomUpgradeUI = upgradeUI != null;

            if (upgradeUI != null)
            {
                upgradeUI.Configure(experience, weaponController);
            }
        }

        private void SetupStartWeaponChoicePanel()
        {
            if (useCustomUpgradeUI)
            {
                return;
            }

            if (!showStartWeaponChoice)
            {
                return;
            }

            if (weaponController == null)
            {
                weaponController = player != null ? player.GetComponent<SurvivorWeaponController>() : null;
            }

            if (weaponController == null)
            {
                Debug.LogWarning($"{nameof(SurvivorModeBootstrap)} could not find {nameof(SurvivorWeaponController)}.");
                return;
            }

            if (startWeaponPanelRoot == null)
            {
                startWeaponPanelRoot = FindSceneGameObjectIncludingInactive("Upgrade_Panel");
            }

            if (startWeaponPanelRoot == null)
            {
                Debug.LogWarning($"{nameof(SurvivorModeBootstrap)} could not find Upgrade_Panel for start weapon choice.");
                return;
            }

            startWeaponPanelRoot.SetActive(true);

            UpgradePanelController oldUpgradePanel = startWeaponPanelRoot.GetComponent<UpgradePanelController>();
            if (oldUpgradePanel != null)
            {
                oldUpgradePanel.enabled = false;
            }

            if (startWeaponChoicePanel == null)
            {
                startWeaponChoicePanel = startWeaponPanelRoot.GetComponent<SurvivorStartWeaponChoicePanel>();
            }

            if (startWeaponChoicePanel == null)
            {
                startWeaponChoicePanel = startWeaponPanelRoot.AddComponent<SurvivorStartWeaponChoicePanel>();
            }

            startWeaponChoicePanel.Configure(weaponController, startWeaponPanelRoot, false);
        }

        private void SetupLevelUpUpgradePanel()
        {
            if (useCustomUpgradeUI)
            {
                SurvivorLevelUpUpgradePanel[] legacyPanels = FindObjectsByType<SurvivorLevelUpUpgradePanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < legacyPanels.Length; i++)
                {
                    if (legacyPanels[i] != null)
                    {
                        legacyPanels[i].enabled = false;
                    }
                }

                return;
            }

            if (!enableLevelUpUpgrades)
            {
                return;
            }

            if (weaponController == null || experience == null)
            {
                return;
            }

            GameObject panelRoot = startWeaponPanelRoot != null
                ? startWeaponPanelRoot
                : FindSceneGameObjectIncludingInactive("Upgrade_Panel");

            if (panelRoot == null)
            {
                Debug.LogWarning($"{nameof(SurvivorModeBootstrap)} could not find Upgrade_Panel for level up upgrades.");
                return;
            }

            panelRoot.SetActive(true);

            if (levelUpUpgradePanel == null)
            {
                levelUpUpgradePanel = panelRoot.GetComponent<SurvivorLevelUpUpgradePanel>();
            }

            if (levelUpUpgradePanel == null)
            {
                levelUpUpgradePanel = panelRoot.AddComponent<SurvivorLevelUpUpgradePanel>();
            }

            levelUpUpgradePanel.Configure(experience, weaponController, panelRoot);
        }

        private void SetupInfiniteGridBackground()
        {
            if (!autoSetupInfiniteGridBackground)
            {
                return;
            }

            if (infiniteGridBackground == null)
            {
                infiniteGridBackground = FindFirstObjectByType<SurvivorInfiniteParallaxBackground>();
            }

            if (infiniteGridBackground != null)
            {
                infiniteGridBackground.Configure(player);
                return;
            }

            Grid[] grids = FindObjectsByType<Grid>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (grids.Length != 4)
            {
                Debug.LogWarning(
                    $"Infinite Grid Background needs exactly four active Grid objects. Found: {grids.Length}.",
                    this);
                return;
            }

            Transform[] gridTransforms = new Transform[grids.Length];
            for (int i = 0; i < grids.Length; i++)
            {
                gridTransforms[i] = grids[i].transform;
            }

            GameObject backgroundObject = new GameObject("Infinite Grid Background");
            infiniteGridBackground = backgroundObject.AddComponent<SurvivorInfiniteParallaxBackground>();
            infiniteGridBackground.Configure(player, gridTransforms);
        }

        private void SetupBossTimeAttack()
        {
            if (!autoSetupBossTimeAttack)
            {
                return;
            }

            if (bossTimeAttack == null)
            {
                bossTimeAttack = FindFirstObjectByType<SurvivorBossTimeAttack>();
            }

            SurvivorEnemySpawner enemySpawner = FindFirstObjectByType<SurvivorEnemySpawner>();

            if (bossTimeAttack == null)
            {
                GameObject timeAttackObject = new GameObject("Boss Time Attack");
                bossTimeAttack = timeAttackObject.AddComponent<SurvivorBossTimeAttack>();
            }

            bossTimeAttack.Configure(player, enemySpawner, bossPrefab);
        }

        private static GameObject FindSceneGameObjectIncludingInactive(string objectName)
        {
            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i].name == objectName && objects[i].scene.IsValid())
                {
                    return objects[i];
                }
            }

            return null;
        }
    }
}
