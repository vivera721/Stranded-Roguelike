using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StrandedRoguelike
{
    public sealed class SurvivorExperienceUI : MonoBehaviour
    {
        [SerializeField] private SurvivorExperience experience;

        [Header("Bar")]
        [SerializeField] private Image expFillImage;
        [SerializeField] private Slider experienceSlider;
        [SerializeField] private bool forceExpBarFilledType = true;
        [SerializeField] private string expBarObjectName = "exp_bar";

        [Header("Text")]
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text experienceText;

        private void Awake()
        {
            AutoBind();
        }

        private void OnEnable()
        {
            AutoBind();

            if (experience != null)
            {
                experience.ExperienceChanged += Refresh;
                Refresh(experience.Level, experience.CurrentExperience, experience.ExperienceToNextLevel);
            }
        }

        private void OnDisable()
        {
            if (experience != null)
            {
                experience.ExperienceChanged -= Refresh;
            }
        }

        private void Refresh(int level, int currentExperience, int experienceToNextLevel)
        {
            float ratio = experienceToNextLevel <= 0 ? 1f : Mathf.Clamp01(currentExperience / (float)experienceToNextLevel);

            if (expFillImage != null)
            {
                ConfigureExpBarImage(expFillImage);
                expFillImage.fillAmount = ratio;
            }

            if (experienceSlider != null)
            {
                experienceSlider.minValue = 0f;
                experienceSlider.maxValue = Mathf.Max(1, experienceToNextLevel);
                experienceSlider.value = Mathf.Clamp(currentExperience, 0, Mathf.Max(1, experienceToNextLevel));
            }

            if (levelText != null)
            {
                levelText.text = $"Lv. {level}";
            }

            if (experienceText != null)
            {
                experienceText.text = experienceToNextLevel <= 0
                    ? "MAX"
                    : $"{currentExperience} / {experienceToNextLevel}";
            }
        }

        private void AutoBind()
        {
            if (experience == null)
            {
                experience = FindFirstObjectByType<SurvivorExperience>();
            }

            if (expFillImage == null)
            {
                expFillImage = FindExpFillImage();
            }

            if (experience != null && expFillImage != null)
            {
                experience.SetExpBar(expFillImage);
            }

            if (experienceSlider == null)
            {
                experienceSlider = GetComponentInChildren<Slider>(true);
            }

            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            if (levelText == null && texts.Length > 0)
            {
                levelText = texts[0];
            }

            if (experienceText == null && texts.Length > 1)
            {
                experienceText = texts[1];
            }
        }

        private Image FindExpFillImage()
        {
            Image[] childImages = GetComponentsInChildren<Image>(true);
            Image image = FindImageByName(childImages, expBarObjectName);
            if (image != null)
            {
                return image;
            }

            Image[] sceneImages = FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            image = FindImageByName(sceneImages, expBarObjectName);
            if (image != null)
            {
                return image;
            }

            for (int i = 0; i < sceneImages.Length; i++)
            {
                if (sceneImages[i] != null && sceneImages[i].transform.parent != null && sceneImages[i].transform.parent.name == expBarObjectName)
                {
                    return sceneImages[i];
                }
            }

            return null;
        }

        private void ConfigureExpBarImage(Image target)
        {
            if (!forceExpBarFilledType || target == null)
            {
                return;
            }

            target.type = Image.Type.Filled;
            target.fillMethod = Image.FillMethod.Horizontal;
            target.fillOrigin = 0;
        }

        private static Image FindImageByName(Image[] images, string imageName)
        {
            if (string.IsNullOrWhiteSpace(imageName))
            {
                return null;
            }

            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null && images[i].gameObject.name == imageName)
                {
                    return images[i];
                }
            }

            return null;
        }
    }
}
