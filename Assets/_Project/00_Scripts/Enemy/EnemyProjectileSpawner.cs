using System.Collections;
using UnityEngine;

namespace StrandedRoguelike
{
    public sealed class EnemyProjectileSpawner : MonoBehaviour
    {
        [Header("Projectile")]
        [SerializeField] private EnemyProjectile projectilePrefab;
        [SerializeField] private Transform firePoint;

        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Default Values")]
        [SerializeField, Min(0f)] private float defaultSpeed = 5f;
        [SerializeField, Min(1)] private int defaultDamage = 1;
        [SerializeField, Min(0.01f)] private float defaultLifeTime = 5f;

        [Header("Pattern 1 - Spiral")]
        [SerializeField, Min(0.1f)] private float spiralDuration = 3f;
        [SerializeField, Min(1)] private int spiralBranches = 6;
        [SerializeField, Min(0.01f)] private float spiralFireInterval = 0.08f;
        [SerializeField] private float spiralAngularSpeed = 130f;

        [Header("Pattern 2 - Aimed Fan Volley")]
        [SerializeField, Min(1)] private int fanVolleyRepeatCount = 3;
        [SerializeField, Range(3, 4)] private int fanShotsPerVolley = 4;
        [SerializeField, Min(0.01f)] private float fanShotInterval = 0.12f;
        [SerializeField, Min(0f)] private float fanVolleyInterval = 0.5f;
        [SerializeField, Min(1)] private int fanBulletCount = 7;
        [SerializeField, Range(0f, 180f)] private float fanSpreadAngle = 55f;

        [Header("Pattern 3 - Triple Bounce")]
        [SerializeField, Range(0f, 89f)] private float tripleSideAngle = 38f;
        [SerializeField] private LayerMask wallLayer;

        private Coroutine currentPattern;

        private void Awake()
        {
            FindTargetIfNeeded();
        }

        [ContextMenu("Play Pattern/Spiral")]
        public void PlaySpiralPattern()
        {
            RestartPattern(SpiralPatternRoutine(
                spiralDuration,
                spiralBranches,
                spiralFireInterval,
                spiralAngularSpeed,
                defaultSpeed,
                defaultDamage,
                defaultLifeTime));
        }

        [ContextMenu("Play Pattern/Aimed Fan Volley")]
        public void PlayAimedFanVolleyPattern()
        {
            RestartPattern(AimedFanVolleyPatternRoutine(
                fanVolleyRepeatCount,
                fanShotsPerVolley,
                fanShotInterval,
                fanVolleyInterval,
                fanBulletCount,
                fanSpreadAngle,
                defaultSpeed,
                defaultDamage,
                defaultLifeTime));
        }

        [ContextMenu("Play Pattern/Triple Bounce")]
        public void PlayTripleBouncePattern()
        {
            Vector2 direction = DirectionToTarget();
            FireTripleBounce(direction, tripleSideAngle, defaultSpeed, defaultDamage, defaultLifeTime);
        }

        public void StopPattern()
        {
            if (currentPattern != null)
            {
                StopCoroutine(currentPattern);
                currentPattern = null;
            }
        }

        public void PlaySpiralPattern(float duration, int branches)
        {
            RestartPattern(SpiralPatternRoutine(
                duration,
                branches,
                spiralFireInterval,
                spiralAngularSpeed,
                defaultSpeed,
                defaultDamage,
                defaultLifeTime));
        }

        public void PlayAimedFanVolleyPattern(int repeatCount)
        {
            RestartPattern(AimedFanVolleyPatternRoutine(
                repeatCount,
                fanShotsPerVolley,
                fanShotInterval,
                fanVolleyInterval,
                fanBulletCount,
                fanSpreadAngle,
                defaultSpeed,
                defaultDamage,
                defaultLifeTime));
        }

        public void FireSpread(Vector2 baseDirection, int count, float spreadAngle, float speed, int damage, float lifeTime)
        {
            count = Mathf.Max(1, count);

            float startAngle = count == 1 ? 0f : -spreadAngle * 0.5f;
            float angleStep = count == 1 ? 0f : spreadAngle / (count - 1);

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + angleStep * i;
                Vector2 direction = Rotate(baseDirection, angle);
                Spawn(direction, speed, damage, lifeTime);
            }
        }

        public void FireRadial(int count, float speed, int damage, float lifeTime)
        {
            count = Mathf.Max(1, count);

            for (int i = 0; i < count; i++)
            {
                float angle = 360f / count * i;
                Vector2 direction = Rotate(Vector2.right, angle);
                Spawn(direction, speed, damage, lifeTime);
            }
        }

