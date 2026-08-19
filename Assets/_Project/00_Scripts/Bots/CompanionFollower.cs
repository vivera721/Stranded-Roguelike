using UnityEngine;

namespace StrandedRoguelike
{
    public sealed class CompanionFollower : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 followOffset = new Vector2(-0.75f, 0.65f);
        [SerializeField, Min(0.1f)] private float followSpeed = 8f;
        [SerializeField, Min(0f)] private float bobAmplitude = 0.08f;
        [SerializeField, Min(0f)] private float bobSpeed = 4f;

        private Vector3 baseScale;

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        private void Update()
        {
            FindTargetIfNeeded();

            if (target == null)
            {
                return;
            }

            Vector3 desired = target.position + (Vector3)followOffset;
            desired.z = transform.position.z;
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));

            float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            transform.localScale = baseScale * (1f + bob);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void FindTargetIfNeeded()
        {
            if (target != null)
            {
                return;
            }

            PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
            if (playerMovement != null)
            {
                target = playerMovement.transform;
            }
        }
    }
}
