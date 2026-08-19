using System.Collections.Generic;
using UnityEngine;

namespace StrandedRoguelike
{
    public sealed class SurvivorWeaponController : MonoBehaviour
    {
        private sealed class WeaponState
        {
            public SurvivorWeaponKind kind;
            public int level;
            public float timer;
        }

        [Header("Targeting")]
        [SerializeField] private LayerMask enemyLayers = ~0;
        [SerializeField, Min(1f)] private float targetSearchRadius = 12f;
        [Tooltip("Target range used by poison bottles, flame bottles, and the axe boomerang.")]
        [SerializeField, Min(1f)] private float limitedProjectileTargetRadius = 15f;
        [SerializeField, Min(1)] private int maxWeaponLevel = 6;

        [Header("Sprites / Prefabs")]
        [SerializeField] private Sprite technoBladeSprite;
        [SerializeField] private GameObject technoBladePrefab;
        [SerializeField] private Sprite fireballSprite;
        [SerializeField] private Sprite axeSprite;
        [SerializeField] private Sprite poisonBottleSprite;
        [SerializeField] private Sprite flameBottleSprite;
        [SerializeField] private Sprite missileSprite;
        [SerializeField] private GameObject poisonBottleProjectilePrefab;
        [SerializeField] private GameObject flameBottleProjectilePrefab;
        [SerializeField] private GameObject fireballImpactVFXPrefab;
        [SerializeField] private GameObject iceSpikeImpactVFXPrefab;
        [SerializeField] private GameObject lightningImpactVFXPrefab;
        [SerializeField] private GameObject missileImpactVFXPrefab;

        [Header("Bottle Settings")]
        [SerializeField] private PoisonBottleSettings poisonBottleSettings = new PoisonBottleSettings();
        [SerializeField] private FlameBottleSettings flameBottleSettings = new FlameBottleSettings();

        [Header("Techno Blade")]
        [SerializeField, Min(0.1f)] private float bladeIdleRadius = 0.85f;
        [Tooltip("Base orbit speed in radians per second.")]
        [InspectorName("Blade Base Orbit Speed")]
        [SerializeField, Min(0f)] private float bladeIdleOrbitSpeed = 5f;
        [Tooltip("Minimum time before the same enemy can take blade damage again.")]
        [SerializeField, Min(0.05f)] private float bladeEnemyHitCooldown = 0.45f;
        [SerializeField] private string bladeAttackBoolName = "isAttack";

        [Header("Chain Lightning")]
        [SerializeField] private Sprite chainLightningSprite;
        [SerializeField] private Color chainLightningColor = new Color(0.35f, 0.95f, 1f, 0.9f);
        [SerializeField, Min(0.03f)] private float chainLightningVisualDuration = 0.16f;
        [SerializeField, Min(0.02f)] private float chainLightningVisualWidth = 0.18f;
        [SerializeField] private int chainLightningSortingOrder = 45;
        [SerializeField, Min(0.1f)] private float chainLightningRadius = 4.2f;
        [SerializeField, Min(0f)] private float chainLightningInternalCooldown = 0.12f;

        [Header("Trail Weapons")]
        [SerializeField, Min(0.05f)] private float trailSampleInterval = 0.2f;
        [SerializeField, Min(2)] private int trailSampleCount = 8;
        [SerializeField, Min(0.1f)] private float iceSpikeDuration = 1.75f;
        [SerializeField, Min(0.1f)] private float iceSpikeVFXLifetime = 1.4f;

        [Header("Level 6 Weapon Fusions")]
        [SerializeField, Min(0.1f)] private float bladeAxeFusionCooldown = 0.8f;
        [SerializeField, Min(0.1f)] private float bladeAxeFlightDuration = 0.85f;
        [SerializeField, Min(0.1f)] private float iceFireballStarDistance = 0.65f;
        [SerializeField, Min(0.1f)] private float iceFireballStarHitRadius = 0.5f;
        [SerializeField] private AnimationClip flameGroundAnimationClip;
        [SerializeField, Min(0.1f)] private float fireballFlameGroundRadius = 0.9f;
        [SerializeField, Min(0.1f)] private float bottleFlameGroundRadius = 1.35f;
        [SerializeField, Min(0.1f)] private float flameGroundDuration = 3.5f;
        [SerializeField, Min(0.1f)] private float flameGroundTickInterval = 0.5f;
        [SerializeField, Min(0)] private int flameGroundTickDamage = 1;
        [SerializeField] private Sprite[] missileBindingRuneFrames;
        [SerializeField, Min(0.1f)] private float missileBindingRadius = 1.8f;
        [SerializeField, Min(0.1f)] private float missileLightningOctagonRadius = 2.1f;
        [SerializeField, Min(0.1f)] private float missileLightningHitRadius = 0.65f;

        [Header("Fusion Tests (Play Mode Only)")]
        [Tooltip("Distance from the player used by the fusion Context Menu tests.")]
        [SerializeField, Min(0.5f)] private float fusionTestDistance = 3f;
        [Tooltip("Shortened missile warning time used only by the fusion Context Menu test.")]
        [SerializeField, Min(0.1f)] private float fusionTestMissileDelay = 1.25f;

