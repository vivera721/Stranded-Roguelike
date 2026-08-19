using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StrandedRoguelike
{
    public sealed class SurvivorStartWeaponChoicePanel : MonoBehaviour
    {
        [SerializeField] private SurvivorWeaponController weaponController;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button[] buttons = new Button[3];
        [SerializeField] private TMP_Text[] titleTexts = new TMP_Text[3];
        [SerializeField] private TMP_Text[] descriptionTexts = new TMP_Text[3];
        [SerializeField] private Image[] iconImages = new Image[3];
        [SerializeField] private bool showOnStart = true;

        [Header("UI Auto Fix")]
        [SerializeField] private bool autoCreateMissingDescriptionText = true;
        [SerializeField] private bool setUpgradeImagesNativeSize = true;
        [SerializeField] private bool rotateMissileImage = true;

        [Header("Icons")]
        [SerializeField] private Sprite technoBladeIcon;
        [SerializeField] private Sprite poisonBottleIcon;
        [SerializeField] private Sprite flameBottleIcon;
        [SerializeField] private Sprite fireballIcon;
        [SerializeField] private Sprite iceSpikeIcon;
        [SerializeField] private Sprite axeBoomerangIcon;

        private readonly SurvivorWeaponKind[] startPool =
        {
            SurvivorWeaponKind.TechnoBlade,
            SurvivorWeaponKind.PoisonBottle,
            SurvivorWeaponKind.FlameBottle,
            SurvivorWeaponKind.Fireball,
            SurvivorWeaponKind.IceSpike,
            SurvivorWeaponKind.AxeBoomerang
        };

        private CanvasGroup canvasGroup;
        private SurvivorWeaponKind[] currentChoices;
        private bool hasSelectedWeapon;

        private void Awake()
        {
            AutoBind();
            Hide(false);
        }

        private void Start()
        {
            if (showOnStart)
            {
                Show();
            }
        }

        public void Show()
        {
            if (hasSelectedWeapon)
            {
                return;
            }

            AutoBind();

            if (weaponController == null)
            {
                Debug.LogWarning($"{nameof(SurvivorStartWeaponChoicePanel)} could not find {nameof(SurvivorWeaponController)}. A random start weapon will be unlocked.", this);
                UnlockFallbackWeapon();
                return;
            }

            currentChoices = RollChoices();

            if (buttons == null || buttons.Length == 0 || buttons[0] == null)
            {
                Debug.LogWarning($"{nameof(SurvivorStartWeaponChoicePanel)} has no buttons assigned. A random start weapon will be unlocked.", this);
                weaponController.UnlockWeapon(currentChoices[0]);
                return;
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null)
                {
                    continue;
                }

                if (i >= currentChoices.Length)
                {
                    buttons[i].gameObject.SetActive(false);
                    continue;
                }

                int choiceIndex = i;
                SurvivorWeaponKind kind = currentChoices[i];
                buttons[i].gameObject.SetActive(true);
                buttons[i].onClick.RemoveAllListeners();
                buttons[i].onClick.AddListener(() => Select(choiceIndex));

                if (titleTexts != null && i < titleTexts.Length && titleTexts[i] != null)
                {
                    titleTexts[i].text = GetTitle(kind);
                }

                if (descriptionTexts != null && i < descriptionTexts.Length && descriptionTexts[i] != null)
                {
                    descriptionTexts[i].gameObject.SetActive(true);
                    descriptionTexts[i].text = GetDescription(kind);
                }

                if (iconImages != null && i < iconImages.Length && iconImages[i] != null)
                {
                    ApplyIcon(iconImages[i], GetIcon(kind), kind);
                }
            }

            SetVisible(true);
            Time.timeScale = 0f;
        }

        private void Select(int index)
        {
            if (currentChoices == null || index < 0 || index >= currentChoices.Length)
            {
                return;
            }

            weaponController.UnlockWeapon(currentChoices[index]);
            hasSelectedWeapon = true;
            Hide(true);
        }

        public void Configure(SurvivorWeaponController newWeaponController, GameObject newPanelRoot, bool newShowOnStart)
        {
            weaponController = newWeaponController;
            panelRoot = newPanelRoot != null ? newPanelRoot : gameObject;
            showOnStart = newShowOnStart;
            AutoBind();
        }

        private SurvivorWeaponKind[] RollChoices()
        {
            List<SurvivorWeaponKind> candidates = new List<SurvivorWeaponKind>(startPool);
            List<SurvivorWeaponKind> choices = new List<SurvivorWeaponKind>();

            while (choices.Count < 3 && candidates.Count > 0)
            {
                int index = Random.Range(0, candidates.Count);
                choices.Add(candidates[index]);
                candidates.RemoveAt(index);
            }

            return choices.ToArray();
        }

        private void UnlockFallbackWeapon()
        {
            SurvivorWeaponController controller = FindFirstObjectByType<SurvivorWeaponController>();
            if (controller != null)
            {
                controller.UnlockWeapon(startPool[Random.Range(0, startPool.Length)]);
            }
        }

        private void Hide(bool resumeTime)
        {
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
            rect.sizeDelta = new Vector2(260f, 80f);

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

        private Sprite GetIcon(SurvivorWeaponKind kind)
        {
            switch (kind)
            {
                case SurvivorWeaponKind.TechnoBlade:
                    return technoBladeIcon;
                case SurvivorWeaponKind.PoisonBottle:
                    return poisonBottleIcon;
                case SurvivorWeaponKind.FlameBottle:
                    return flameBottleIcon;
                case SurvivorWeaponKind.Fireball:
                    return fireballIcon;
                case SurvivorWeaponKind.IceSpike:
                    return iceSpikeIcon;
                case SurvivorWeaponKind.AxeBoomerang:
                    return axeBoomerangIcon;
                default:
                    return null;
            }
        }

        private static string GetTitle(SurvivorWeaponKind kind)
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
                case SurvivorWeaponKind.AxeBoomerang:
                    return "Axe Boomerang";
                default:
                    return kind.ToString();
            }
        }

        private static string GetDescription(SurvivorWeaponKind kind)
        {
            switch (kind)
            {
                case SurvivorWeaponKind.TechnoBlade:
                    return "Continuously orbits you and damages enemies.";
                case SurvivorWeaponKind.PoisonBottle:
                    return "Throws an arcing poison bottle. Poison can spread when enemies die.";
                case SurvivorWeaponKind.FlameBottle:
                    return "Throws an arcing flame bottle. Burning enemies can explode on death.";
                case SurvivorWeaponKind.Fireball:
                    return "Shoots fireballs toward nearby enemies. Upgrades add more fireballs.";
                case SurvivorWeaponKind.IceSpike:
                    return "Leaves delayed ice spikes behind your movement path and slows enemies.";
                case SurvivorWeaponKind.AxeBoomerang:
                    return "Throws an axe in an arc, then returns it back to you.";
                default:
                    return string.Empty;
            }
        }
    }
}
