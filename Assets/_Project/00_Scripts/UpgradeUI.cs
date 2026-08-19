using StrandedRoguelike;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UpgradeUI : MonoBehaviour
{
    private static UpgradeUI activeInstance;

    [Header("Window")]
    [SerializeField] private GameObject upgradeWindow;
    [SerializeField] private UpgradePanelUI upgradePanel_1;
    [SerializeField] private UpgradePanelUI upgradePanel_2;
    [SerializeField] private UpgradePanelUI upgradePanel_3;
    [SerializeField] private bool testOpenWithU = true;

    [Header("Start")]
    [SerializeField] private bool unlockTechnoBladeOnStart = true;
    [SerializeField] private bool openUpgradeOnStart;

    [Header("Upgrade Pool")]
    [SerializeField] private UpgradeData[] upgrades;

    private readonly List<UpgradeData> currentChoices = new List<UpgradeData>();
    private readonly List<UpgradeData> fallbackUpgrades = new List<UpgradeData>();
    private SurvivorWeaponController weaponController;
    private SurvivorExperience experience;
    private PlayerHealth playerHealth;
    private CanvasGroup canvasGroup;
    private bool isOpen;
    private int pendingLevelUps;

    private UpgradePanelUI[] Panels => new[] { upgradePanel_1, upgradePanel_2, upgradePanel_3 };

    private void Awake()
    {
        AutoBind();
        if (!ClaimPrimaryInstance())
        {
            return;
        }

        HideWindow(false);
    }

    private void OnEnable()
    {
        AutoBind();
        if (!ClaimPrimaryInstance())
        {
            return;
        }

        SubscribeExperience();
    }

    private void OnDisable()
    {
        UnsubscribeExperience();

        if (activeInstance == this)
        {
            activeInstance = null;
        }
    }

    private void Start()
    {
        AutoBind();

        if (unlockTechnoBladeOnStart && weaponController != null && !weaponController.HasWeapon(SurvivorWeaponKind.TechnoBlade))
        {
            weaponController.UnlockWeapon(SurvivorWeaponKind.TechnoBlade);
        }

        if (openUpgradeOnStart)
        {
            OpenUpgradeUI();
        }
        else
        {
            HideWindow(false);
        }
    }

    private void Update()
    {
        if (testOpenWithU && Keyboard.current != null && Keyboard.current.uKey.wasPressedThisFrame)
        {
            OpenUpgradeUI();
        }
    }

    public void Configure(SurvivorExperience newExperience, SurvivorWeaponController newWeaponController)
    {
        AutoBind();
        if (!ClaimPrimaryInstance())
        {
            return;
        }

        UnsubscribeExperience();
        experience = newExperience;
        weaponController = newWeaponController;
        SubscribeExperience();
    }

    public bool HasPanelReferences()
    {
        AutoBind();
        UpgradePanelUI[] panels = Panels;
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    public void OpenUpgradeUI()
    {
        AutoBind();

        if (weaponController == null)
        {
            Debug.LogWarning($"{nameof(UpgradeUI)} could not find {nameof(SurvivorWeaponController)}.", this);
            return;
        }

        RollChoices();

        if (currentChoices.Count == 0)
        {
            pendingLevelUps = 0;
            HideWindow(true);
            return;
        }

        BindPanels();
        ShowWindow();
    }

    public void SelectUpgrade(UpgradeData upgradeData)
    {
        if (upgradeData == null)
        {
            return;
        }

        if (upgradeData.type == UpgradeType.Hp)
        {
            if (playerHealth == null)
            {
                playerHealth = FindFirstObjectByType<PlayerHealth>();
            }

            if (playerHealth == null)
            {
                Debug.LogWarning($"{nameof(UpgradeUI)} could not find {nameof(PlayerHealth)}.", this);
                return;
            }

            int healedAmount = playerHealth.IncreaseMaxHealthAndHealPercent(1, 0.5f);
            Debug.Log(
                $"[UpgradeUI] Applied Upgrade: Max HP +1 / Healed {healedAmount} / Current {playerHealth.CurrentHealth} / Max {playerHealth.MaxHealth}",
                this);
            CompleteUpgradeSelection();
            return;
        }

        if (weaponController == null)
        {
            return;
        }

        SurvivorWeaponKind weaponKind = ConvertToWeaponKind(upgradeData.type);
        int beforeLevel = weaponController.GetWeaponLevel(weaponKind);
        Debug.Log($"[UpgradeUI] Selected Upgrade: {upgradeData.title} / Type: {upgradeData.type} / Weapon: {weaponKind} / Current Level: {beforeLevel}", this);
        weaponController.UnlockWeapon(weaponKind);
        int afterLevel = weaponController.GetWeaponLevel(weaponKind);
        Debug.Log($"[UpgradeUI] Applied Upgrade: {weaponKind} Lv.{beforeLevel} -> Lv.{afterLevel}", this);

        CompleteUpgradeSelection();
    }

    public void SelectUpgrade(SurvivorWeaponKind weaponKind)
    {
        if (weaponController == null)
        {
            return;
        }

        weaponController.UnlockWeapon(weaponKind);
        Debug.Log($"[UpgradeUI] Selected Upgrade By WeaponKind: {weaponKind} / New Level: {weaponController.GetWeaponLevel(weaponKind)}", this);
        pendingLevelUps = Mathf.Max(0, pendingLevelUps - 1);
        HideWindow(true);
    }

    private void OnLeveledUp(int newLevel)
    {
        pendingLevelUps++;

        if (!isOpen)
        {
            OpenUpgradeUI();
        }
    }

    private void RollChoices()
    {
        currentChoices.Clear();

        List<UpgradeData> candidates = BuildCandidates();
        while (currentChoices.Count < 3 && candidates.Count > 0)
        {
            int index = Random.Range(0, candidates.Count);
            currentChoices.Add(candidates[index]);
            candidates.RemoveAt(index);
        }
    }

    private List<UpgradeData> BuildCandidates()
    {
        List<UpgradeData> candidates = new List<UpgradeData>();
        UpgradeData[] pool = upgrades != null && upgrades.Length > 0 ? upgrades : GetFallbackUpgrades();

        for (int i = 0; i < pool.Length; i++)
        {
            UpgradeData data = pool[i];
            if (data == null)
            {
                continue;
            }

            if (data.type == UpgradeType.Hp)
            {
                if (!ContainsType(candidates, data.type))
                {
                    candidates.Add(CreateDisplayData(data, SurvivorWeaponKind.TechnoBlade));
                }

                continue;
            }

            SurvivorWeaponKind kind = ConvertToWeaponKind(data.type);
            if (weaponController != null && !weaponController.CanUpgradeWeapon(kind))
            {
                continue;
            }

            if (ContainsType(candidates, data.type))
            {
                continue;
            }

            candidates.Add(CreateDisplayData(data, kind));
        }

        return candidates;
    }

    private UpgradeData CreateDisplayData(UpgradeData source, SurvivorWeaponKind kind)
    {
        UpgradeData displayData = new UpgradeData
        {
            type = source.type,
            icon = source.icon,
            title = string.IsNullOrWhiteSpace(source.title) ? GetDefaultTitle(source.type) : source.title,
            description = source.description
        };

        if (source.type == UpgradeType.Hp)
        {
            displayData.description = GetDefaultUnlockDescription(source.type);
            return displayData;
        }

        int currentLevel = weaponController != null ? weaponController.GetWeaponLevel(kind) : 0;
        int nextLevel = currentLevel + 1;

        if (currentLevel <= 0)
        {
            if (string.IsNullOrWhiteSpace(displayData.description))
            {
                displayData.description = GetDefaultUnlockDescription(source.type);
            }

            return displayData;
        }

        displayData.title = $"{displayData.title} Lv.{nextLevel}";
        displayData.description = GetLevelUpgradeDescription(source.type, nextLevel);
        return displayData;
    }

    private void BindPanels()
    {
        UpgradePanelUI[] panels = Panels;

        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] == null)
            {
                continue;
            }

            if (i < currentChoices.Count)
            {
                panels[i].Setup(this, currentChoices[i]);
            }
            else
            {
                panels[i].Clear();
            }
        }
    }

    private void CompleteUpgradeSelection()
    {
        pendingLevelUps = Mathf.Max(0, pendingLevelUps - 1);

        if (pendingLevelUps > 0)
        {
            OpenUpgradeUI();
            return;
        }

        HideWindow(true);
    }

    private void ShowWindow()
    {
        EnsureCanvasGroup();

        if (upgradeWindow != null)
        {
            upgradeWindow.SetActive(true);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        Time.timeScale = 0f;
        isOpen = true;
    }

    private void HideWindow(bool resumeTime)
    {
        EnsureCanvasGroup();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (upgradeWindow != null && upgradeWindow != gameObject)
        {
            upgradeWindow.SetActive(false);
        }

        if (resumeTime)
        {
            Time.timeScale = 1f;
        }

        isOpen = false;
    }

    private void AutoBind()
    {
        if (weaponController == null)
        {
            weaponController = FindFirstObjectByType<SurvivorWeaponController>();
        }

        if (experience == null)
        {
            experience = FindFirstObjectByType<SurvivorExperience>();
        }

        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        if (upgradeWindow == null)
        {
            upgradeWindow = gameObject;
        }

        if (upgradePanel_1 == null || upgradePanel_2 == null || upgradePanel_3 == null)
        {
            UpgradePanelUI[] panels = upgradeWindow.GetComponentsInChildren<UpgradePanelUI>(true);
            if (upgradePanel_1 == null && panels.Length > 0)
            {
                upgradePanel_1 = panels[0];
            }

            if (upgradePanel_2 == null && panels.Length > 1)
            {
                upgradePanel_2 = panels[1];
            }

            if (upgradePanel_3 == null && panels.Length > 2)
            {
                upgradePanel_3 = panels[2];
            }
        }

        EnsureCanvasGroup();
    }

    private bool ClaimPrimaryInstance()
    {
        if (activeInstance == null || activeInstance == this)
        {
            activeInstance = this;
            return true;
        }

        bool thisHasPanels = HasPanelReferencesWithoutAutoBind();
        bool activeHasPanels = activeInstance.HasPanelReferencesWithoutAutoBind();

        if (thisHasPanels && !activeHasPanels)
        {
            activeInstance.enabled = false;
            activeInstance = this;
            return true;
        }

        enabled = false;
        return false;
    }

    private bool HasPanelReferencesWithoutAutoBind()
    {
        return upgradePanel_1 != null || upgradePanel_2 != null || upgradePanel_3 != null;
    }

    private void EnsureCanvasGroup()
    {
        if (upgradeWindow == null)
        {
            return;
        }

        canvasGroup = upgradeWindow.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = upgradeWindow.AddComponent<CanvasGroup>();
        }
    }

    private void SubscribeExperience()
    {
        if (experience != null)
        {
            experience.LeveledUp -= OnLeveledUp;
            experience.LeveledUp += OnLeveledUp;
        }
    }

    private void UnsubscribeExperience()
    {
        if (experience != null)
        {
            experience.LeveledUp -= OnLeveledUp;
        }
    }

    private UpgradeData[] GetFallbackUpgrades()
    {
        if (fallbackUpgrades.Count == 0)
        {
            fallbackUpgrades.Add(CreateFallback(UpgradeType.TechnoBlade));
            fallbackUpgrades.Add(CreateFallback(UpgradeType.PoisonBottle));
            fallbackUpgrades.Add(CreateFallback(UpgradeType.FlameBottle));
            fallbackUpgrades.Add(CreateFallback(UpgradeType.Fireball));
            fallbackUpgrades.Add(CreateFallback(UpgradeType.IceSpikes));
            fallbackUpgrades.Add(CreateFallback(UpgradeType.LightningBolt));
            fallbackUpgrades.Add(CreateFallback(UpgradeType.ChainLightning));
            fallbackUpgrades.Add(CreateFallback(UpgradeType.AxeBoomerang));
            fallbackUpgrades.Add(CreateFallback(UpgradeType.Missile));
            fallbackUpgrades.Add(CreateFallback(UpgradeType.Hp));
        }

        return fallbackUpgrades.ToArray();
    }

    private static UpgradeData CreateFallback(UpgradeType type)
    {
        UpgradeData data = new UpgradeData
        {
            type = type
        };

        FillMissingText(data);
        return data;
    }

    private static void FillMissingText(UpgradeData data)
    {
        if (data == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(data.title))
        {
            data.title = GetDefaultTitle(data.type);
        }

        if (string.IsNullOrWhiteSpace(data.description))
        {
            data.description = GetDefaultUnlockDescription(data.type);
        }
    }

    private static bool ContainsType(List<UpgradeData> list, UpgradeType type)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null && list[i].type == type)
            {
                return true;
            }
        }

        return false;
    }

    private static SurvivorWeaponKind ConvertToWeaponKind(UpgradeType upgradeType)
    {
        switch (upgradeType)
        {
            case UpgradeType.AxeBoomerang:
                return SurvivorWeaponKind.AxeBoomerang;
            case UpgradeType.ChainLightning:
                return SurvivorWeaponKind.ChainLightning;
            case UpgradeType.Fireball:
                return SurvivorWeaponKind.Fireball;
            case UpgradeType.FlameBottle:
                return SurvivorWeaponKind.FlameBottle;
            case UpgradeType.IceSpikes:
                return SurvivorWeaponKind.IceSpike;
            case UpgradeType.LightningBolt:
                return SurvivorWeaponKind.LightningBolt;
            case UpgradeType.Missile:
                return SurvivorWeaponKind.Missile;
            case UpgradeType.PoisonBottle:
                return SurvivorWeaponKind.PoisonBottle;
            case UpgradeType.TechnoBlade:
                return SurvivorWeaponKind.TechnoBlade;
            default:
                return SurvivorWeaponKind.TechnoBlade;
        }
    }

    private static string GetDefaultTitle(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.AxeBoomerang:
                return "Axe Boomerang";
            case UpgradeType.ChainLightning:
                return "Chain Lightning";
            case UpgradeType.Fireball:
                return "Fireball";
            case UpgradeType.FlameBottle:
                return "Flame Bottle";
            case UpgradeType.IceSpikes:
                return "Ice Spikes";
            case UpgradeType.LightningBolt:
                return "Lightning Bolt";
            case UpgradeType.Missile:
                return "Missile";
            case UpgradeType.PoisonBottle:
                return "Poison Bottle";
            case UpgradeType.TechnoBlade:
                return "Techno Blade";
            case UpgradeType.Hp:
                return "Max HP +1";
            default:
                return type.ToString();
        }
    }

    private static string GetDefaultUnlockDescription(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.AxeBoomerang:
                return "Throws an axe that returns.";
            case UpgradeType.ChainLightning:
                return "Hits may chain lightning.";
            case UpgradeType.Fireball:
                return "Fires at a nearby enemy.";
            case UpgradeType.FlameBottle:
                return "Throws a burning bottle.";
            case UpgradeType.IceSpikes:
                return "Leaves ice spikes behind.";
            case UpgradeType.LightningBolt:
                return "Strikes a random area.";
            case UpgradeType.Missile:
                return "Calls a missile on a marked area.";
            case UpgradeType.PoisonBottle:
                return "Throws a poisoning bottle.";
            case UpgradeType.TechnoBlade:
                return "An orbiting blade continuously damages enemies.";
            case UpgradeType.Hp:
                return "Max HP +1. Restore 50% HP.";
            default:
                return "Choose an upgrade.";
        }
    }

    private static string GetLevelUpgradeDescription(UpgradeType type, int nextLevel)
    {
        switch (type)
        {
            case UpgradeType.Fireball:
                switch (nextLevel)
                {
                    case 2:
                        return "Damage +1.";
                    case 3:
                        return "Pierces 1 more enemy.";
                    case 4:
                        return "Damage +1.";
                    case 5:
                        return "Projectile +1.";
                    case 6:
                        return "Projectiles +2.";
                }
                break;
            case UpgradeType.AxeBoomerang:
                switch (nextLevel)
                {
                    case 2:
                        return "Damage +2.";
                    case 3:
                        return "Cooldown -20%.";
                    case 4:
                        return "Damage +2.";
                    case 5:
                        return "Cooldown -20% more.";
                    case 6:
                        return "Size and hit area increased.";
                }
                break;
            case UpgradeType.FlameBottle:
                if (nextLevel == 3)
                {
                    return "Blast radius: 75%.";
                }

                if (nextLevel == 6)
                {
                    return "Blast radius: 100%.";
                }

                return "Blast and burn damage +1.";
            case UpgradeType.PoisonBottle:
                if (nextLevel == 3)
                {
                    return "Blast radius: 75%.";
                }

                if (nextLevel == 6)
                {
                    return "Blast radius: 100%.";
                }

                return "Poison duration +1 sec.";
            case UpgradeType.IceSpikes:
                switch (nextLevel)
                {
                    case 2:
                        return "Damage +2.";
                    case 3:
                        return "Cooldown -20%.";
                    case 4:
                        return "Damage +2.";
                    case 5:
                        return "Cooldown -15% more.";
                    case 6:
                        return "Duration x1.5.";
                }
                break;
            case UpgradeType.ChainLightning:
                switch (nextLevel)
                {
                    case 2:
                        return "Damage +1.";
                    case 3:
                        return "Chain chance: 75%.";
                    case 4:
                        return "Targets +1.";
                    case 5:
                        return "Chain chance: 100%.";
                    case 6:
                        return "Damage up. DoT can chain.";
                }
                break;
            case UpgradeType.LightningBolt:
                return GetDamageCooldownDamageCooldownFinalDescription("Lightning", nextLevel);
            case UpgradeType.Missile:
                return GetDamageCooldownDamageCooldownFinalDescription("Missile", nextLevel);
            case UpgradeType.TechnoBlade:
                switch (nextLevel)
                {
                    case 2:
                        return "Speed +20%.";
                    case 3:
                        return "Damage +1. Speed +10%.";
                    case 4:
                        return "Blade +1.";
                    case 5:
                        return "Damage +1. Speed +10%.";
                    case 6:
                        return "Blade +1.";
                }
                break;
        }

        return "Ability upgraded.";
    }

    private static string GetDamageCooldownDamageCooldownFinalDescription(string weaponName, int nextLevel)
    {
        switch (nextLevel)
        {
            case 2:
                return $"{weaponName} damage +2.";
            case 3:
                return $"{weaponName} cooldown -20%.";
            case 4:
                return $"{weaponName} damage +2.";
            case 5:
                return $"{weaponName} cooldown -15% more.";
            case 6:
                return $"{weaponName} damage +2. Fires twice.";
            default:
                return $"{weaponName} upgraded.";
        }
    }
}
