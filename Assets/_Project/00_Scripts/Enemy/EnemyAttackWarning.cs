using System.Collections;
using UnityEngine;

namespace StrandedRoguelike
{
    public sealed class EnemyAttackWarning : MonoBehaviour
    {
        private enum WarningShape
        {
            Box,
            Circle
        }

        private WarningShape shape;

        [SerializeField] private Color warningColor = new Color(1f, 0.15f, 0.05f, 0.8f);
        [SerializeField, Min(0.01f)] private float lineWidth = 0.04f;

        private LineRenderer lineRenderer;
        private SpriteRenderer spriteRenderer;
        private Sprite warningSprite;

        public static EnemyAttackWarning ShowCircle(Vector2 position, float radius, float duration)
        {
            return ShowCircle(position, radius, duration, null);
        }

        public static EnemyAttackWarning ShowCircle(Vector2 position, float radius, float duration, Sprite sprite)
        {
            EnemyAttackWarning warning = Create("Enemy Circle Attack Warning", sprite);
            warning.shape = WarningShape.Circle;
            warning.CreateRenderer();
            warning.DrawCircle(position, radius);
            warning.StartCoroutine(warning.HideAfter(duration));
            return warning;
        }

        public static EnemyAttackWarning ShowBox(Vector2 position, Vector2 size, float angle, float duration)
        {
            return ShowBox(position, size, angle, duration, null);
        }

        public static EnemyAttackWarning ShowBox(Vector2 position, Vector2 size, float angle, float duration, Sprite sprite)
        {
            EnemyAttackWarning warning = Create("Enemy Box Attack Warning", sprite);
            warning.shape = WarningShape.Box;
            warning.CreateRenderer();
            warning.DrawBox(position, size, angle);
            warning.StartCoroutine(warning.HideAfter(duration)); 
            return warning;
        }

        private static EnemyAttackWarning Create(string objectName, Sprite sprite)
        {
            GameObject warningObject = new GameObject(objectName);
            EnemyAttackWarning warning = warningObject.AddComponent<EnemyAttackWarning>();
            warning.warningSprite = sprite;
            return warning;
        }

        private void CreateRenderer()
        {
            if (warningSprite != null)
            {
                CreateSpriteRenderer();
                return;
            }

            CreateLineRenderer();
        }

        private void CreateSpriteRenderer()
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = warningSprite;
            spriteRenderer.color = warningColor;
            spriteRenderer.sortingOrder = 50;
            if (shape == WarningShape.Box)
            {
                spriteRenderer.drawMode = SpriteDrawMode.Tiled;
            }
            else
            {
                spriteRenderer.drawMode = SpriteDrawMode.Simple;
            }
        }

        private void CreateLineRenderer()
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = true;
            lineRenderer.numCapVertices = 2;
            lineRenderer.numCornerVertices = 2;
            lineRenderer.sortingOrder = 50;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.startColor = warningColor;
            lineRenderer.endColor = warningColor;

            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader != null)
            {
                lineRenderer.material = new Material(shader);
            }
        }

        private void DrawCircle(Vector2 position, float radius)
        {
            if (spriteRenderer != null)
            {
                transform.position = position;
                transform.rotation = Quaternion.identity;

                float diameter = radius * 2f;

                Vector2 spriteSize = warningSprite.bounds.size;

                if (spriteSize.x <= 0f || spriteSize.y <= 0f)
                {
                    transform.localScale = Vector3.one;
                    return;
                }

                transform.localScale = new Vector3(
                    diameter / spriteSize.x,
                    diameter / spriteSize.y,
                    1f);
                spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f);

                return;
            }

            const int segments = 48;
            lineRenderer.positionCount = segments;

            for (int i = 0; i < segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                Vector3 point = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
                lineRenderer.SetPosition(i, (Vector3)position + point);
            }
        }

        private void DrawBox(Vector2 position, Vector2 size, float angle)
        {
            if (spriteRenderer != null)
            {
                DrawSprite(position, size, angle);
                spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f);
                return;
            }

            lineRenderer.positionCount = 4;

            Vector2 half = size * 0.5f;
            Vector2[] corners =
            {
                new Vector2(-half.x, -half.y),
                new Vector2(-half.x, half.y),
                new Vector2(half.x, half.y),
                new Vector2(half.x, -half.y)
            };

            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

            for (int i = 0; i < corners.Length; i++)
            {
                lineRenderer.SetPosition(i, (Vector3)position + rotation * corners[i]);
            }
        }

        private void DrawSprite(Vector2 position, Vector2 size, float angle)
        {
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            transform.localScale = Vector3.one;

            if (spriteRenderer == null || warningSprite == null)
            {
                return;
            }

            spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f);
            spriteRenderer.size = size;

            //if (warningSprite == null)
            //{
            //    return;
            //}

            //Vector2 spriteSize = warningSprite.bounds.size;

            //if (spriteSize.x <= 0f || spriteSize.y <= 0f)
            //{
            //    transform.localScale = Vector3.one;
            //    return;
            //}

            //transform.localScale = new Vector3(
            //    size.x / spriteSize.x,
            //    size.y / spriteSize.y,
            //    1f);
        }

        private IEnumerator HideAfter(float duration)
        {
            yield return new WaitForSeconds(duration);
            Destroy(gameObject);
        }
    }
}
