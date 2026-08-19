using System;
using UnityEngine;

namespace StrandedRoguelike
{
    public enum SurvivorDamageKind
    {
        Direct,
        StatusTick,
        ChainLightning
    }

    public static class SurvivorDamageEvents
    {
        public static event Action<EnemyHealth, int, Vector2, SurvivorDamageKind> EnemyDamaged;
        public static event Action<EnemyHealth> EnemyDied;

        public static void RaiseEnemyDamaged(EnemyHealth enemy, int damage, Vector2 hitOrigin, SurvivorDamageKind damageKind = SurvivorDamageKind.Direct)
        {
            if (enemy == null || damage <= 0)
            {
                return;
            }

            EnemyDamaged?.Invoke(enemy, damage, hitOrigin, damageKind);
        }

        public static void RaiseEnemyDied(EnemyHealth enemy)
        {
            if (enemy == null)
            {
                return;
            }

            EnemyDied?.Invoke(enemy);
        }
    }
}
