using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum UpgradeType
{
    AxeBoomerang,
    ChainLightning,
    Fireball,
    FlameBottle,
    IceSpikes,
    LightningBolt,
    Missile,
    PoisonBottle,
    TechnoBlade,
    Hp
}

[Serializable]
public class UpgradeData
{
    public UpgradeType type;
    public string title;

    [TextArea]
    public string description;

    public Sprite icon;
}

public class UpgradePanelUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button selectButton;
    [SerializeField] private bool setIconNativeSize = true;
    [SerializeField] private bool rotateMissileIcon = true;

    private UpgradeUI upgradeUI;
    private UpgradeData currentUpgrade;

    private void Awake()
    {
        TryAutoBind();
    }

    public void Setup(UpgradeUI owner, UpgradeData upgradeData)
    {
        TryAutoBind();

        upgradeUI = owner;
        currentUpgrade = upgradeData;

        if (upgradeData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = upgradeData.icon;
            iconImage.enabled = upgradeData.icon != null;
            iconImage.preserveAspect = true;

            if (upgradeData.icon != null && setIconNativeSize)
            {
                iconImage.SetNativeSize();
            }

            iconImage.rectTransform.localRotation = rotateMissileIcon && upgradeData.type == UpgradeType.Missile
                ? Quaternion.Euler(0f, 0f, 90f)
                : Quaternion.identity;
        }

        if (titleText != null)
        {
            titleText.text = upgradeData.title;
        }

        if (descriptionText != null)
        {
            descriptionText.text = upgradeData.description;
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(SelectUpgrade);
        }

        gameObject.SetActive(true);
    }

    public void Clear()
    {
        currentUpgrade = null;
        gameObject.SetActive(false);
    }

    private void SelectUpgrade()
    {
        if (upgradeUI == null || currentUpgrade == null)
        {
            return;
        }

        upgradeUI.SelectUpgrade(currentUpgrade);
    }

    private void AutoBind()
    {
        if (selectButton == null)
        {
            selectButton = GetComponent<Button>();
        }

        if (selectButton == null)
        {
            selectButton = GetComponentInChildren<Button>(true);
        }

        if (iconImage == null)
        {
            iconImage = FindIconImage();
        }

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        if (titleText == null)
        {
            titleText = FindTextByName(texts, "title", "name", "label");
        }

        if (descriptionText == null)
        {
            descriptionText = FindTextByName(texts, "description", "desc", "explain", "info");
        }

        if (titleText == null && texts.Length > 0)
        {
            titleText = texts[0];
        }

        if (descriptionText == null)
        {
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i] != titleText)
                {
                    descriptionText = texts[i];
                    break;
                }
            }
        }
    }

    private void TryAutoBind()
    {
        try
        {
            AutoBind();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"{nameof(UpgradePanelUI)} failed to auto bind UI references on {gameObject.name}. Please assign Icon, Title, Description, and Button manually if this warning keeps appearing.\n{exception}", this);
        }
    }

    private Image FindIconImage()
    {
        Image rootImage = GetComponent<Image>();
        Image[] images = GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null || images[i] == rootImage)
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
            if (images[i] != null && images[i] != rootImage)
            {
                return images[i];
            }
        }

        return null;
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
}
