namespace StrandedRoguelike
{
    public enum UpgradeCategory
    {
        Core,
        Element,
        Companion
    }

    public enum UpgradeId
    {
        SlashDamage,
        SlashRange,
        SlashCooldown,
        SlashRecovery,
        SlashThirdHit,
        SlashAfterDodge,
        ElementLightning,
        ElementFire,
        ElementIce,
        ElementPoison,
        ElementTechnoBlade,
        CompanionFlameBottle,
        CompanionPoisonBottle,
        CompanionMissile,
        CompanionFireball
    }

    public enum SlashElementKind
    {
        Lightning,
        Fire,
        Ice,
        Poison,
        TechnoBlade
    }

    public enum CompanionSkillKind
    {
        FlameBottle,
        PoisonBottle,
        Missile,
        Fireball
    }
}
