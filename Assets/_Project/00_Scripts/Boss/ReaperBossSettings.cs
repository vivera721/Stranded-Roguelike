using UnityEngine;

namespace StrandedRoguelike
{
    [System.Serializable]
    public sealed class ReaperReferenceSettings
    {
        [Header("Core")]
        public Animator animator;
        public Transform target;

        [Header("Points")]
        public Transform leftHandSlamPoint;
        public Transform rightHandSlamPoint;
        public Transform laserGroundPoint;

        [Header("Prefabs")]
        public GameObject orbBurstPrefab;
        public EnemyProjectile bulletPrefab;
    }

    [System.Serializable]
    public sealed class ReaperAnimatorSettings
    {
        public Animator bodyAnim;
        public Animator leftHandAnim;
        public Animator rightHandAnim;
    }

    [System.Serializable]
    public sealed class ReaperRoomSettings
    {
        public Transform roomCenter;
        public Vector2 roomSize = new Vector2(14f, 8f);
    }

    [System.Serializable]
    public sealed class ReaperWarningSettings
    {
        public Sprite lineWarningSprite;
        public Sprite sweepWarningSprite;
        public Sprite circleWarningSprite;
    }

    [System.Serializable]
    public sealed class ReaperBossFlowSettings
    {
        [Header("Pattern Loop")]
        public bool autoStartPatterns = true;

        [Min(2f)]
        public float patternDelay = 2f;

        [Header("Phase Condition")]
        [Range(0.01f, 0.99f)]
        public float blackSpellHealthRatio = 0.4f;

        [Range(0.01f, 0.99f)]
        public float finalRageHealthRatio = 0.1f;

        [Header("Animator Triggers")]
        public string blackSpellTrigger = "BlackSpell";
        public string rageTrigger = "Rage";

        [Header("Black Spell")]
        public GameObject blackSpellEffect;

        [Min(0.1f)]
        public float blackSpellDuration = 2f;
    }

    [System.Serializable]
    public sealed class ReaperPhaseRoarSettings
    {
        [Min(0.1f)]
        public float radius = 6f;

        [Min(0f)]
        public float knockbackDistance = 1.35f;

        [Min(0.01f)]
        public float knockbackDuration = 0.18f;

        [Min(0.05f)]
        public float visualDuration = 0.45f;

        [Min(0.01f)]
        public float lineWidth = 0.08f;

        public Color color = Color.white;
    }

    [System.Serializable]
    public sealed class ReaperShockwaveSettings
    {
        [Header("Base")]
        [Min(1)]
        public int shockwaveDamage = 1;

        [Min(0.01f)]
        public float shockwaveRadius = 0.48f;

        [Min(0.1f)]
        public float shockwaveDistance = 7f;

        [Min(0.1f)]
        public float shockwaveSpacing = 0.75f;

        [Header("Timing")]
        [Min(0f)]
        public float shockwaveStepDelay = 0.055f;

        [Min(0f)]
        public float warningTime = 0.08f;

        [Min(0f)]
        public float handSlamAttackWarningTime = 0.75f;

        [Header("Line Shockwave")]
        [Min(0.01f)]
        public float lineShockwaveWarningWidth = 1.2f;

        [Header("Wide Shockwave")]
        [Min(1)]
        public int wideShockwaveRings = 4;

        [Min(4)]
        public int wideShockwaveBurstsPerRing = 12;

        [Min(0.1f)]
        public float wideShockwaveRingSpacing = 0.9f;
    }

    [System.Serializable]
    public sealed class ReaperLaserSettings
    {
        [Min(0.1f)]
        public float laserDuration = 4f;

        public ReaperLeftLaserSettings left;
        public ReaperRightLaserSettings right;
    }

    [System.Serializable]
    public sealed class ReaperLeftLaserSettings
    {
        [Min(0.01f)]
        public float fireInterval = 0.16f;

        [Min(0f)]
        public float bulletSpeed = 2.4f;

        [Min(0.01f)]
        public float bulletLifeTime = 5f;

        [Min(1)]
        public int bulletDamage = 1;

        [Min(1)]
        public int spiralBranches = 3;

        public float spiralAngularSpeed = 65f;
    }

    [System.Serializable]
    public sealed class ReaperRightLaserSettings
    {
        [Min(0.1f)]
        public float aimInterval = 1f;

        [Min(0f)]
        public float bulletSpeed = 4.2f;

        [Min(0.01f)]
        public float bulletLifeTime = 5f;

        [Min(1)]
        public int bulletDamage = 1;
    }

    [System.Serializable]
    public sealed class ReaperBlackSpellSettings
    {
        [Header("Fan Bullet")]
        [Min(1)]
        public int fanBulletCount = 9;

        [Range(0f, 180f)]
        public float fanSpreadAngle = 65f;

        [Min(0.01f)]
        public float fanInterval = 0.18f;

        [Header("Buff")]
        [Min(0)]
        public int addLaserBranches = 1;

        [Min(0f)]
        public float reduceRightLaserAimInterval = 0.35f;

        [Min(0.01f)]
        public float minRightLaserAimInterval = 0.25f;
    }

    [System.Serializable]
    public sealed class ReaperSweepSettings
    {
        [Min(1)]
        public int sweepRows = 4;

        [Min(0.01f)]
        public float sweepStepInterval = 0.08f;

        [Min(0.1f)]
        public float columnSpacing = 0.75f;

        [Min(0f)]
        public float sweepBulletSpeed = 4f;

        [Min(0.01f)]
        public float sweepBulletLifeTime = 5f;

        [Min(1)]
        public int sweepBulletDamage = 1;

        [Min(0f)]
        public float handSweepAttackWarningTime = 1f;
    }

    [System.Serializable]
    public sealed class ReaperFinalRageSettings
    {
        [Min(0f)]
        public float centerMoveSpeed = 4f;

        [Min(0.1f)]
        public float finalSpiralDuration = 10f;

        [Min(0.01f)]
        public float finalSpiralFireInterval = 0.11f;

        [Min(0f)]
        public float finalSpiralBulletSpeed = 2.2f;

        [Min(0.01f)]
        public float finalSpiralBulletLifeTime = 5f;

        [Min(1)]
        public int finalSpiralBulletDamage = 1;

        public float finalSpiralAngularSpeed = 65f;
    }

}