        private readonly List<WeaponState> weapons = new List<WeaponState>();
        private readonly Queue<Vector2> positionTrail = new Queue<Vector2>();
        private readonly List<Transform> bladeObjects = new List<Transform>();
        private readonly List<Animator> bladeAnimators = new List<Animator>();
        private float bladeOrbitAngle = -25f * Mathf.Deg2Rad;
        private bool processingChainLightning;
        private float nextChainLightningTime;
        private float trailSampleTimer;
        private static Sprite fallbackChainLightningSprite;

        private readonly Dictionary<EnemyHealth, float> technoBladeNextHitTimes = new Dictionary<EnemyHealth, float>();
        private readonly List<EnemyHealth> technoBladeHitCleanup = new List<EnemyHealth>();
        private float nextTechnoBladeHitCleanupTime;

        private void Awake()
        {
            maxWeaponLevel = 6;
#if UNITY_EDITOR
            AutoAssignFusionVisualsInEditor();
#endif
        }

        private void OnValidate()
        {
            maxWeaponLevel = 6;
#if UNITY_EDITOR
            AutoAssignFusionVisualsInEditor();
#endif
        }

        private void OnEnable()
        {
            SurvivorDamageEvents.EnemyDamaged += OnEnemyDamaged;
        }

        private void OnDisable()
        {
            SurvivorDamageEvents.EnemyDamaged -= OnEnemyDamaged;
            technoBladeNextHitTimes.Clear();
            technoBladeHitCleanup.Clear();
            SetBladeAttackBool(false);
        }

        private void Update()
        {
            SampleTrailPosition();
            UpdateTechnoBladeOrbit();

            for (int i = 0; i < weapons.Count; i++)
            {
                if (weapons[i].kind == SurvivorWeaponKind.ChainLightning
                    || weapons[i].kind == SurvivorWeaponKind.TechnoBlade)
                {
                    continue;
                }

                weapons[i].timer -= Time.deltaTime;
                if (weapons[i].timer <= 0f)
                {
                    FireWeapon(weapons[i]);
                    weapons[i].timer = GetCooldown(weapons[i]);
                }
            }
        }

        public void UnlockWeapon(SurvivorWeaponKind kind)
        {
            WeaponState existing = FindWeapon(kind);
            if (existing != null)
            {
                UpgradeWeapon(kind);
                return;
            }

            WeaponState state = new WeaponState
            {
                kind = kind,
                level = 1,
                timer = 0f
            };

            weapons.Add(state);

            if (kind == SurvivorWeaponKind.TechnoBlade)
            {
                EnsureBladeObjects(GetTechnoBladeCount(1));
                SetBladeAttackBool(true);
            }
        }

        public void UpgradeWeapon(SurvivorWeaponKind kind)
        {
            WeaponState state = FindWeapon(kind);
            if (state == null)
            {
                UnlockWeapon(kind);
                return;
            }

            state.level = Mathf.Min(maxWeaponLevel, state.level + 1);

            if (kind == SurvivorWeaponKind.TechnoBlade)
            {
                EnsureBladeObjects(GetTechnoBladeCount(state.level));
                SetBladeAttackBool(true);
            }
        }

        public bool HasWeapon(SurvivorWeaponKind kind)
        {
            return FindWeapon(kind) != null;
        }

        public int GetWeaponLevel(SurvivorWeaponKind kind)
        {
            return GetLevel(kind);
        }

        public int GetMaxWeaponLevel()
        {
            return maxWeaponLevel;
        }

        public bool CanUpgradeWeapon(SurvivorWeaponKind kind)
        {
            WeaponState state = FindWeapon(kind);
            return state == null || state.level < maxWeaponLevel;
        }

        private WeaponState FindWeapon(SurvivorWeaponKind kind)
        {
            for (int i = 0; i < weapons.Count; i++)
            {
                if (weapons[i].kind == kind)
                {
                    return weapons[i];
                }
            }

            return null;
        }

        private int GetLevel(SurvivorWeaponKind kind)
        {
            WeaponState state = FindWeapon(kind);
            return state != null ? state.level : 0;
        }

        private void FireWeapon(WeaponState state)
        {
            switch (state.kind)
            {
                case SurvivorWeaponKind.PoisonBottle:
                    FireBottle(state.level, true);
                    break;
                case SurvivorWeaponKind.FlameBottle:
                    FireBottle(state.level, false);
                    break;
                case SurvivorWeaponKind.Fireball:
                    FireFireball(state.level);
                    break;
                case SurvivorWeaponKind.IceSpike:
                    FireIceSpike(state.level);
                    break;
                case SurvivorWeaponKind.LightningBolt:
                    FireLightningBolt(state.level);
                    break;
                case SurvivorWeaponKind.AxeBoomerang:
                    FireAxeBoomerang(state.level);
                    break;
                case SurvivorWeaponKind.Missile:
                    FireMissile(state.level);
                    break;
            }
        }

