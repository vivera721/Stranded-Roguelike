using System.Collections.Generic;
using UnityEngine;

namespace StrandedRoguelike
{
    [RequireComponent(typeof(PlayerAttack))]
    public sealed class PlayerAttackElectricAugment : MonoBehaviour
    {
        [SerializeField] private bool electricChainEnabled = true;
        [SerializeField, Min(0)] private int chainDamage = 1;
        [SerializeField, Min(0)] private int maxChainTargets = 3;
        [SerializeField, Min(0f)] private float chainRadius = 2.8f;
        [SerializeField, Min(0f)] private float visualDuration = 0.08f;
        [SerializeField] private Color chainColor = new Color(0.25f, 0.9f, 1f, 0.9f);

        private PlayerAttack playerAttack;

        private void Awake()
        {
            playerAttack = GetComponent<PlayerAttack>();
        }

        private void OnEnable()
        {
            if (playerAttack != null)
            {
                playerAttack.EnemyHit += OnEnemyHit;
            }
        }

        private void OnDisable()
        {
            if (playerAttack != null)
            {
                playerAttack.EnemyHit -= OnEnemyHit;
            }
        }

        public void SetElectricChainEnabled(bool enabled)
        {
            electricChainEnabled = enabled;
        }

        private void OnEnemyHit(EnemyHealth firstTarget, Vector2 attackDirection)
        {
            if (!electricChainEnabled || firstTarget == null || maxChainTargets <= 0 || chainDamage <= 0)
            {
                return;
            }

            List<EnemyHealth> chained = new List<EnemyHealth> { firstTarget };
            Vector2 currentPosition = firstTarget.transform.position;

            for (int i = 0; i < maxChainTargets; i++)
            {
                EnemyHealth nextTarget = FindNearestUnchainedEnemy(currentPosition, chained);
                if (nextTarget == null)
                {
                    break;
                }

                DrawLightning(currentPosition, nextTarget.transform.position);
                nextTarget.TakeDamage(chainDamage, currentPosition);
                chained.Add(nextTarget);
                currentPosition = nextTarget.transform.position;
            }
        }

        private EnemyHealth FindNearestUnchainedEnemy(Vector2 origin, List<EnemyHealth> excluded)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, chainRadius);
            EnemyHealth nearest = null;
            float nearestSqrDistance = chainRadius * chainRadius;

            for (int i = 0; i < hits.Length; i++)
            {
                if (!hits[i].TryGetComponent(out EnemyHealth enemy) || enemy.isDead || excluded.Contains(enemy))
                {
                    continue;
                }

                float sqrDistance = ((Vector2)enemy.transform.position - origin).sqrMagnitude;
                if (sqrDistance <= nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        private void DrawLightning(Vector2 start, Vector2 end)
        {
            GameObject lineObject = new GameObject("Electric Chain");
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.positionCount = 4;
            line.useWorldSpace = true;
            line.startWidth = 0.05f;
            line.endWidth = 0.02f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = chainColor;
            line.endColor = Color.white;

            Vector2 direction = (end - start).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x);
            line.SetPosition(0, start);
            line.SetPosition(1, Vector2.Lerp(start, end, 0.33f) + normal * Random.Range(-0.12f, 0.12f));
            line.SetPosition(2, Vector2.Lerp(start, end, 0.66f) + normal * Random.Range(-0.12f, 0.12f));
            line.SetPosition(3, end);

            Destroy(lineObject, visualDuration);
        }
    }
}
