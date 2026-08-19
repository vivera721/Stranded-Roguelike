using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StrandedRoguelike
{
    public sealed class UpgradeCardUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;

        [SerializeField] private Button button;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;

        private UpgradePanelController owner;
        private UpgradeDefinition currentUpgrade;

        private void Awake()
        {
            AutoBind();
        }

        public void Setup(UpgradePanelController newOwner, UpgradeDefinition upgrade)
        {
            owner = newOwner;
            currentUpgrade = upgrade;
            AutoBind();
            if (iconImage != null && owner != null)
            {
                Sprite icon = owner.GetIcon(upgrade.Id);
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (titleText != null)
            {
                titleText.text = upgrade.Title;
            }

            if (descriptionText != null)
            {
                descriptionText.text = upgrade.Description;
            }

            if (button != null)
            {
                button.onClick.RemoveListener(Select);
                button.onClick.AddListener(Select);
            }

            gameObject.SetActive(true);
        }

        private void Select()
        {
            owner?.SelectUpgrade(currentUpgrade);
        }

        private void AutoBind()
        {
            if (iconImage == null)
            {
                iconImage = FindIconImage();
            }

            if (button == null)
            {
                button = GetComponent<Button>();
            }

            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            if (titleText == null && texts.Length > 0)
            {
                titleText = texts[0];
            }

            if (descriptionText == null && texts.Length > 1)
            {
                descriptionText = texts[1];
            }
            else if (descriptionText == null && texts.Length > 0)
            {
                descriptionText = texts[0];
            }
        }

        private Image FindIconImage()
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            Image rootImage = GetComponent<Image>();

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