        private float GetCooldown(WeaponState state)
        {
            switch (state.kind)
            {
                case SurvivorWeaponKind.PoisonBottle:
                case SurvivorWeaponKind.FlameBottle:
                    return 3.1f;
                case SurvivorWeaponKind.Fireball:
                    return 1.25f;
                case SurvivorWeaponKind.IceSpike:
                    return state.level >= 5 ? 1.65f : state.level >= 3 ? 1.95f : 2.4f;
                case SurvivorWeaponKind.LightningBolt:
                    return state.level >= 5 ? 2.05f : state.level >= 3 ? 2.45f : 3.0f;
                case SurvivorWeaponKind.AxeBoomerang:
                    if (IsFusionActive(SurvivorWeaponKind.AxeBoomerang, SurvivorWeaponKind.TechnoBlade))
                    {
                        return bladeAxeFusionCooldown;
                    }

                    return state.level >= 5 ? 1.75f : state.level >= 3 ? 2.2f : 2.7f;
                case SurvivorWeaponKind.Missile:
                    return state.level >= 5 ? 4.5f : state.level >= 3 ? 5.2f : 6.0f;
                default:
                    return 1f;
            }
        }

        public void TryHitWithTechnoBlade(EnemyHealth enemy)
        {
            if (enemy == null || enemy.isDead) return;

            int level = GetLevel(SurvivorWeaponKind.TechnoBlade);
            if (level <= 0) return;

            if (technoBladeNextHitTimes.TryGetValue(enemy, out float nextHitTime)
                && Time.time < nextHitTime)
            {
                return;
            }

            technoBladeNextHitTimes[enemy] = Time.time + Mathf.Max(0.05f, bladeEnemyHitCooldown);
            enemy.TakeDamage(GetTechnoBladeDamage(level), transform.position);
        }

        private void FireBottle(int level, bool poison)
        {
            EnemyHealth target = SurvivorTargeting.FindNearestEnemy(
                transform.position,
                limitedProjectileTargetRadius,
                enemyLayers);

            if (target == null)
            {
                return;
            }

            Vector2 targetPosition = target.transform.position;

            GameObject prefab = poison ? poisonBottleProjectilePrefab : flameBottleProjectilePrefab;
            Sprite sprite = poison ? poisonBottleSprite : flameBottleSprite;

            ArcProjectileSettings settings = poison
                ? poisonBottleSettings.ToArcSettings()
                : flameBottleSettings.ToArcSettings();

            int effectUpgradeCount = GetBottleEffectUpgradeCount(level);
            settings.enemyLayers = enemyLayers;
            settings.impactRadius *= GetBottleRadiusMultiplier(level);

            if (poison)
            {
                settings.poisonDuration += effectUpgradeCount;
            }
            else
            {
                settings.impactDamage += effectUpgradeCount;
                settings.burnTickDamage += effectUpgradeCount;
                settings.burnDeathExplosionDamage += effectUpgradeCount;
            }

            GameObject projectileObject = prefab != null
                ? Instantiate(prefab, transform.position, Quaternion.identity)
                : new GameObject(poison ? "Poison Bottle" : "Flame Bottle");

            SpriteRenderer renderer = projectileObject.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = projectileObject.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 30;
            }

            if (renderer.sprite == null)
            {
                renderer.sprite = sprite;
            }

            ArcProjectile projectile = projectileObject.GetComponent<ArcProjectile>();
            if (projectile == null)
            {
                projectile = projectileObject.AddComponent<ArcProjectile>();
            }

            System.Action<Vector2> impactCallback = !poison
                && IsFusionActive(SurvivorWeaponKind.FlameBottle, SurvivorWeaponKind.Fireball)
                    ? OnFusedFlameBottleImpact
                    : null;

            projectile.Configure(settings, impactCallback);
            projectile.Launch(transform.position, targetPosition);
        }

        private void FireFireball(int level)
        {
            EnemyHealth target = SurvivorTargeting.FindNearestEnemy(transform.position, targetSearchRadius, enemyLayers);
            if (target == null)
            {
                return;
            }

            Vector2 aimDirection =
                ((Vector2)target.transform.position - (Vector2)transform.position).normalized;

            int count = GetFireballCount(level);
            int damage = GetFireballDamage(level);
            int pierce = level >= 3 ? 2 : 1;
            float fanStep = count <= 1 ? 0f : 12f;
            float startAngle = -(count - 1) * fanStep * 0.5f;
            System.Action<Vector2> hitCallback =
                IsFusionActive(SurvivorWeaponKind.IceSpike, SurvivorWeaponKind.Fireball)
                || IsFusionActive(SurvivorWeaponKind.FlameBottle, SurvivorWeaponKind.Fireball)
                    ? OnFusedFireballHit
                    : null;

            for (int i = 0; i < count; i++)
            {
                Vector2 direction = Rotate(aimDirection, startAngle + i * fanStep);
                SurvivorStraightProjectile.Spawn(
                    transform.position,
                    direction,
                    fireballSprite,
                    8f,
                    2.5f,
                    0.25f,
                    damage,
                    pierce,
                    enemyLayers,
                    fireballImpactVFXPrefab,
                    hitCallback: hitCallback);
            }
        }

        private void FireIceSpike(int level)
        {
            Vector2 position = positionTrail.Count > 0 ? positionTrail.Peek() : (Vector2)transform.position;
            float durationMultiplier = level >= 6 ? 1.5f : 1f;
            SurvivorDelayedAreaHit.Spawn(
                SurvivorAreaHitKind.IceSpike,
                position,
                0.55f,
                0.85f + level * 0.08f,
                GetIceSpikeDamage(level),
                enemyLayers,
                iceSpikeImpactVFXPrefab,
                iceSpikeDuration * durationMultiplier,
                0.45f,
                iceSpikeVFXLifetime * durationMultiplier);
        }

