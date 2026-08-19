using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StrandedRoguelike
{
    public sealed class SurvivorExperience : MonoBehaviour
    {
        [Header("Level")]
        [SerializeField, Min(1)] private int level = 1;
        [SerializeField, Min(0)] private int currentExperience;
        [SerializeField, Min(1)] private int maxLevel = 15;

        [Tooltip("현재 레벨에서 다음 레벨로 가기 위해 필요한 경험치입니다. Index 0 = Level 1 필요 경험치.")]
        [SerializeField] private int[] experienceRequirementsByLevel =
        {
            10, 15, 25, 40, 65,
            105, 170, 275, 445, 720,
            1165, 1885, 3050, 4935, 7985
        };

        [Header("UI")]
        [Tooltip("Canvas의 exp_bar Image를 넣으면 경험치가 오를 때 fillAmount로 표시됩니다.")]
        [SerializeField] private Image expBar;
        [SerializeField] private bool autoFindExpBar = true;
        [SerializeField] private bool forceExpBarFilledType = true;
        [SerializeField] private string expBarObjectName = "exp_bar";

        [Header("Level Text")]
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private bool autoFindLevelText = true;
        [SerializeField] private string levelTextObjectName = "LevelText";
        [SerializeField] private string levelTextFormat = "Lv. {0}";

        [Header("Level Up Gem Collection")]
        [SerializeField, Min(1)] private int gemCollectionStartLevel = 5;
        [SerializeField, Min(0f)] private float levelUpGemCollectionRadius = 20f;

        public event Action<int, int, int> ExperienceChanged;
        public event Action<int> LeveledUp;

        public int Level => level;
        public int CurrentExperience => currentExperience;
        public int MaxLevel => Mathf.Max(1, maxLevel);
        public bool IsMaxLevel => level >= MaxLevel;
        public int ExperienceToNextLevel => GetExperienceToNextLevel(level);
        public float ExperienceRatio
        {
            get
            {
                if (IsMaxLevel)
                {
                    return 1f;
                }

                int requiredExperience = ExperienceToNextLevel;
                return requiredExperience <= 0 ? 1f : Mathf.Clamp01(currentExperience / (float)requiredExperience);
            }
        }

        private void Awake()
        {
            AutoBindExpBar();
            AutoBindLevelText();
        }

        private void Start()
        {
            RaiseExperienceChanged();
        }

        private void OnValidate()
        {
            level = Mathf.Clamp(level, 1, Mathf.Max(1, maxLevel));
            currentExperience = Mathf.Max(0, currentExperience);
            SanitizeRequirements();
            UpdateExpBar();
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0 || IsMaxLevel)
            {
                RaiseExperienceChanged();
                return;
            }

            currentExperience += amount;
            bool shouldCollectNearbyGems = ProcessLevelUps();

            if (shouldCollectNearbyGems && !IsMaxLevel)
            {
                int collectedExperience = SurvivorExperienceGem.CollectAllInRadius(
                    transform.position,
                    levelUpGemCollectionRadius);

                if (collectedExperience > 0)
                {
                    currentExperience += collectedExperience;
                    ProcessLevelUps();
                }
            }

            if (IsMaxLevel)
            {
                currentExperience = 0;
            }

            RaiseExperienceChanged();
        }

        private bool ProcessLevelUps()
        {
            bool shouldCollectNearbyGems = false;

            while (!IsMaxLevel)
            {
                int requiredExperience = ExperienceToNextLevel;
                if (requiredExperience <= 0 || currentExperience < requiredExperience)
                {
                    break;
                }

                currentExperience -= requiredExperience;
                level++;
                LeveledUp?.Invoke(level);

                if (level >= Mathf.Max(1, gemCollectionStartLevel))
                {
                    shouldCollectNearbyGems = true;
                }
            }

            return shouldCollectNearbyGems;
        }

        public void SetExpBar(Image newExpBar)
        {
            expBar = newExpBar;
            UpdateExpBar();
        }

        public void SetLevelText(TMP_Text newLevelText)
        {
            levelText = newLevelText;
            UpdateLevelText();
        }

        public int GetExperienceToNextLevel(int targetLevel)
        {
            if (targetLevel >= MaxLevel)
            {
                return 0;
            }

            SanitizeRequirements();

            if (experienceRequirementsByLevel == null || experienceRequirementsByLevel.Length == 0)
            {
                return 1;
            }

            int index = Mathf.Clamp(targetLevel - 1, 0, experienceRequirementsByLevel.Length - 1);
            return Mathf.Max(1, experienceRequirementsByLevel[index]);
        }

        [ContextMenu("Reset Experience Progress")]
        private void ResetExperienceProgress()
        {
            level = 1;
            currentExperience = 0;
            RaiseExperienceChanged();
        }

        [ContextMenu("Auto Fill Fibonacci Requirements")]
        private void AutoFillFibonacciRequirements()
        {
            int count = Mathf.Max(1, maxLevel);
            experienceRequirementsByLevel = new int[count];
            experienceRequirementsByLevel[0] = 10;

            if (count > 1)
            {
                experienceRequirementsByLevel[1] = 15;
            }

            for (int i = 2; i < count; i++)
            {
                experienceRequirementsByLevel[i] = experienceRequirementsByLevel[i - 1] + experienceRequirementsByLevel[i - 2];
            }

            RaiseExperienceChanged();
        }

        private void RaiseExperienceChanged()
        {
            UpdateExpBar();
            UpdateLevelText();
            ExperienceChanged?.Invoke(level, currentExperience, ExperienceToNextLevel);
        }

        private void UpdateExpBar()
        {
            if (expBar == null)
            {
                AutoBindExpBar();
            }

            if (expBar != null)
            {
                ConfigureExpBarImage(expBar);
                expBar.fillAmount = ExperienceRatio;
            }
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

        private void AutoBindExpBar()
        {
            if (!autoFindExpBar || expBar != null || string.IsNullOrWhiteSpace(expBarObjectName))
            {
                return;
            }

            Image[] images = FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null && images[i].gameObject.name == expBarObjectName)
                {
                    expBar = images[i];
                    return;
                }
            }

            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null && images[i].transform.parent != null && images[i].transform.parent.name == expBarObjectName)
                {
                    expBar = images[i];
                    return;
                }
            }
        }

        private void UpdateLevelText()
        {
            if (levelText == null)
            {
                AutoBindLevelText();
            }

            if (levelText == null)
            {
                return;
            }

            string format = string.IsNullOrWhiteSpace(levelTextFormat) ? "Lv. {0}" : levelTextFormat;
            levelText.text = string.Format(format, level);
        }

        private void AutoBindLevelText()
        {
            if (!autoFindLevelText || levelText != null || string.IsNullOrWhiteSpace(levelTextObjectName))
            {
                return;
            }

            TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            TMP_Text nameMatch = null;

            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || text.gameObject.name != levelTextObjectName)
                {
                    continue;
                }

                if (text.transform.parent != null && text.transform.parent.name == "LevelPatch")
                {
                    levelText = text;
                    return;
                }

                nameMatch = text;
            }

            levelText = nameMatch;
        }

        private void SanitizeRequirements()
        {
            if (experienceRequirementsByLevel == null || experienceRequirementsByLevel.Length == 0)
            {
                experienceRequirementsByLevel = new[] { 10 };
            }

            for (int i = 0; i < experienceRequirementsByLevel.Length; i++)
            {
                experienceRequirementsByLevel[i] = Mathf.Max(1, experienceRequirementsByLevel[i]);
            }
        }
    }
}
