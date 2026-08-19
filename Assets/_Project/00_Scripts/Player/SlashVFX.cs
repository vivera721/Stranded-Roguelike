using System.Collections;
using UnityEngine;

namespace StrandedRoguelike
{
    public sealed class SlashVFX : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float duration = 0.2f;
        [SerializeField, Min(0.01f)] private float radius = 0.65f;
        [SerializeField, Min(0.01f)] private float distance = 0.45f;
        [SerializeField, Range(3, 24)] private int segments = 10;
        [SerializeField] private Color innerColor = new Color(1f, 1f, 0.85f, 1f);
        [SerializeField] private Color outerColor = new Color(1f, 0.25f, 0.05f, 0f);

        private Transform effectTransform;
        private LineRenderer lineRenderer;
        private Coroutine playRoutine;

        private void Awake()
        {
            CreateRenderer();
        }

        public void Play(Vector2 direction)
        {
            if (direction == Vector2.zero)
            {
                direction = Vector2.down;
            }

            CreateRenderer();

            if (playRoutine != null)
            {
                StopCoroutine(playRoutine);
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            effectTransform.localPosition = direction.normalized * distance;
            effectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            playRoutine = StartCoroutine(PlayRoutine());
        }

        private void CreateRenderer()
        {
            if (lineRenderer != null)
            {
                return;
            }

            GameObject effect = new GameObject("Slash VFX");
            effectTransform = effect.transform;
            effectTransform.SetParent(transform, false);

            lineRenderer = effect.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = false;
            lineRenderer.positionCount = segments;
            lineRenderer.numCapVertices = 2;
            lineRenderer.numCornerVertices = 2;
            lineRenderer.sortingOrder = 20;
            lineRenderer.textureMode = LineTextureMode.Stretch;
            lineRenderer.enabled = false;
            lineRenderer.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.25f, 1f),
                new Keyframe(0.75f, 1f),
                new Keyframe(1f, 0f));

            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            lineRenderer.material = new Material(shader);
            BuildArc();
        }

        private void BuildArc()
        {
            lineRenderer.positionCount = segments;

            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)(segments - 1);
                float angle = Mathf.Lerp(-65f, 65f, t) * Mathf.Deg2Rad;
                lineRenderer.SetPosition(
                    i,
                    new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
            }
        }

        private IEnumerator PlayRoutine()
        {
            lineRenderer.enabled = true;

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = 1f - t;
                float scale = Mathf.Lerp(0.65f, 1.25f, t);

                effectTransform.localScale = Vector3.one * scale;
                lineRenderer.startWidth = Mathf.Lerp(0.18f, 0.035f, t);
                lineRenderer.endWidth = Mathf.Lerp(0.07f, 0.01f, t);

                Color start = innerColor;
                Color end = outerColor;
                start.a *= alpha;
                end.a *= alpha;
                lineRenderer.startColor = start;
                lineRenderer.endColor = end;
                yield return null;
            }

            lineRenderer.enabled = false;
            playRoutine = null;
        }
    }
}
