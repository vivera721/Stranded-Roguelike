using UnityEngine;

namespace StrandedRoguelike
{
    public sealed class SurvivorBossHealthBar : MonoBehaviour
    {
        [SerializeField] private EnemyHealth health;
        [SerializeField, Min(0.5f)] private float width = 3.2f;
        [SerializeField, Min(0.05f)] private float height = 0.18f;
        [SerializeField, Min(0f)] private float verticalPadding = 0.35f;
        [SerializeField] private Color fillColor = new Color(0.85f, 0.08f, 0.12f, 1f);
        [SerializeField] private Color backgroundColor = new Color(0.08f, 0.03f, 0.04f, 0.95f);
        [SerializeField] private Color borderColor = new Color(1f, 0.9f, 0.75f, 1f);

        private static Sprite whiteSprite;

        private Transform barRoot;
        private SpriteRenderer fillRenderer;
        private bool built;

        public void Configure(EnemyHealth newHealth)
        {
            health = newHealth;
            BuildIfNeeded();
            Refresh();
        }

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<EnemyHealth>();
            }

            BuildIfNeeded();
        }

        private void LateUpdate()
        {
            Refresh();
        }

        private void BuildIfNeeded()
        {
            if (built || health == null)
            {
                return;
            }

            built = true;
            float localTop = FindLocalVisualTop();
            int sortingLayerId = 0;
            int sortingOrder = 100;
            FindSorting(out sortingLayerId, out sortingOrder);

            GameObject rootObject = new GameObject("Boss HP Bar");
            barRoot = rootObject.transform;
            barRoot.SetParent(transform, false);
            barRoot.localPosition = new Vector3(0f, localTop + verticalPadding, 0f);

            SpriteRenderer border = CreateBarPart("Border", borderColor, sortingLayerId, sortingOrder + 2);
            border.transform.localScale = new Vector3(width + 0.12f, height + 0.12f, 1f);

            SpriteRenderer background = CreateBarPart("Background", backgroundColor, sortingLayerId, sortingOrder + 3);
            background.transform.localScale = new Vector3(width, height, 1f);

            fillRenderer = CreateBarPart("Fill", fillColor, sortingLayerId, sortingOrder + 4);
        }

        private SpriteRenderer CreateBarPart(string objectName, Color color, int sortingLayerId, int sortingOrder)
        {
            GameObject partObject = new GameObject(objectName);
            partObject.transform.SetParent(barRoot, false);

            SpriteRenderer renderer = partObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GetWhiteSprite();
            renderer.color = color;
            renderer.sortingLayerID = sortingLayerId;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void Refresh()
        {
            if (!built || health == null || fillRenderer == null)
            {
                return;
            }

            float ratio = Mathf.Clamp01(health.HealthRatio);
            float fillWidth = width * ratio;
            fillRenderer.transform.localScale = new Vector3(fillWidth, height, 1f);
            fillRenderer.transform.localPosition = new Vector3(-width * 0.5f + fillWidth * 0.5f, 0f, 0f);
        }

        private float FindLocalVisualTop()
        {
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            bool hasBounds = false;
            Bounds bounds = default;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderers[i].bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            if (!hasBounds)
            {
                return 1.5f;
            }

            return transform.InverseTransformPoint(new Vector3(transform.position.x, bounds.max.y, transform.position.z)).y;
        }

        private void FindSorting(out int sortingLayerId, out int sortingOrder)
        {
            sortingLayerId = 0;
            sortingOrder = 100;

            SpriteRenderer rootRenderer = GetComponent<SpriteRenderer>();
            if (rootRenderer == null)
            {
                return;
            }

            sortingLayerId = rootRenderer.sortingLayerID;
            sortingOrder = rootRenderer.sortingOrder + 100;
        }

        private static Sprite GetWhiteSprite()
        {
            if (whiteSprite != null)
            {
                return whiteSprite;
            }

            Texture2D texture = new Texture2D(1, 1);
            texture.name = "Runtime Boss HP Bar";
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return whiteSprite;
        }
    }
}
