using UnityEngine;

namespace StrandedRoguelike
{
    public sealed class CompanionBotController : MonoBehaviour
    {
        [Header("Player Buff")]
        [SerializeField] private bool enableElectricChainBuff = true;

        [Header("Missile")]
        [SerializeField] private bool useMissileAttack = true;
        [SerializeField, Min(0.5f)] private float missileCooldown = 8f;
        [SerializeField, Min(0.5f)] private float missileRange = 9f;
        [SerializeField, Min(0.5f)] private float missileDelay = 3.5f;
        [SerializeField, Min(0.1f)] private float missileFallDuration = 0.45f;
        [SerializeField, Min(0f)] private float missileStartHeight = 6f;
        [SerializeField, Min(0f)] private float missileRadius = 1.6f;
        [SerializeField, Min(0)] private int missileDamage = 5;
        [SerializeField] private bool missileUseShadow = true;
        [SerializeField] private Sprite missileSprite;
        [SerializeField] private GameObject impactVFXPrefab;

        private float nextMissileTime;
        private PlayerAttackElectricAugment electricAugment;

        private void Start()
        {
            ApplyElectricBuff();
        }

        private void Update()
        {
            if (!useMissileAttack || Time.time < nextMissileTime)
            {
                return;
            }

            EnemyHealth target = FindNearestEnemy();
            if (target == null)
            {
                return;
            }

            MissileStrike.Spawn(target.transform.position, missileDelay, missileFallDuration, missileStartHeight, missileRadius, missileDamage, missileSprite, impactVFXPrefab, missileUseShadow);
            nextMissileTime = Time.time + missileCooldown;
        }

        private void ApplyElectricBuff()
        {
            PlayerAttack playerAttack = FindFirstObjectByType<PlayerAttack>();
            if (playerAttack == null)
            {
                return;
            }

            electricAugment = playerAttack.GetComponent<PlayerAttackElectricAugment>();
            if (electricAugment == null)
            {
                electricAugment = playerAttack.gameObject.AddComponent<PlayerAttackElectricAugment>();
            }

            electricAugment.SetElectricChainEnabled(enableElectricChainBuff);
        }

        private EnemyHealth FindNearestEnemy()
        {
            EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            EnemyHealth nearest = null;
            float nearestSqrDistance = missileRange * missileRange;

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
