using System.Collections;
using UnityEngine;

namespace StrandedRoguelike
{
    public sealed class HitFlash : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private SpriteRenderer[] spriteRenderers;

        [Header("Flash")]
        [SerializeField] private Material flashMat;
        [SerializeField, Min(0.01f)] private float flashDuration = 0.08f;
        [SerializeField, Min(1)] private int flashCount = 1;

        private Material[] originalMaterials;
        private Coroutine flashCoroutine;

        private void Awake()
        {
            if (spriteRenderers == null || spriteRenderers.Length == 0)
            {
                spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
            }

            originalMaterials = new Material[spriteRenderers.Length];

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    originalMaterials[i] = spriteRenderers[i].sharedMaterial;
                }
            }
        }

        public void Play()
        {
            if (spriteRenderers == null || spriteRenderers.Length == 0)
                return;

            if (flashMat == null)
                return;

            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }

            flashCoroutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            for (int i = 0; i < flashCount; i++)
            {
                SetFlashMaterial();
                yield return new WaitForSeconds(flashDuration);

                RestoreMaterial();
                yield return new WaitForSeconds(flashDuration);
            }

            RestoreMaterial();
            flashCoroutine = null;
        }

        private void SetFlashMaterial()
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].sharedMaterial = flashMat;
                }
            }
        }

        private void RestoreMaterial()
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].sharedMaterial = originalMaterials[i];
                }
            }
        }
    }
}