        public void FireTripleBounce(Vector2 baseDirection, float sideAngle, float speed, int damage, float lifeTime)
        {
            if (baseDirection == Vector2.zero)
            {
                baseDirection = Vector2.down;
            }

            LayerMask resolvedWallLayer = ResolveWallLayer();

            Spawn(Rotate(baseDirection, -sideAngle), speed, damage, lifeTime, resolvedWallLayer, 1);
            Spawn(baseDirection.normalized, speed, damage, lifeTime);
            Spawn(Rotate(baseDirection, sideAngle), speed, damage, lifeTime, resolvedWallLayer, 1);
        }

        private IEnumerator SpiralPatternRoutine(
            float duration,
            int branches,
            float fireInterval,
            float angularSpeed,
            float speed,
            int damage,
            float lifeTime)
        {
            duration = Mathf.Max(0.1f, duration);
            branches = Mathf.Max(1, branches);
            fireInterval = Mathf.Max(0.01f, fireInterval);

            float elapsed = 0f;
            float angle = 0f;

            while (elapsed < duration)
            {
                for (int i = 0; i < branches; i++)
                {
                    float branchAngle = angle + 360f / branches * i;
                    Spawn(Rotate(Vector2.right, branchAngle), speed, damage, lifeTime);
                }

                yield return new WaitForSeconds(fireInterval);

                elapsed += fireInterval;
                angle += angularSpeed * fireInterval;
            }

            currentPattern = null;
        }

        private IEnumerator AimedFanVolleyPatternRoutine(
            int repeatCount,
            int shotsPerVolley,
            float shotInterval,
            float volleyInterval,
            int bulletCount,
            float spreadAngle,
            float speed,
            int damage,
            float lifeTime)
        {
            repeatCount = Mathf.Max(1, repeatCount);
            shotsPerVolley = Mathf.Clamp(shotsPerVolley, 3, 4);
            shotInterval = Mathf.Max(0.01f, shotInterval);
            volleyInterval = Mathf.Max(0f, volleyInterval);

            for (int repeat = 0; repeat < repeatCount; repeat++)
            {
                for (int shot = 0; shot < shotsPerVolley; shot++)
                {
                    FireSpread(DirectionToTarget(), bulletCount, spreadAngle, speed, damage, lifeTime);
                    yield return new WaitForSeconds(shotInterval);
                }

                if (repeat < repeatCount - 1)
                {
                    yield return new WaitForSeconds(volleyInterval);
                }
            }

            currentPattern = null;
        }

        private void Spawn(Vector2 direction, float speed, int damage, float lifeTime)
        {
            Spawn(direction, speed, damage, lifeTime, default, 0);
        }

        private void Spawn(Vector2 direction, float speed, int damage, float lifeTime, LayerMask bounceLayers, int maxBounceCount)
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning($"{name}: Projectile Prefab is missing.");
                return;
            }

            Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;

            EnemyProjectile projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
            projectile.Launch(direction, speed, damage, lifeTime, bounceLayers, maxBounceCount);
        }

        private void RestartPattern(IEnumerator routine)
        {
            StopPattern();
            currentPattern = StartCoroutine(routine);
        }

        private Vector2 DirectionToTarget()
        {
            FindTargetIfNeeded();

            if (target == null)
            {
                return Vector2.down;
            }

            Vector2 direction = target.position - FirePosition();
            return direction == Vector2.zero ? Vector2.down : direction.normalized;
        }

        private Vector3 FirePosition()
        {
            return firePoint != null ? firePoint.position : transform.position;
        }

        private void FindTargetIfNeeded()
        {
            if (target != null)
            {
                return;
            }

            PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth != null)
            {
                target = playerHealth.transform;
                return;
            }

            PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
            if (playerMovement != null)
            {
                target = playerMovement.transform;
            }
        }

        private LayerMask ResolveWallLayer()
        {
            if (wallLayer.value != 0)
            {
                return wallLayer;
            }

            int wallLayerIndex = LayerMask.NameToLayer("Wall");
            return wallLayerIndex < 0 ? 0 : 1 << wallLayerIndex;
        }

        private static Vector2 Rotate(Vector2 direction, float angle)
        {
            float radians = angle * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);

            return new Vector2(
                direction.x * cos - direction.y * sin,
                direction.x * sin + direction.y * cos
            ).normalized;
        }
    }
}
