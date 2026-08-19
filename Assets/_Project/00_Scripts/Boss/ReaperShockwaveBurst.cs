using System.Collections;
using UnityEngine;

namespace StrandedRoguelike
{
    public sealed class ReaperShockwaveBurst : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float warningTime = 0.12f;
        [SerializeField, Min(0.01f)] private float damageRadius = 0.45f;
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField, Min(0.01f)] private float lifeTime = 0.45f;

        private Vector2 knockbackOrigin;
        private Coroutine burstRoutine;

        public void Setup(float radius, int burstDamage, float delay, Vector2 origin)
        {
            damageRadius = radius;
            damage = burstDamage;
            warningTime = delay;
            knockbackOrigin = origin;
            
            if (burstRoutine != null)
            {
                StopCoroutine(burstRoutine);
            }

            burstRoutine = StartCoroutine(BurstRoutine());
        }

        private IEnumerator BurstRoutine()
        {
            if (warningTime > 0f)
            {
                EnemyAttackWarning.ShowCircle(transform.position, damageRadius, warningTime);
                yield return new WaitForSeconds(warningTime);
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, damageRadius);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].TryGetComponent(out PlayerHealth playerHealth))
                {
                    playerHealth.TakeDamageFromAttacker(damage, knockbackOrigin);
                }
            }

            yield return new WaitForSeconds(lifeTime);
            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.65f, 0f, 1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, damageRadius);
        }
    }
}
