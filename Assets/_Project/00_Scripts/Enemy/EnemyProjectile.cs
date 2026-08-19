using UnityEngine;

namespace StrandedRoguelike
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyProjectile : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float speed = 5f;
        [SerializeField, Min(0.01f)] private float lifeTime = 4f;
        [SerializeField, Min(1)] private int damage = 1;

        [SerializeField] private LayerMask destroyLayers;
        [SerializeField] private LayerMask bounceLayers;
        [SerializeField, Min(0)] private int maxBounceCount;

        private Vector2 direction = Vector2.right;
        private float lifeTimer;
        private int bounceCount;
        private Rigidbody2D body;

        public void Launch(Vector2 launchDirection, float launchSpeed, int launchDamage, float projectileLifeTime)
        {
            Launch(launchDirection, launchSpeed, launchDamage, projectileLifeTime, 0, 0);
        }

        public void Launch(
            Vector2 launchDirection,
            float launchSpeed,
            int launchDamage,
            float projectileLifeTime,
            LayerMask launchBounceLayers,
            int launchMaxBounceCount)
        {
            direction = launchDirection == Vector2.zero ? Vector2.right : launchDirection.normalized;
            speed = launchSpeed;
            damage = launchDamage;
            lifeTime = projectileLifeTime;
            lifeTimer = lifeTime;
            bounceLayers = launchMaxBounceCount > 0
                ? ResolveBounceLayers(launchBounceLayers)
                : launchBounceLayers;
            maxBounceCount = launchMaxBounceCount;
            bounceCount = 0;

            UpdateRotation();
        }

        private void Awake()
        {
            Collider2D projectileCollider = GetComponent<Collider2D>();
            projectileCollider.isTrigger = true;

            if (!TryGetComponent(out body))
            {
                body = gameObject.AddComponent<Rigidbody2D>();
            }

            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.simulated = true;

            lifeTimer = lifeTime;
        }

        private void Update()
        {
            lifeTimer -= Time.deltaTime;

            if (lifeTimer <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void FixedUpdate()
        {
            body.MovePosition(body.position + direction * (speed * Time.fixedDeltaTime));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (((1 << other.gameObject.layer) & bounceLayers) != 0)
            {
                if (bounceCount < maxBounceCount)
                {
                    Bounce(other);
                    return;
                }

                Destroy(gameObject);
                return;
            }

            if(((1 << other.gameObject.layer) & destroyLayers) != 0)
            {
                Destroy(gameObject);
                return;
            }

            if(!other.TryGetComponent(out PlayerHealth playerHealth))
            {
                return;
            }

            playerHealth.TakeDamageFromProjectile(damage, direction);
            Destroy(gameObject);
        }

        private void Bounce(Collider2D other)
        {
            bounceCount++;

            Vector2 closestPoint = other.ClosestPoint(transform.position);
            Vector2 normal = (Vector2)transform.position - closestPoint;

            if (normal.sqrMagnitude < 0.0001f)
            {
                normal = Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)
                    ? new Vector2(-Mathf.Sign(direction.x), 0f)
                    : new Vector2(0f, -Mathf.Sign(direction.y));
            }

            direction = Vector2.Reflect(direction, normal.normalized).normalized;
            transform.position += (Vector3)(direction * 0.05f);
            UpdateRotation();
        }

        private static LayerMask ResolveBounceLayers(LayerMask requestedLayers)
        {
            if (requestedLayers.value != 0)
            {
                return requestedLayers;
            }

            int wallLayer = LayerMask.NameToLayer("Wall");
            return wallLayer < 0 ? 0 : 1 << wallLayer;
        }

        private void UpdateRotation()
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
