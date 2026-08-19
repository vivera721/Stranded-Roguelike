using UnityEngine;

namespace StrandedRoguelike
{
    public enum BossHandSide
    {
        left, right
    }

    public sealed class ReaperBossAnimationRelay : MonoBehaviour
    {
        [SerializeField] private BossHandSide handSide;

        private ReaperBossController boss;

        private void Awake()
        {
            boss = GetComponentInParent<ReaperBossController>();
        }

        public void AE_DestroyBoss()
        {
            Destroy(gameObject);
        }

        public void AE_HandSlamShockWave()
        {
            if (boss == null) return;

            switch(boss.CurrentAttackType)
            {
                case ReaperAttackType.LeftHandSlam:

                    if (handSide == BossHandSide.left)
                    {
                        boss.Anim_LeftHandSlamShockwave();
                    }
                    break;
                case ReaperAttackType.RightHandSlam:

                    if (handSide == BossHandSide.right)
                    {
                        boss.Anim_RightHandSlamShockwave();
                    }
                    break;
                case ReaperAttackType.BothHandSlam:

                    if (handSide == BossHandSide.left)
                    {
                        boss.Anim_DoubleHandSlamShockwave();
                    }
                    break;
            }
        }

        public void AE_LaserGroundPattern()
        {
            if (boss == null) return;

            if (boss.CurrentAttackType != ReaperAttackType.Laser)
                return;

            if (handSide != BossHandSide.left)
                return;

            boss.Anim_DoubleHandLaserGroundPattern();
        }

        public void AE_HandSwipe()
        {
            if (boss == null)
            {
                Debug.LogWarning($"{name} AE_HandSwipe: boss is null");
                return;
            }

            Debug.Log($"AE_HandSwipe 호출됨 / handSide: {handSide} / CurrentAttackType: {boss.CurrentAttackType}");

            switch (boss.CurrentAttackType)
            {
                case ReaperAttackType.LeftSwipe:

                    if (handSide == BossHandSide.left)
                    {
                        boss.Anim_SweepLowerLeftToRight();
                    }
                    break;
                case ReaperAttackType.RightSwipe:

                    if (handSide == BossHandSide.right)
                    {
                        boss.Anim_SweepUpperRightToLeft();
                    }
                    break;
            }
        }
    }
}