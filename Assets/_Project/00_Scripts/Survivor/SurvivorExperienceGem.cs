using UnityEngine;

namespace StrandedRoguelike
{
    public sealed class SurvivorExperienceGem : MonoBehaviour
    {
        private static Sprite fallbackSprite;

        [SerializeField, Min(1)] private int experienceAmount = 1;
        [SerializeField, Min(0.05f)] private float collectDistance = 0.45f;
        [SerializeField, Min(0.1f)] private float magnetRadius = 3.2f;
        [SerializeField, Min(0.1f)] private float magnetSpeed = 8f;
        [SerializeField, Min(0.05f)] private float visualScale = 0.25f;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Transform player;
        private SurvivorExperience playerExperience;
        private bool collected;

        public static SurvivorExperienceGem Spawn(Vector2 position, int amount, Sprite sprite = null)
        {
            GameObject gemObject = new GameObject("Experience Gem");
            gemObject.transform.position = position;
            gemObject.transform.localScale = Vector3.one * 0.25f;

            SpriteRenderer renderer = gemObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite != null ? sprite : GetFallbackSprite();
            renderer.color = sprite != null ? Color.white : new Color(0.25f, 0.8f, 1f, 1f);
            renderer.sortingOrder = 12;

            SurvivorExperienceGem gem = gemObject.AddComponent<SurvivorExperienceGem>();
            gem.spriteRenderer = renderer;
            gem.Initialize(amount);
            return gem;
        }

        public void Initialize(int amount)
        {
            experienceAmount = Mathf.Max(1, amount);
            FindPlayer();
        }

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null && spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = GetFallbackSprite();
                spriteRenderer.color = new Color(0.25f, 0.8f, 1f, 1f);
            }

            transform.localScale = Vector3.one * visualScale;

            FindPlayer();
        }

        private void Update()
        {
            if (collected)
            {
                return;
            }

            if (player == null || playerExperience == null)
            {
                FindPlayer();
                return;
            }

            Vector2 toPlayer = player.position - transform.position;
            float distance = toPlayer.magnitude;

            if (distance <= collectDistance)
            {
                Collect();
                return;
            }

            if (distance <= magnetRadius)
            {
                transform.position = Vector2.MoveTowards(transform.position, player.position, magnetSpeed * Time.deltaTime);
            }
        }

        private void Collect()
        {
            if (collected || playerExperience == null)
            {
                return;
            }

            collected = true;
            int amount = experienceAmount;
            Destroy(gameObject);
            playerExperience.AddExperience(amount);
        }

        public static int CollectAllInRadius(Vector2 center, float radius)
        {
            if (radius <= 0f)
            {
                return 0;
            }

            SurvivorExperienceGem[] gems = FindObjectsByType<SurvivorExperienceGem>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            float radiusSquared = radius * radius;
            int totalExperience = 0;

            for (int i = 0; i < gems.Length; i++)
            {
                SurvivorExperienceGem gem = gems[i];
                if (gem == null || gem.collected)
                {
                    continue;
                }

                float distanceSquared = ((Vector2)gem.transform.position - center).sqrMagnitude;
                if (distanceSquared > radiusSquared)
                {
                    continue;
                }

                gem.collected = true;
                totalExperience += gem.experienceAmount;
                Destroy(gem.gameObject);
            }

            return totalExperience;
        }

        private void FindPlayer()
        {
            if (playerExperience == null)
            {
                playerExperience = FindFirstObjectByType<SurvivorExperience>();
            }

            if (playerExperience != null)
            {
                player = playerExperience.transform;
            }
        }

        private static Sprite GetFallbackSprite()
        {
            if (fallbackSprite != null)
            {
                return fallbackSprite;
            }

            Texture2D texture = new Texture2D(16, 16);
            texture.filterMode = FilterMode.Point;
            Vector2 center = new Vector2(7.5f, 7.5f);

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    texture.SetPixel(x, y, distance <= 6f ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
            return fallbackSprite;
        }
    }
}
