using UnityEngine;

namespace StrandedRoguelike
{
    public sealed class GunBotController : MonoBehaviour
    {
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject poisonBottlePrefab;
        [SerializeField] private GameObject flameBottlePrefab;
        [SerializeField, Min(0.2f)] private float attackCooldown = 1.8f;
        [SerializeField, Min(0.5f)] private float attackRange = 8f;
        [SerializeField] private bool alternateBottleType = true;
        [SerializeField] private PoisonBottleSettings poisonSettings = new PoisonBottleSettings();
        //{
        //    kind = BotProjectileKind.PoisonBottle,
        //    flightTime = 0.7f,
        //    arcHeight = 1.5f,
        //    impactRadius = 1.2f,
        //    impactDamage = 1,
        //    poisonDuration = 6f,
        //    poisonTickInterval = 1f,
        //    poisonTickDamage = 1,
        //    poisonSpreadRadius = 2.2f,
        //    poisonSpreadCount = 2
        //};
        [SerializeField] private FlameBottleSettings flameSettings = new FlameBottleSettings();
        //{
        //    kind = BotProjectileKind.FlameBottle,
        //    flightTime = 0.65f,
        //    arcHeight = 1.35f,
        //    impactRadius = 1.15f,
        //    impactDamage = 1,
        //    burnDuration = 2.5f,
        //    burnTickInterval = 0.5f,
        //    burnTickDamage = 1,
        //    burnDeathExplosionRadius = 1.25f,
        //    burnDeathExplosionDamage = 1
        //};

        private float nextAttackTime;
        private bool usePoisonNext = true;

        private void Update()
        {
            if (Time.time < nextAttackTime)
            {
                return;
            }

            EnemyHealth target = FindNearestEnemy();
            if (target == null)
            {
                return;
            }

            ThrowBottle(target.transform.position);
            nextAttackTime = Time.time + attackCooldown;
        }

        private void ThrowBottle(Vector2 targetPosition)
        {
            bool usePoison = !alternateBottleType || usePoisonNext;
            GameObject prefab = usePoison ? poisonBottlePrefab : flameBottlePrefab;
            ArcProjectileSettings selectedSettings = usePoison ? poisonSettings.ToArcSettings() : flameSettings.ToArcSettings();

            if (prefab == null)
            {
                return;
            }

            Transform origin = firePoint != null ? firePoint : transform;
            GameObject projectileObject = Instantiate(prefab, origin.position, Quaternion.identity);
            projectileObject.SetActive(true);

            ArcProjectile projectile = projectileObject.GetComponent<ArcProjectile>();
            if (projectile == null)
            {
                projectile = projectileObject.AddComponent<ArcProjectile>();
            }

            projectile.Configure(selectedSettings);
            projectile.Launch(origin.position, targetPosition);

            if (alternateBottleType)
            {
                usePoisonNext = !usePoisonNext;
            }
        }

        private EnemyHealth FindNearestEnemy()
        {
            EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            EnemyHealth nearest = null;
            float nearestSqrDistance = attackRange * attackRange;

            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == null || enemies[i].isDead)
                {
                    continue;
                }

                float sqrDistance = ((Vector2)enemies[i].transform.position - (Vector2)transform.position).sqrMagnitude;
                if (sqrDistance <= nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearest = enemies[i];
                }
            }

            return nearest;
        }
    }
}
