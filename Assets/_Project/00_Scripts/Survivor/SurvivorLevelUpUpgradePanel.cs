using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StrandedRoguelike
{
    public sealed class SurvivorLevelUpUpgradePanel : MonoBehaviour
    {
        private sealed class UpgradeOption
        {
            public SurvivorWeaponKind weaponKind;
            public bool isNewWeapon;
            public string title;
            public string description;
            public Sprite icon;
        }

        [SerializeField] private SurvivorExperience experience;
        [SerializeField] private SurvivorWeaponController weaponController;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button[] buttons = new Button[3];
        [SerializeField] private TMP_Text[] titleTexts = new TMP_Text[3];
        [SerializeField] private TMP_Text[] descriptionTexts = new TMP_Text[3];
        [SerializeField] private Image[] iconImages = new Image[3];
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private bool createHeaderIfMissing = true;

        [Header("UI Auto Fix")]
        [SerializeField] private bool autoCreateMissingDescriptionText = true;
        [SerializeField] private bool setUpgradeImagesNativeSize = true;
        [SerializeField] private bool rotateMissileImage = true;

        [Header("Upgrade Icons")]
        [SerializeField] private Sprite technoBladeIcon;
        [SerializeField] private Sprite poisonBottleIcon;
        [SerializeField] private Sprite flameBottleIcon;
        [SerializeField] private Sprite fireballIcon;
        [SerializeField] private Sprite iceSpikeIcon;
        [SerializeField] private Sprite lightningBoltIcon;
        [SerializeField] private Sprite chainLightningIcon;
        [SerializeField] private Sprite axeBoomerangIcon;
        [SerializeField] private Sprite missileIcon;

        [Header("Vampire-like UI Colors")]
        [SerializeField] private Color panelColor = new Color(0.03f, 0.025f, 0.055f, 0.92f);
        [SerializeField] private Color cardColor = new Color(0.12f, 0.09f, 0.22f, 0.95f);
        [SerializeField] private Color cardHighlightColor = new Color(0.26f, 0.18f, 0.48f, 1f);
        [SerializeField] private Color headerColor = new Color(1f, 0.86f, 0.28f, 1f);

        private readonly SurvivorWeaponKind[] upgradePool =
        {
            SurvivorWeaponKind.TechnoBlade,
            SurvivorWeaponKind.PoisonBottle,
            SurvivorWeaponKind.FlameBottle,
            SurvivorWeaponKind.Fireball,
            SurvivorWeaponKind.IceSpike,
            SurvivorWeaponKind.LightningBolt,
            SurvivorWeaponKind.ChainLightning,
            SurvivorWeaponKind.AxeBoomerang,
            SurvivorWeaponKind.Missile
        };

        private readonly List<UpgradeOption> currentOptions = new List<UpgradeOption>();
        private static readonly Dictionary<SurvivorWeaponKind, Sprite> fallbackIcons = new Dictionary<SurvivorWeaponKind, Sprite>();
        private CanvasGroup canvasGroup;
        private int pendingLevelUps;
        private bool isShowing;

        private void Awake()
        {
            if (HasCustomUpgradeUI())
            {
                enabled = false;
                return;
            }

            AutoBind();
            Hide(false);
        }

        private void OnEnable()
        {
            if (HasCustomUpgradeUI())
            {
                enabled = false;
                return;
            }

            AutoBind();

            if (experience != null)
            {
                experience.LeveledUp += OnLeveledUp;
            }
        }

        private void OnDisable()
        {
            if (experience != null)
            {
                experience.LeveledUp -= OnLeveledUp;
            }
        }

        private static bool HasCustomUpgradeUI()
        {
            return FindFirstObjectByType<global::UpgradeUI>(FindObjectsInactive.Include) != null;
        }

        public void Configure(SurvivorExperience newExperience, SurvivorWeaponController newWeaponController, GameObject newPanelRoot)
        {
            if (experience != null)
            {
                experience.LeveledUp -= OnLeveledUp;
            }

            experience = newExperience;
            weaponController = newWeaponController;
            panelRoot = newPanelRoot != null ? newPanelRoot : gameObject;
            AutoBind();

            if (isActiveAndEnabled && experience != null)
            {
                experience.LeveledUp += OnLeveledUp;
            }
        }

        private void OnLeveledUp(int newLevel)
        {
            pendingLevelUps++;

            if (!isShowing)
            {
                ShowNextLevelUp();
            }
        }

        private void ShowNextLevelUp()
        {
            AutoBind();

            if (weaponController == null)
            {
                Debug.LogWarning($"{nameof(SurvivorLevelUpUpgradePanel)} could not find {nameof(SurvivorWeaponController)}.", this);
                pendingLevelUps = 0;
                return;
            }

            currentOptions.Clear();
            List<UpgradeOption> candidates = BuildCandidates();

            if (candidates.Count == 0)
            {
                pendingLevelUps = 0;
                Hide(true);
                return;
            }

            while (currentOptions.Count < 3 && candidates.Count > 0)
            {
                int index = Random.Range(0, candidates.Count);
                currentOptions.Add(candidates[index]);
                candidates.RemoveAt(index);
            }

            ApplyVisualStyle();
            BindCards();
            SetVisible(true);
            Time.timeScale = 0f;
            isShowing = true;
        }

        private List<UpgradeOption> BuildCandidates()
        {
            List<UpgradeOption> candidates = new List<UpgradeOption>();

            for (int i = 0; i < upgradePool.Length; i++)
            {
                SurvivorWeaponKind kind = upgradePool[i];
                if (!weaponController.CanUpgradeWeapon(kind))
                {
                    continue;
                }

                int currentLevel = weaponController.GetWeaponLevel(kind);
                bool isNewWeapon = currentLevel <= 0;

                candidates.Add(new UpgradeOption
                {
                    weaponKind = kind,
                    isNewWeapon = isNewWeapon,
                    title = isNewWeapon ? GetWeaponTitle(kind) : $"{GetWeaponTitle(kind)} Lv.{currentLevel + 1}",
                    description = isNewWeapon ? GetUnlockDescription(kind) : GetUpgradeDescription(kind, currentLevel + 1),
                    icon = GetIcon(kind)
                });
            }

            return candidates;
        }

        private void BindCards()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null)
                {
                    continue;
                }

                if (i >= currentOptions.Count)
                {
                    buttons[i].gameObject.SetActive(false);
                    continue;
                }

                int optionIndex = i;
                UpgradeOption option = currentOptions[i];
                buttons[i].gameObject.SetActive(true);
                buttons[i].onClick.RemoveAllListeners();
                buttons[i].onClick.AddListener(() => Select(optionIndex));

                if (titleTexts != null && i < titleTexts.Length && titleTexts[i] != null)
                {
                    titleTexts[i].text = option.title;
                }

                if (descriptionTexts != null && i < descriptionTexts.Length && descriptionTexts[i] != null)
                {
                    descriptionTexts[i].gameObject.SetActive(true);
                    descriptionTexts[i].text = option.description;
                }

                if (iconImages != null && i < iconImages.Length && iconImages[i] != null)
                {
                    ApplyIcon(iconImages[i], option.icon, option.weaponKind);
                }
            }

            if (headerText != null)
            {
                int level = experience != null ? experience.Level : 1;
                headerText.text = $"LEVEL UP!  Lv.{level}";
            }
        }

        private void Select(int index)
        {
            if (index < 0 || index >= currentOptions.Count || weaponController == null)
            {
                return;
            }

            UpgradeOption option = currentOptions[index];
            if (option.isNewWeapon)
            {
                weaponController.UnlockWeapon(option.weaponKind);
            }
            else
            {
                weaponController.UpgradeWeapon(option.weaponKind);
            }

            pendingLevelUps = Mathf.Max(0, pendingLevelUps - 1);

            if (pendingLevelUps > 0)
            {
                ShowNextLevelUp();
            }
            else
            {
                Hide(true);
            }
        }

        private void Hide(bool resumeTime)
        {
            isShowing = false;
            SetVisible(false);

            if (resumeTime)
            {
                Time.timeScale = 1f;
            }
        }

        private void SetVisible(bool visible)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        private void AutoBind()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            if (experience == null)
            {
                experience = FindFirstObjectByType<SurvivorExperience>();
            }

            if (weaponController == null)
            {
                weaponController = FindFirstObjectByType<SurvivorWeaponController>();
            }

            canvasGroup = panelRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panelRoot.AddComponent<CanvasGroup>();
            }

            if (buttons == null || buttons.Length == 0 || !HasAnyButton(buttons))
            {
                buttons = panelRoot.GetComponentsInChildren<Button>(true);
            }

            if (buttons != null && buttons.Length > 0)
            {
                EnsureCardArrays();
            }

            if (headerText == null && createHeaderIfMissing)
            {
                headerText = CreateHeaderText();
            }
        }

        private void EnsureCardArrays()
        {
            if (titleTexts == null || titleTexts.Length != buttons.Length)
            {
                titleTexts = new TMP_Text[buttons.Length];
            }

            if (descriptionTexts == null || descriptionTexts.Length != buttons.Length)
            {
                descriptionTexts = new TMP_Text[buttons.Length];
            }

            if (iconImages == null || iconImages.Length != buttons.Length)
            {
                iconImages = new Image[buttons.Length];
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null)
                {
                    continue;
                }

                BindCardTexts(i);

                if (iconImages[i] == null || iconImages[i] == buttons[i].GetComponent<Image>())
                {
                    iconImages[i] = FindIconImage(buttons[i]);
                }
            }
        }

        private void BindCardTexts(int index)
        {
            Transform searchRoot = GetCardSearchRoot(buttons[index]);
            TMP_Text[] texts = searchRoot.GetComponentsInChildren<TMP_Text>(true);

            if (titleTexts[index] == null)
            {
                titleTexts[index] = FindTextByName(texts, "title", "name", "label");
            }

            TMP_Text namedDescription = FindTextByName(texts, "description", "desc", "explain", "info");
            if (namedDescription != null && (descriptionTexts[index] == null || !descriptionTexts[index].transform.IsChildOf(searchRoot)))
            {
                descriptionTexts[index] = namedDescription;
            }

            if (titleTexts[index] == null && texts.Length > 0)
            {
                titleTexts[index] = texts[0];
            }

            if (descriptionTexts[index] == null)
            {
                for (int i = 0; i < texts.Length; i++)
                {
                    if (texts[i] != null && texts[i] != titleTexts[index])
                    {
                        descriptionTexts[index] = texts[i];
                        break;
                    }
                }
            }

            if ((descriptionTexts[index] == null || !descriptionTexts[index].transform.IsChildOf(searchRoot)) && autoCreateMissingDescriptionText)
            {
                descriptionTexts[index] = CreateDescriptionText(searchRoot);
            }
        }

        private static Transform GetCardSearchRoot(Button button)
        {
            if (button.transform.parent == null)
            {
                return button.transform;
            }

            Button[] siblingButtons = button.transform.parent.GetComponentsInChildren<Button>(true);
            return siblingButtons.Length <= 1 ? button.transform.parent : button.transform;
        }

        private static TMP_Text FindTextByName(TMP_Text[] texts, params string[] keywords)
        {
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] == null)
                {
                    continue;
                }

                string textName = texts[i].gameObject.name.ToLowerInvariant();
                for (int j = 0; j < keywords.Length; j++)
                {
                    if (textName.Contains(keywords[j]))
                    {
                        return texts[i];
                    }
                }
            }

            return null;
        }

        private static TMP_Text CreateDescriptionText(Transform parent)
        {
            GameObject textObject = new GameObject("Description");
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 8f);
            rect.sizeDelta = new Vector2(280f, 90f);

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.color = Color.white;
            text.fontSize = 17f;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        private void ApplyIcon(Image target, Sprite icon, SurvivorWeaponKind kind)
        {
            if (target == null)
            {
                return;
            }

            target.sprite = icon;
            target.enabled = icon != null;
            target.preserveAspect = true;

            if (icon != null && setUpgradeImagesNativeSize)
            {
                target.SetNativeSize();
            }

            RectTransform rect = target.rectTransform;
            if (rect != null)
            {
                rect.localRotation = rotateMissileImage && kind == SurvivorWeaponKind.Missile
                    ? Quaternion.Euler(0f, 0f, 90f)
                    : Quaternion.identity;
            }
        }

        private void ApplyVisualStyle()
        {
            Image panelImage = panelRoot != null ? panelRoot.GetComponent<Image>() : null;
            if (panelImage != null)
            {
                panelImage.color = panelColor;
            }

            for (int i = 0; buttons != null && i < buttons.Length; i++)
            {
                if (buttons[i] == null)
                {
                    continue;
                }

                Image buttonImage = buttons[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = cardColor;
                }

                ColorBlock colors = buttons[i].colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = cardHighlightColor;
                colors.selectedColor = cardHighlightColor;
                colors.pressedColor = cardHighlightColor * 0.85f;
                buttons[i].colors = colors;
            }

            if (headerText != null)
            {
                headerText.color = headerColor;
                headerText.alignment = TextAlignmentOptions.Center;
                headerText.fontSize = 42f;
            }
        }

        private TMP_Text CreateHeaderText()
        {
            GameObject headerObject = new GameObject("Survivor Level Up Header");
            headerObject.transform.SetParent(panelRoot.transform, false);
            RectTransform rect = headerObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -35f);
            rect.sizeDelta = new Vector2(700f, 70f);

            TextMeshProUGUI text = headerObject.AddComponent<TextMeshProUGUI>();
            text.text = "LEVEL UP!";
            text.color = headerColor;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 42f;
            return text;
        }

        private Sprite GetIcon(SurvivorWeaponKind kind)
        {
            Sprite icon = null;

            switch (kind)
            {
                case SurvivorWeaponKind.TechnoBlade:
                    icon = technoBladeIcon;
                    break;
                case SurvivorWeaponKind.PoisonBottle:
                    icon = poisonBottleIcon;
                    break;
                case SurvivorWeaponKind.FlameBottle:
                    icon = flameBottleIcon;
                    break;
                case SurvivorWeaponKind.Fireball:
                    icon = fireballIcon;
                    break;
                case SurvivorWeaponKind.IceSpike:
                    icon = iceSpikeIcon;
                    break;
                case SurvivorWeaponKind.LightningBolt:
                    icon = lightningBoltIcon;
                    break;
                case SurvivorWeaponKind.ChainLightning:
                    icon = chainLightningIcon;
                    break;
                case SurvivorWeaponKind.AxeBoomerang:
                    icon = axeBoomerangIcon;
                    break;
                case SurvivorWeaponKind.Missile:
                    icon = missileIcon;
                    break;
            }

            return icon != null ? icon : GetFallbackIcon(kind);
        }

        private static Sprite GetFallbackIcon(SurvivorWeaponKind kind)
        {
            if (fallbackIcons.TryGetValue(kind, out Sprite sprite) && sprite != null)
            {
                return sprite;
            }

            Color color = GetFallbackColor(kind);
            Texture2D texture = new Texture2D(32, 32);
            texture.filterMode = FilterMode.Point;
            Vector2 center = new Vector2(15.5f, 15.5f);

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    bool inside = distance <= 13f;
                    bool border = distance > 10.5f && distance <= 13f;
                    texture.SetPixel(x, y, inside ? (border ? Color.white : color) : Color.clear);
                }
            }

            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0f, 0f, 32f, 32f), new Vector2(0.5f, 0.5f), 32f);
            fallbackIcons[kind] = sprite;
            return sprite;
        }

        private static Color GetFallbackColor(SurvivorWeaponKind kind)
        {
            switch (kind)
            {
                case SurvivorWeaponKind.TechnoBlade:
                    return new Color(0.25f, 0.9f, 1f, 1f);
                case SurvivorWeaponKind.PoisonBottle:
                    return new Color(0.25f, 0.95f, 0.25f, 1f);
                case SurvivorWeaponKind.FlameBottle:
                case SurvivorWeaponKind.Fireball:
                    return new Color(1f, 0.35f, 0.12f, 1f);
                case SurvivorWeaponKind.IceSpike:
                    return new Color(0.45f, 0.85f, 1f, 1f);
                case SurvivorWeaponKind.LightningBolt:
                case SurvivorWeaponKind.ChainLightning:
                    return new Color(1f, 0.9f, 0.2f, 1f);
                case SurvivorWeaponKind.AxeBoomerang:
                    return new Color(0.8f, 0.8f, 0.9f, 1f);
                case SurvivorWeaponKind.Missile:
                    return new Color(1f, 0.75f, 0.2f, 1f);
                default:
                    return Color.white;
            }
        }

        private static string GetWeaponTitle(SurvivorWeaponKind kind)
        {
            switch (kind)
            {
                case SurvivorWeaponKind.TechnoBlade:
                    return "Techno Blade";
                case SurvivorWeaponKind.PoisonBottle:
                    return "Poison Bottle";
                case SurvivorWeaponKind.FlameBottle:
                    return "Flame Bottle";
                case SurvivorWeaponKind.Fireball:
                    return "Fireball";
                case SurvivorWeaponKind.IceSpike:
                    return "Ice Spike";
                case SurvivorWeaponKind.LightningBolt:
                    return "Lightning Bolt";
                case SurvivorWeaponKind.ChainLightning:
                    return "Chain Lightning";
                case SurvivorWeaponKind.AxeBoomerang:
                    return "Axe Boomerang";
                case SurvivorWeaponKind.Missile:
                    return "Missile";
                default:
                    return kind.ToString();
            }
        }

        private static string GetUnlockDescription(SurvivorWeaponKind kind)
        {
            switch (kind)
            {
                case SurvivorWeaponKind.TechnoBlade:
                    return "Continuously orbits you and damages enemies.";
                case SurvivorWeaponKind.PoisonBottle:
                    return "Throw poison bottles in an arc. Poison can spread on enemy death.";
                case SurvivorWeaponKind.FlameBottle:
                    return "Throw flame bottles in an arc. Burning enemies can explode on death.";
                case SurvivorWeaponKind.Fireball:
                    return "Shoot a fireball toward the nearest enemy.";
                case SurvivorWeaponKind.IceSpike:
                    return "Create delayed ice spikes behind your movement path. Slows enemies.";
                case SurvivorWeaponKind.LightningBolt:
                    return "Strike random ground with powerful lightning.";
                case SurvivorWeaponKind.ChainLightning:
                    return "Your hits chain lightning damage to nearby enemies.";
                case SurvivorWeaponKind.AxeBoomerang:
                    return "Throw an axe in a high arc. It returns to you after hitting enemies.";
                case SurvivorWeaponKind.Missile:
                    return "Mark a target, then call down a heavy delayed missile strike.";
                default:
                    return string.Empty;
            }
        }

        private static string GetUpgradeDescription(SurvivorWeaponKind kind, int nextLevel)
        {
            switch (kind)
            {
                case SurvivorWeaponKind.Fireball:
                    return nextLevel <= 5 ? "Increase fireball damage and add another fireball to the fan." : "Increase fireball damage and reduce cooldown.";
                case SurvivorWeaponKind.ChainLightning:
                    return "Increase chain damage and chain jump count.";
                case SurvivorWeaponKind.LightningBolt:
                    return "Increase lightning damage and reduce cooldown.";
                case SurvivorWeaponKind.IceSpike:
                    return "Increase spike damage, radius, and slow duration.";
                case SurvivorWeaponKind.TechnoBlade:
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
                        default:
                            return "Upgrade Techno Blade.";
                    }
                case SurvivorWeaponKind.AxeBoomerang:
                    return "Increase axe damage and reduce cooldown.";
                case SurvivorWeaponKind.PoisonBottle:
                    return "Increase poison bottle damage, radius, and reduce cooldown.";
                case SurvivorWeaponKind.FlameBottle:
                    return "Increase flame bottle damage, radius, and reduce cooldown.";
                case SurvivorWeaponKind.Missile:
                    return "Increase missile damage, blast radius, and reduce cooldown.";
                default:
                    return "Upgrade this weapon.";
            }
        }

        private static bool HasAnyButton(Button[] targetButtons)
        {
            if (targetButtons == null)
            {
                return false;
            }

            for (int i = 0; i < targetButtons.Length; i++)
            {
                if (targetButtons[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static Image FindIconImage(Button button)
        {
            Image rootImage = button.GetComponent<Image>();
            Image[] images = button.GetComponentsInChildren<Image>(true);

            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] == rootImage)
                {
                    continue;
                }

                string imageName = images[i].gameObject.name.ToLowerInvariant();
                if (imageName.Contains("icon") || imageName.Contains("image") || imageName.Contains("sprite"))
                {
                    return images[i];
                }
            }

            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != rootImage)
                {
                    return images[i];
                }
            }

            return null;
        }
    }
}