        private void FireLightningBolt(int level)
        {
            if (IsFusionActive(SurvivorWeaponKind.Missile, SurvivorWeaponKind.LightningBolt))
            {
                return;
            }

            int count = level >= 6 ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                Vector2 position = SurvivorTargeting.RandomPointInCircle(transform.position, 6f);
                SurvivorDelayedAreaHit.Spawn(SurvivorAreaHitKind.LightningBolt, position, 0.35f, 1.05f, GetLightningBoltDamage(level), enemyLayers, lightningImpactVFXPrefab);
            }
        }

        private void FireAxeBoomerang(int level)
        {
            if (IsFusionActive(SurvivorWeaponKind.AxeBoomerang, SurvivorWeaponKind.TechnoBlade))
            {
                FireBladeAxeFusion(level);
                return;
            }

            EnemyHealth target = SurvivorTargeting.FindNearestEnemy(
                transform.position,
                limitedProjectileTargetRadius,
                enemyLayers);

            if (target == null)
            {
                return;
            }

            Vector2 targetPosition = target.transform.position;

            float hitRadius = level >= 6 ? 0.55f : 0.35f;
            SurvivorAxeBoomerangProjectile.Spawn(transform, targetPosition, axeSprite, 1.25f, 0.9f, hitRadius, GetAxeDamage(level), enemyLayers);
        }

        private void FireMissile(int level)
        {
            bool useMissileLightningFusion =
                IsFusionActive(SurvivorWeaponKind.Missile, SurvivorWeaponKind.LightningBolt);
            int count = level >= 6 ? 2 : 1;

            for (int i = 0; i < count; i++)
            {
                EnemyHealth target = SurvivorTargeting.FindNearestEnemy(transform.position, targetSearchRadius, enemyLayers);
                Vector2 targetPosition;

                if (target != null)
                {
                    targetPosition = useMissileLightningFusion
                        ? target.transform.position
                        : (Vector2)target.transform.position + Random.insideUnitCircle * 0.45f;
                }
                else
                {
                    targetPosition = SurvivorTargeting.RandomPointInCircle(transform.position, 5f);
                }

                SpawnMissileAt(targetPosition, level, useMissileLightningFusion, 3f);
            }
        }

        private void SpawnMissileAt(
            Vector2 targetPosition,
            int level,
            bool useMissileLightningFusion,
            float missileDelay)
        {
            const float missileFallDuration = 0.45f;
            float missileRadius = 1.35f + level * 0.1f;

            if (useMissileLightningFusion)
            {
                SurvivorBindingField.Spawn(
                    targetPosition,
                    missileBindingRuneFrames,
                    Mathf.Max(missileBindingRadius, missileRadius),
                    missileDelay + missileFallDuration,
                    enemyLayers);
            }

            MissileStrike.Spawn(
                targetPosition,
                missileDelay,
                missileFallDuration,
                5.5f,
                missileRadius,
                GetMissileDamage(level),
                missileSprite,
                missileImpactVFXPrefab,
                true,
                useMissileLightningFusion ? OnFusedMissileImpact : null);
        }

        private void FireBladeAxeFusion(int axeLevel, bool useFallbackTargets = false)
        {
            int bladeCount = GetTechnoBladeCount(GetLevel(SurvivorWeaponKind.TechnoBlade));
            EnsureBladeObjects(bladeCount);

            List<EnemyHealth> selectedTargets = new List<EnemyHealth>();
            float hitRadius = axeLevel >= 6 ? 0.55f : 0.35f;

            for (int i = 0; i < bladeCount && i < bladeObjects.Count; i++)
            {
                Transform blade = bladeObjects[i];
                if (blade == null || !blade.gameObject.activeInHierarchy)
                {
                    continue;
                }

                EnemyHealth target = SurvivorTargeting.FindNearestEnemyExcept(
                    blade.position,
                    limitedProjectileTargetRadius,
                    enemyLayers,
                    selectedTargets);

                if (target == null)
                {
                    target = SurvivorTargeting.FindNearestEnemy(
                        blade.position,
                        limitedProjectileTargetRadius,
                        enemyLayers);
                }

                Vector2 targetPosition;
                if (target != null)
                {
                    selectedTargets.Add(target);
                    targetPosition = target.transform.position;
                }
                else if (useFallbackTargets)
                {
                    float fallbackAngle = i * 360f / Mathf.Max(1, bladeCount);
                    Vector2 fallbackDirection = Rotate(Vector2.right, fallbackAngle);
                    targetPosition = (Vector2)blade.position + fallbackDirection * fusionTestDistance;
                }
                else
                {
                    continue;
                }

                SurvivorAxeBoomerangProjectile.Spawn(
                    blade,
                    targetPosition,
                    axeSprite,
                    bladeAxeFlightDuration,
                    0.65f,
                    hitRadius,
                    GetAxeDamage(axeLevel),
                    enemyLayers);
            }
        }

        private void OnFusedFireballHit(Vector2 hitPosition)
        {
            if (IsFusionActive(SurvivorWeaponKind.IceSpike, SurvivorWeaponKind.Fireball))
            {
                SpawnIceFireballStar(hitPosition);
            }

            if (IsFusionActive(SurvivorWeaponKind.FlameBottle, SurvivorWeaponKind.Fireball))
            {
                SpawnFlameGround(hitPosition, fireballFlameGroundRadius);
            }
        }

        private void SpawnIceFireballStar(Vector2 center)
        {
            const int branchCount = 6;
            int damage = GetIceSpikeDamage(maxWeaponLevel);

            for (int i = 0; i < branchCount; i++)
            {
                float angle = i * 360f / branchCount;
                Vector2 direction = Rotate(Vector2.right, angle);
                Vector2 spikePosition = center + direction * iceFireballStarDistance;

                SurvivorDelayedAreaHit.Spawn(
                    SurvivorAreaHitKind.IceSpike,
                    spikePosition,
                    0.08f,
                    iceFireballStarHitRadius,
                    damage,
                    enemyLayers,
                    iceSpikeImpactVFXPrefab,
                    iceSpikeDuration,
                    0.35f,
                    iceSpikeVFXLifetime,
                    angle - 90f);
            }
        }

        private void OnFusedFlameBottleImpact(Vector2 impactPosition)
        {
            SpawnFlameGround(impactPosition, bottleFlameGroundRadius);
        }

        private void SpawnFlameGround(Vector2 position, float radius)
        {
            SurvivorFlameGround.Spawn(
                position,
                flameGroundAnimationClip,
                radius,
                flameGroundDuration,
                flameGroundTickInterval,
                flameGroundTickDamage,
                enemyLayers);
        }

        private void OnFusedMissileImpact(Vector2 impactPosition)
        {
            const int lightningCount = 8;
            int lightningDamage = Mathf.Max(
                1,
                Mathf.CeilToInt(GetLightningBoltDamage(maxWeaponLevel) * 0.5f));

            for (int i = 0; i < lightningCount; i++)
            {
                float angle = i * 360f / lightningCount;
                Vector2 direction = Rotate(Vector2.right, angle);
                Vector2 lightningPosition =
                    impactPosition + direction * missileLightningOctagonRadius;

                SurvivorDelayedAreaHit.Spawn(
                    SurvivorAreaHitKind.LightningBolt,
                    lightningPosition,
                    0.12f,
                    missileLightningHitRadius,
                    lightningDamage,
                    enemyLayers,
                    lightningImpactVFXPrefab);
            }
        }

        private bool IsFusionActive(SurvivorWeaponKind first, SurvivorWeaponKind second)
        {
            return GetLevel(first) >= maxWeaponLevel
                && GetLevel(second) >= maxWeaponLevel;
        }

        [ContextMenu("Fusion Tests/01 Axe Boomerang + Techno Blade")]
        private void TestAxeTechnoBladeFusion()
        {
            if (!CanRunFusionTest("Axe Boomerang + Techno Blade"))
            {
                return;
            }

            SetFusionTestLevels(
                SurvivorWeaponKind.AxeBoomerang,
                SurvivorWeaponKind.TechnoBlade);
            UpdateTechnoBladeOrbit();
            FireBladeAxeFusion(maxWeaponLevel, true);
            ResetFusionTestTimers(
                SurvivorWeaponKind.AxeBoomerang,
                SurvivorWeaponKind.TechnoBlade);
            Debug.Log("[Fusion Test] Axe Boomerang + Techno Blade activated at Lv.6.", this);
        }

        [ContextMenu("Fusion Tests/02 Ice Spike + Fireball")]
        private void TestIceFireballFusion()
        {
            if (!CanRunFusionTest("Ice Spike + Fireball"))
            {
                return;
            }

            SetFusionTestLevels(
                SurvivorWeaponKind.IceSpike,
                SurvivorWeaponKind.Fireball);
            SpawnIceFireballStar(GetFusionTestPosition(Vector2.right));
            ResetFusionTestTimers(
                SurvivorWeaponKind.IceSpike,
                SurvivorWeaponKind.Fireball);
            Debug.Log("[Fusion Test] Ice Spike + Fireball activated at Lv.6.", this);
        }

        [ContextMenu("Fusion Tests/03 Missile + Lightning Bolt")]
        private void TestMissileLightningFusion()
        {
            if (!CanRunFusionTest("Missile + Lightning Bolt"))
            {
                return;
            }

            SetFusionTestLevels(
                SurvivorWeaponKind.Missile,
                SurvivorWeaponKind.LightningBolt);
            SpawnMissileAt(
                GetFusionTestPosition(Vector2.up),
                maxWeaponLevel,
                true,
                fusionTestMissileDelay);
            ResetFusionTestTimers(
                SurvivorWeaponKind.Missile,
                SurvivorWeaponKind.LightningBolt);
            Debug.Log("[Fusion Test] Missile + Lightning Bolt activated at Lv.6.", this);
        }

        [ContextMenu("Fusion Tests/04 Flame Bottle + Fireball")]
        private void TestFlameFireballFusion()
        {
            if (!CanRunFusionTest("Flame Bottle + Fireball"))
            {
                return;
            }

            SetFusionTestLevels(
                SurvivorWeaponKind.FlameBottle,
                SurvivorWeaponKind.Fireball);
            Vector2 testPosition = GetFusionTestPosition(Vector2.left);
            SpawnFlameGround(testPosition, bottleFlameGroundRadius);
            SpawnFlameGround(
                testPosition + Vector2.down * fireballFlameGroundRadius,
                fireballFlameGroundRadius);
            ResetFusionTestTimers(
                SurvivorWeaponKind.FlameBottle,
                SurvivorWeaponKind.Fireball);
            Debug.Log("[Fusion Test] Flame Bottle + Fireball activated at Lv.6.", this);
        }

        [ContextMenu("Fusion Tests/05 Test All Fusions")]
        private void TestAllFusions()
        {
            if (!CanRunFusionTest("All Fusions"))
            {
                return;
            }

            SetFusionTestLevels(
                SurvivorWeaponKind.AxeBoomerang,
                SurvivorWeaponKind.TechnoBlade,
                SurvivorWeaponKind.IceSpike,
                SurvivorWeaponKind.Fireball,
                SurvivorWeaponKind.Missile,
                SurvivorWeaponKind.LightningBolt,
                SurvivorWeaponKind.FlameBottle);

            UpdateTechnoBladeOrbit();
            FireBladeAxeFusion(maxWeaponLevel, true);
            OnFusedFireballHit(GetFusionTestPosition(Vector2.right));
            OnFusedFlameBottleImpact(GetFusionTestPosition(Vector2.left));
            SpawnMissileAt(
                GetFusionTestPosition(Vector2.up),
                maxWeaponLevel,
                true,
                fusionTestMissileDelay);

            ResetFusionTestTimers(
                SurvivorWeaponKind.AxeBoomerang,
                SurvivorWeaponKind.TechnoBlade,
                SurvivorWeaponKind.IceSpike,
                SurvivorWeaponKind.Fireball,
                SurvivorWeaponKind.Missile,
                SurvivorWeaponKind.LightningBolt,
                SurvivorWeaponKind.FlameBottle);
            Debug.Log("[Fusion Test] All four Lv.6 fusions activated.", this);
        }

        private bool CanRunFusionTest(string testName)
        {
            if (Application.isPlaying)
            {
                return true;
            }

            Debug.LogWarning(
                $"[Fusion Test] {testName} can only be used while the game is in Play Mode.",
                this);
            return false;
        }

        private void SetFusionTestLevels(params SurvivorWeaponKind[] kinds)
        {
            for (int i = 0; i < kinds.Length; i++)
            {
                SurvivorWeaponKind kind = kinds[i];
                WeaponState state = FindWeapon(kind);

                if (state == null)
                {
                    state = new WeaponState
                    {
                        kind = kind,
                        level = maxWeaponLevel,
                        timer = 0f
                    };
                    weapons.Add(state);
                }
                else
                {
                    state.level = maxWeaponLevel;
                }

                if (kind == SurvivorWeaponKind.TechnoBlade)
                {
                    EnsureBladeObjects(GetTechnoBladeCount(maxWeaponLevel));
                    SetBladeAttackBool(true);
                }
            }
        }

        private void ResetFusionTestTimers(params SurvivorWeaponKind[] kinds)
        {
            for (int i = 0; i < kinds.Length; i++)
            {
                WeaponState state = FindWeapon(kinds[i]);
                if (state != null
                    && state.kind != SurvivorWeaponKind.ChainLightning
                    && state.kind != SurvivorWeaponKind.TechnoBlade)
                {
                    state.timer = GetCooldown(state);
                }
            }
        }

        private Vector2 GetFusionTestPosition(Vector2 direction)
        {
            Vector2 normalizedDirection =
                direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
            return (Vector2)transform.position + normalizedDirection * fusionTestDistance;
        }

        private void OnEnemyDamaged(EnemyHealth source, int damage, Vector2 hitOrigin, SurvivorDamageKind damageKind)
        {
            int level = GetLevel(SurvivorWeaponKind.ChainLightning);
            if (level <= 0 || source == null || processingChainLightning || Time.time < nextChainLightningTime)
            {
                return;
            }

            if (damageKind == SurvivorDamageKind.ChainLightning)
            {
                return;
            }

            if (damageKind == SurvivorDamageKind.StatusTick && level < 6)
            {
                return;
            }

            if (Random.value > GetChainLightningChance(level))
            {
                return;
            }

            nextChainLightningTime = Time.time + chainLightningInternalCooldown;
            ProcessChainLightning(source, level);
        }

        private void ProcessChainLightning(EnemyHealth source, int level)
        {
            processingChainLightning = true;

            List<EnemyHealth> visited = new List<EnemyHealth>();
            visited.Add(source);

            Vector2 currentPosition = source.transform.position;
            int jumps = GetChainLightningJumps(level);
            int chainDamage = GetChainLightningDamage(level);

            for (int i = 0; i < jumps; i++)
            {
                EnemyHealth next = SurvivorTargeting.FindNearestEnemyExcept(currentPosition, chainLightningRadius, enemyLayers, visited);
                if (next == null)
                {
                    break;
                }

                visited.Add(next);
                SpawnChainLightningVisual(currentPosition, next.transform.position);
                next.TakeDamage(chainDamage, currentPosition, SurvivorDamageKind.ChainLightning);
                currentPosition = next.transform.position;
            }

            processingChainLightning = false;
        }

        private void SpawnChainLightningVisual(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            float distance = delta.magnitude;
            if (distance <= 0.05f)
            {
                return;
            }

            GameObject visualObject = new GameObject("Chain Lightning Visual");
            visualObject.transform.position = Vector2.Lerp(from, to, 0.5f);
            visualObject.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
            renderer.sprite = chainLightningSprite != null ? chainLightningSprite : GetFallbackChainLightningSprite();
            renderer.color = chainLightningColor;
            renderer.sortingOrder = chainLightningSortingOrder;

            Vector2 spriteSize = renderer.sprite != null ? renderer.sprite.bounds.size : Vector2.one;
            float scaleX = spriteSize.x > 0.001f ? distance / spriteSize.x : distance;
            float scaleY = spriteSize.y > 0.001f ? chainLightningVisualWidth / spriteSize.y : chainLightningVisualWidth;
            visualObject.transform.localScale = new Vector3(scaleX, scaleY, 1f);

            RuntimeObjectLifetime.Attach(visualObject, chainLightningVisualDuration);
        }

        private void EnsureBladeObjects(int desiredCount)
        {
            desiredCount = Mathf.Max(1, desiredCount);

            while (bladeObjects.Count < desiredCount)
            {
                GameObject blade = technoBladePrefab != null
                    ? Instantiate(technoBladePrefab, transform)
                    : new GameObject("Techno Blade");

                blade.name = $"Techno Blade {bladeObjects.Count + 1}";
                blade.transform.SetParent(transform);

                SpriteRenderer renderer = blade.GetComponent<SpriteRenderer>();
                if (renderer == null)
                {
                    renderer = blade.AddComponent<SpriteRenderer>();
                }

                if (renderer.sprite == null)
                {
                    renderer.sprite = technoBladeSprite;
                }

                renderer.color = renderer.sprite != null ? Color.white : new Color(0.35f, 0.9f, 1f, 1f);
                renderer.sortingOrder = 35;

                TechnoBladeHitbox hitbox = blade.GetComponent<TechnoBladeHitbox>();
                if (hitbox == null)
                {
                    hitbox = blade.AddComponent<TechnoBladeHitbox>();
                }

                hitbox.Initialize(this);

                bladeObjects.Add(blade.transform);
                bladeAnimators.Add(blade.GetComponent<Animator>());
            }

            for (int i = 0; i < bladeObjects.Count; i++)
            {
                if (bladeObjects[i] != null)
                {
                    bladeObjects[i].gameObject.SetActive(i < desiredCount);
                }
            }
        }

        private void UpdateTechnoBladeOrbit()
        {
            int level = GetLevel(SurvivorWeaponKind.TechnoBlade);
            if (level <= 0)
            {
                return;
            }

            int bladeCount = GetTechnoBladeCount(level);
            EnsureBladeObjects(bladeCount);

            float orbitSpeed = bladeIdleOrbitSpeed * GetTechnoBladeSpeedMultiplier(level);

            bladeOrbitAngle = Mathf.Repeat(
                bladeOrbitAngle + Mathf.Max(0f, orbitSpeed) * Time.deltaTime,
                Mathf.PI * 2f);

            for (int i = 0; i < bladeCount && i < bladeObjects.Count; i++)
            {
                Transform blade = bladeObjects[i];
                if (blade == null)
                {
                    continue;
                }

                float angle = bladeOrbitAngle + i * Mathf.PI * 2f / bladeCount;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                blade.position = (Vector2)transform.position + direction * bladeIdleRadius;
                blade.rotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg - 90f);
            }

            CleanupExpiredTechnoBladeHits();
        }

        private void CleanupExpiredTechnoBladeHits()
        {
            if (Time.time < nextTechnoBladeHitCleanupTime || technoBladeNextHitTimes.Count == 0)
            {
                return;
            }

            nextTechnoBladeHitCleanupTime = Time.time + 2f;
            technoBladeHitCleanup.Clear();

            foreach (KeyValuePair<EnemyHealth, float> pair in technoBladeNextHitTimes)
            {
                if (pair.Key == null || pair.Value <= Time.time)
                {
                    technoBladeHitCleanup.Add(pair.Key);
                }
            }

            for (int i = 0; i < technoBladeHitCleanup.Count; i++)
            {
                technoBladeNextHitTimes.Remove(technoBladeHitCleanup[i]);
            }
        }

        private void SetBladeAttackBool(bool isAttack)
        {
            if (string.IsNullOrWhiteSpace(bladeAttackBoolName))
            {
                return;
            }

            for (int i = 0; i < bladeAnimators.Count; i++)
            {
                Animator animator = bladeAnimators[i];
                if (animator == null)
                {
                    continue;
                }

                animator.SetBool(bladeAttackBoolName, isAttack);
            }
        }

        private void SampleTrailPosition()
        {
            trailSampleTimer -= Time.deltaTime;
            if (trailSampleTimer > 0f)
            {
                return;
            }

            trailSampleTimer = trailSampleInterval;
            positionTrail.Enqueue(transform.position);

            while (positionTrail.Count > trailSampleCount)
            {
                positionTrail.Dequeue();
            }
        }

        private static int GetFireballDamage(int level)
        {
            return 2 + (level >= 2 ? 1 : 0) + (level >= 4 ? 1 : 0);
        }

        private static float GetBottleRadiusMultiplier(int level)
        {
            if (level >= 6)
            {
                return 1f;
            }

            return level >= 3 ? 0.75f : 0.5f;
        }

        private static int GetBottleEffectUpgradeCount(int level)
        {
            return (level >= 2 ? 1 : 0)
                + (level >= 4 ? 1 : 0)
                + (level >= 5 ? 1 : 0);
        }

        private static int GetFireballCount(int level)
        {
            if (level >= 6)
            {
                return 4;
            }

            return level >= 5 ? 2 : 1;
        }

        private static int GetAxeDamage(int level)
        {
            return 3 + (level >= 2 ? 2 : 0) + (level >= 4 ? 2 : 0);
        }

        private static int GetIceSpikeDamage(int level)
        {
            return 2 + (level >= 2 ? 2 : 0) + (level >= 4 ? 2 : 0);
        }

        private static int GetLightningBoltDamage(int level)
        {
            return 7 + (level >= 2 ? 2 : 0) + (level >= 4 ? 2 : 0) + (level >= 6 ? 2 : 0);
        }

        private static int GetMissileDamage(int level)
        {
            return 8 + (level >= 2 ? 2 : 0) + (level >= 4 ? 2 : 0) + (level >= 6 ? 2 : 0);
        }

        private static int GetTechnoBladeDamage(int level)
        {
            return 2 + (level >= 3 ? 1 : 0) + (level >= 5 ? 1 : 0);
        }

        private static float GetTechnoBladeSpeedMultiplier(int level)
        {
            float multiplier = 1f;

            if (level >= 2)
            {
                multiplier *= 1.2f;
            }

            if (level >= 3)
            {
                multiplier *= 1.1f;
            }

            if (level >= 5)
            {
                multiplier *= 1.1f;
            }

            return multiplier;
        }

        private static int GetTechnoBladeCount(int level)
        {
            if (level >= 6)
            {
                return 3;
            }

            return level >= 4 ? 2 : 1;
        }

        private static float GetChainLightningChance(int level)
        {
            if (level >= 5)
            {
                return 1f;
            }

            return level >= 3 ? 0.75f : 0.5f;
        }

        private static int GetChainLightningJumps(int level)
        {
            return level >= 4 ? 2 : 1;
        }

        private static int GetChainLightningDamage(int level)
        {
            return 1 + (level >= 2 ? 1 : 0) + (level >= 6 ? 1 : 0);
        }

        private static Vector2 Rotate(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos).normalized;
        }

        private static Sprite GetFallbackChainLightningSprite()
        {
            if (fallbackChainLightningSprite != null)
            {
                return fallbackChainLightningSprite;
            }

            Texture2D texture = new Texture2D(64, 12);
            texture.filterMode = FilterMode.Point;

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }

            Vector2[] points =
            {
                new Vector2(0f, 6f),
                new Vector2(12f, 3f),
                new Vector2(22f, 8f),
                new Vector2(35f, 4f),
                new Vector2(48f, 9f),
                new Vector2(63f, 6f)
            };

            for (int i = 0; i < points.Length - 1; i++)
            {
                DrawLine(texture, points[i], points[i + 1], Color.white, 2);
            }

            texture.Apply();
            fallbackChainLightningSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
            return fallbackChainLightningSprite;
        }

        private static void DrawLine(Texture2D texture, Vector2 from, Vector2 to, Color color, int thickness)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(from, to) * 2f);
            for (int i = 0; i <= steps; i++)
            {
                float t = steps <= 0 ? 0f : i / (float)steps;
                Vector2 point = Vector2.Lerp(from, to, t);
                int centerX = Mathf.RoundToInt(point.x);
                int centerY = Mathf.RoundToInt(point.y);

                for (int y = -thickness; y <= thickness; y++)
                {
                    for (int x = -thickness; x <= thickness; x++)
                    {
                        int pixelX = centerX + x;
                        int pixelY = centerY + y;
                        if (pixelX < 0 || pixelX >= texture.width || pixelY < 0 || pixelY >= texture.height)
                        {
                            continue;
                        }

                        texture.SetPixel(pixelX, pixelY, color);
                    }
                }
            }
        }

#if UNITY_EDITOR
        private void AutoAssignFusionVisualsInEditor()
        {
            if (flameGroundAnimationClip == null)
            {
                flameGroundAnimationClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    "Assets/_Project/_Arts/STRANDED - Roguelike Powerups/Flame Bottle/FlameGround.anim");
            }

            if (missileBindingRuneFrames != null && missileBindingRuneFrames.Length > 0)
            {
                return;
            }

            UnityEngine.Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
                "Assets/_Project/_Arts/STRANDED - Roguelike Powerups/Aoe Rune Spell/Aoe Rune Spell-Spin.png");
            List<Sprite> frames = new List<Sprite>();

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    frames.Add(sprite);
                }
            }

            frames.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            missileBindingRuneFrames = frames.ToArray();
        }
#endif

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, bladeIdleRadius);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, limitedProjectileTargetRadius);
        }

    }
}
