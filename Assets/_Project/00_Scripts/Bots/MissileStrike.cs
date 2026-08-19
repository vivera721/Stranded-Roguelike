using System;
using System.Collections;
using UnityEngine;

namespace StrandedRoguelike
{
    public sealed class MissileStrike : MonoBehaviour
    {
        private Vector2 targetPosition;
        private float delay;
        private float fallDuration;
        private float startHeight;
        private float radius;
        private int damage;
        private Sprite missileSprite;
        private GameObject impactVFXPrefab;
        private bool useShadow;
        private GameObject markerObject;
        private GameObject missileObject;
        private GameObject shadowObject;
        private float impactVFXLifetime = 1.6f;
        private Action<Vector2> impactCallback;

        public static void Spawn(Vector2 targetPosition, float delay, float fallDuration, float startHeight, float radius, int damage, Sprite missileSprite, GameObject impactVFXPrefab, bool useShadow = true, Action<Vector2> impactCallback = null)
        {
            GameObject strikeObject = new GameObject("Missile Strike");
            MissileStrike strike = strikeObject.AddComponent<MissileStrike>();
            strike.Initialize(targetPosition, delay, fallDuration, startHeight, radius, damage, missileSprite, impactVFXPrefab, useShadow, impactCallback);
        }

        private void Initialize(Vector2 newTargetPosition, float newDelay, float newFallDuration, float newStartHeight, float newRadius, int newDamage, Sprite newMissileSprite, GameObject newImpactVFXPrefab, bool newUseShadow, Action<Vector2> newImpactCallback)
        {
            targetPosition = newTargetPosition;
            delay = newDelay;
            fallDuration = newFallDuration;
            startHeight = newStartHeight;
            radius = newRadius;
            damage = newDamage;
            missileSprite = newMissileSprite;
            impactVFXPrefab = newImpactVFXPrefab;
            useShadow = newUseShadow;
            impactCallback = newImpactCallback;
            StartCoroutine(StrikeRoutine());
        }

        private IEnumerator StrikeRoutine()
        {
            markerObject = CreateMarker();
            yield return new WaitForSeconds(delay);

            missileObject = CreateMissile();
            shadowObject = useShadow ? CreateShadow() : null;
            Vector2 start = targetPosition + Vector2.up * startHeight;
            float elapsed = 0f;

            while (elapsed < fallDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, fallDuration));
                if (missileObject != null)
                {
                    missileObject.transform.position = Vector2.Lerp(start, targetPosition, t);
                }

                if (shadowObject != null)
                {
                    UpdateShadow(shadowObject, t);
                }

                yield return null;
            }

            CleanupVisualObjects();
            Impact();
            Destroy(gameObject);
        }

        private GameObject CreateMarker()
        {
            GameObject marker = new GameObject("Missile Target Marker");
            marker.transform.position = targetPosition;

            SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateCircleSprite(48, true);
            renderer.color = new Color(1f, 0.15f, 0.05f, 0.35f);
            renderer.sortingOrder = 5;
            marker.transform.localScale = Vector3.one * radius * 2f;

            return marker;
        }

        private GameObject CreateMissile()
        {
            GameObject missile = new GameObject("Falling Missile");
            SpriteRenderer renderer = missile.AddComponent<SpriteRenderer>();
            renderer.sprite = missileSprite != null ? missileSprite : CreateCircleSprite(24, false);
            renderer.color = missileSprite != null ? Color.white : new Color(1f, 0.75f, 0.2f, 1f);
            renderer.sortingOrder = 40;
            missile.transform.localScale = Vector3.one * 0.9f;

            return missile;
        }

        private GameObject CreateShadow()
        {
            GameObject shadow = new GameObject("Missile Fake Shadow");
            shadow.transform.position = targetPosition;

            SpriteRenderer renderer = shadow.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateCircleSprite(24, false);
            renderer.color = new Color(0f, 0f, 0f, 0.15f);
            renderer.sortingOrder = 4;
            shadow.transform.localScale = Vector3.one * radius * 0.45f;

            return shadow;
        }

        private void UpdateShadow(GameObject shadow, float fallRatio)
        {
            float scale = Mathf.Lerp(0.45f, 1f, fallRatio);
            float alpha = Mathf.Lerp(0.15f, 0.38f, fallRatio);

            shadow.transform.position = targetPosition;
            shadow.transform.localScale = Vector3.one * radius * scale;

            SpriteRenderer renderer = shadow.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = new Color(0f, 0f, 0f, alpha);
            }
        }

        private void Impact()
        {
            if (impactVFXPrefab != null)
            {
                GameObject impactVFX = Instantiate(impactVFXPrefab, targetPosition, Quaternion.identity);
                RuntimeObjectLifetime.Attach(impactVFX, impactVFXLifetime);
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(targetPosition, radius);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].TryGetComponent(out EnemyHealth enemyHealth) && !enemyHealth.isDead)
                {
                    enemyHealth.TakeDamage(damage, targetPosition);
                }
            }

            impactCallback?.Invoke(targetPosition);
        }

        private void OnDestroy()
        {
            CleanupVisualObjects();
        }

        private void CleanupVisualObjects()
        {
            if (markerObject != null)
            {
                Destroy(markerObject);
                markerObject = null;
            }

            if (missileObject != null)
            {
                Destroy(missileObject);
                missileObject = null;
            }

            if (shadowObject != null)
            {
                Destroy(shadowObject);
                shadowObject = null;
            }
        }

        private static Sprite CreateCircleSprite(int size, bool ring)
        {
            Texture2D texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;
            Vector2 center = new Vector2(size - 1, size - 1) * 0.5f;
            float outer = size * 0.45f;
            float inner = ring ? size * 0.35f : 0f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    bool draw = distance <= outer && (!ring || distance >= inner);
                    texture.SetPixel(x, y, draw ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
