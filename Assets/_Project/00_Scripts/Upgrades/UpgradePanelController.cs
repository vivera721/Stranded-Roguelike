using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StrandedRoguelike
{
    public sealed class UpgradePanelController : MonoBehaviour
    {
        [SerializeField] private GameObject upgradeGroup;
        [SerializeField] private UpgradeCardUI[] cards = new UpgradeCardUI[3];
        [SerializeField] private bool showOnStart;
        [SerializeField] private bool testOpenWithU = true;

        [Header("Icons")]
        [SerializeField] private Sprite fireIcon;
        [SerializeField] private Sprite poisonIcon;
        [SerializeField] private Sprite missileIcon;
        [SerializeField] private Sprite botIcon;

        private CanvasGroup canvasGroup;
        private readonly List<UpgradeId> pickedUpgrades = new List<UpgradeId>();
        private CompanionSkillController companionSkillController;

        private static readonly UpgradeDefinition[] CompanionUpgrades =
        {
            new UpgradeDefinition(UpgradeId.CompanionFlameBottle, UpgradeCategory.Companion, "Flame Bottle", "Unlocks Flame Bottle."),
            new UpgradeDefinition(UpgradeId.CompanionPoisonBottle, UpgradeCategory.Companion, "Poison Bottle", "Unlocks Poison Bottle."),
            new UpgradeDefinition(UpgradeId.CompanionMissile, UpgradeCategory.Companion, "Missile", "Calls a missile at the target."),
            new UpgradeDefinition(UpgradeId.CompanionFireball, UpgradeCategory.Companion, "Fireball", "Fires at the nearest enemy.")
        };

        private void Awake()
        {
            AutoBind();
            EnsureCanvasGroup();
            FindRuntimeTargets();
            Hide();
        }

        private void Start()
        {
            if (showOnStart)
            {
                ShowUpgradeChoices();
            }
        }

        private void Update()
        {
            if (testOpenWithU && Keyboard.current != null && Keyboard.current.uKey.wasPressedThisFrame)
            {
                ShowUpgradeChoices();
            }
        }

        public Sprite GetIcon(UpgradeId id)
        {
            switch (id)
            {
                case UpgradeId.CompanionFlameBottle:
                    return fireIcon != null ? fireIcon : botIcon;
                case UpgradeId.CompanionPoisonBottle:
                    return poisonIcon != null ? poisonIcon : botIcon;
                case UpgradeId.CompanionMissile:
                    return missileIcon != null ? missileIcon : botIcon;
                case UpgradeId.CompanionFireball:
                    return fireIcon != null ? fireIcon : botIcon;
                default:
                    return botIcon;
            }
        }

        public void ShowUpgradeChoices()
        {
            AutoBind();
            FindRuntimeTargets();
            List<UpgradeDefinition> choices = RollChoices();

            if (cards == null || cards.Length == 0 || !HasAnyCard(cards))
            {
                Debug.LogWarning($"{nameof(UpgradePanelController)} could not find any upgrade cards.", this);
            }

            for (int i = 0; cards != null && i < cards.Length; i++)
            {
                if (cards[i] == null)
                {
                    continue;
                }

                if (i < choices.Count)
                {
                    cards[i].Setup(this, choices[i]);
                }
                else
                {
                    cards[i].gameObject.SetActive(false);
                }
            }

            if (upgradeGroup != null)
            {
                upgradeGroup.SetActive(true);
            }

            EnsureCanvasGroup();
            SetCanvasGroupVisible(canvasGroup, true);
            Time.timeScale = 0f;
        }

        public void SelectUpgrade(UpgradeDefinition upgrade)
        {
            if (pickedUpgrades.Contains(upgrade.Id))
            {
                Hide();
                return;
            }

            pickedUpgrades.Add(upgrade.Id);
            ApplyUpgrade(upgrade.Id);
            Hide();
        }

        private List<UpgradeDefinition> RollChoices()
        {
            List<UpgradeDefinition> choices = new List<UpgradeDefinition>();
            int guard = 0;

            while (choices.Count < 3 && guard < 30)
            {
                guard++;
                AddRandomUnique(choices, CompanionUpgrades);
            }

            return choices;
        }

        private void AddRandomUnique(List<UpgradeDefinition> choices, UpgradeDefinition[] pool)
        {
            List<UpgradeDefinition> candidates = new List<UpgradeDefinition>();

            for (int i = 0; i < pool.Length; i++)
            {
                if (!pickedUpgrades.Contains(pool[i].Id) && !Contains(choices, pool[i].Id))
                {
                    candidates.Add(pool[i]);
                }
            }

            if (candidates.Count <= 0)
            {
                return;
            }

            choices.Add(candidates[Random.Range(0, candidates.Count)]);
        }

        private void ApplyUpgrade(UpgradeId upgradeId)
        {
            switch (upgradeId)
            {
                case UpgradeId.CompanionFlameBottle:
                    companionSkillController?.UnlockSkill(CompanionSkillKind.FlameBottle);
                    break;
                case UpgradeId.CompanionPoisonBottle:
                    companionSkillController?.UnlockSkill(CompanionSkillKind.PoisonBottle);
                    break;
                case UpgradeId.CompanionMissile:
                    companionSkillController?.UnlockSkill(CompanionSkillKind.Missile);
                    break;
                case UpgradeId.CompanionFireball:
                    companionSkillController?.UnlockSkill(CompanionSkillKind.Fireball);
                    break;
            }
        }

        private void Hide()
        {
            EnsureCanvasGroup();

            if (canvasGroup != null)
            {
                SetCanvasGroupVisible(canvasGroup, false);
            }
            else if (upgradeGroup != null && upgradeGroup != gameObject)
            {
                upgradeGroup.SetActive(false);
            }

            Time.timeScale = 1f;
        }

        private void AutoBind()
        {
            if (upgradeGroup == null || upgradeGroup.transform.IsChildOf(transform))
            {
                upgradeGroup = gameObject;
            }

            if (cards == null || cards.Length == 0 || !HasAnyCard(cards))
            {
                cards = GetComponentsInChildren<UpgradeCardUI>(true);

                if (cards == null || cards.Length == 0)
                {
                    cards = FindCardsByName();
                }
            }
        }

        private void EnsureCanvasGroup()
        {
            if (upgradeGroup == null)
            {
                return;
            }

            canvasGroup = upgradeGroup.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = upgradeGroup.AddComponent<CanvasGroup>();
            }
        }

        private static void SetCanvasGroupVisible(CanvasGroup target, bool visible)
        {
            if (target == null)
            {
                return;
            }

            target.gameObject.SetActive(true);
            target.alpha = visible ? 1f : 0f;
            target.interactable = visible;
            target.blocksRaycasts = visible;
        }

        private static bool HasAnyCard(UpgradeCardUI[] targetCards)
        {
            if (targetCards == null)
            {
                return false;
            }

            for (int i = 0; i < targetCards.Length; i++)
            {
                if (targetCards[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static UpgradeCardUI[] FindCardsByName()
        {
            return new[]
            {
                FindCardByName("Upgrade_Panel_1"),
                FindCardByName("Upgrade_Panel_2"),
                FindCardByName("Upgrade_Panel_3")
            };
        }

        private static UpgradeCardUI FindCardByName(string objectName)
        {
            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i].name == objectName && objects[i].scene.IsValid())
                {
                    return objects[i].GetComponent<UpgradeCardUI>();
                }
            }

            return null;
        }

        private void FindRuntimeTargets()
        {
            if (companionSkillController == null)
            {
                SurvivorWeaponController survivorWeaponController = FindFirstObjectByType<SurvivorWeaponController>();
                if (survivorWeaponController != null)
                {
                    companionSkillController = survivorWeaponController.GetComponent<CompanionSkillController>();
                    if (companionSkillController == null)
                    {
                        companionSkillController = survivorWeaponController.gameObject.AddComponent<CompanionSkillController>();
                    }
                }
            }
        }

        private static bool Contains(List<UpgradeDefinition> choices, UpgradeId id)
        {
            for (int i = 0; i < choices.Count; i++)
            {
                if (choices[i].Id == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
