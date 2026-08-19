using UnityEngine;

namespace StrandedRoguelike
{
    [System.Serializable]
    public sealed class ArcProjectileSettings
    {
        public BotProjectileKind kind = BotProjectileKind.Plain;
        [Min(0.05f)] public float flightTime = 0.65f;
        [Min(0f)] public float arcHeight = 1.4f;
        [Min(0f)] public float impactRadius = 1.15f;
        [Min(0)] public int impactDamage = 1;
        public GameObject impactVFXPrefab;
        public LayerMask enemyLayers = ~0;

        [Header("Fake Height Shadow")]
        public bool useShadow = true;
        public Color shadowColor = new Color(0f, 0f, 0f, 0.35f);
        public Vector2 shadowGroundScale = new Vector2(1f, 0.35f);
        [Range(0.05f, 1f)] public float shadowScaleAtPeak = 0.45f;
        [Range(0f, 1f)] public float shadowAlphaAtPeak = 0.18f;

        [Header("Poison")]
        [Min(0f)] public float poisonDuration = 6f;
        [Min(0.05f)] public float poisonTickInterval = 1f;
        [Min(0)] public int poisonTickDamage = 1;
        [Min(0f)] public float poisonSpreadRadius = 2.2f;
        [Min(0)] public int poisonSpreadCount = 2;

        [Header("Flame")]
        [Min(0f)] public float burnDuration = 2.5f;
        [Min(0.05f)] public float burnTickInterval = 0.5f;
        [Min(0)] public int burnTickDamage = 1;
        [Min(0f)] public float burnDeathExplosionRadius = 1.25f;
        [Min(0)] public int burnDeathExplosionDamage = 1;
    }

    [System.Serializable]
    public sealed class PoisonBottleSettings
    {
        [Header("Common")]
        [Min(0.05f)] public float flightTime = 0.7f;
        [Min(0f)] public float arcHeight = 1.5f;
        [Min(0f)] public float impactRadius = 1.2f;
        [Min(0)] public int impactDamage = 1;
        public GameObject impactVFXPrefab;
        public LayerMask enemyLayers = ~0;

        [Header("Poison")]
        [Min(0f)] public float poisonDuration = 6f;
        [Min(0.05f)] public float poisonTickInterval = 1f;
        [Min(0)] public int poisonTickDamage = 1;
        [Min(0f)] public float poisonSpreadRadius = 2.2f;
        [Min(0)] public int poisonSpreadCount = 2;

        public ArcProjectileSettings ToArcSettings()
        {
            return new ArcProjectileSettings
            {
                kind = BotProjectileKind.PoisonBottle,
                flightTime = flightTime,
                arcHeight = arcHeight,
                impactRadius = impactRadius,
                impactDamage = impactDamage,
                impactVFXPrefab = impactVFXPrefab,
                enemyLayers = enemyLayers,

                poisonDuration = poisonDuration,
                poisonTickInterval = poisonTickInterval,
                poisonTickDamage = poisonTickDamage,
                poisonSpreadRadius = poisonSpreadRadius,
                poisonSpreadCount = poisonSpreadCount
            };
        }
    }

    [System.Serializable]
    public sealed class FlameBottleSettings
    {
        [Header("Common")]
        [Min(0.05f)] public float flightTime = 0.65f;
        [Min(0f)] public float arcHeight = 1.35f;
        [Min(0f)] public float impactRadius = 1.15f;
        [Min(0)] public int impactDamage = 1;
        public GameObject impactVFXPrefab;
        public LayerMask enemyLayers = ~0;

        [Header("Flame")]
        [Min(0f)] public float burnDuration = 2.5f;
        [Min(0.05f)] public float burnTickInterval = 0.5f;
        [Min(0)] public int burnTickDamage = 1;
        [Min(0f)] public float burnDeathExplosionRadius = 1.25f;
        [Min(0)] public int burnDeathExplosionDamage = 1;

        public ArcProjectileSettings ToArcSettings()
        {
            return new ArcProjectileSettings
            {
                kind = BotProjectileKind.FlameBottle,
                flightTime = flightTime,
                arcHeight = arcHeight,
                impactRadius = impactRadius,
                impactDamage = impactDamage,
                impactVFXPrefab = impactVFXPrefab,
                enemyLayers = enemyLayers,

                burnDuration = burnDuration,
                burnTickInterval = burnTickInterval,
                burnTickDamage = burnTickDamage,
                burnDeathExplosionRadius = burnDeathExplosionRadius,
                burnDeathExplosionDamage = burnDeathExplosionDamage
            };
        }
    }
}
