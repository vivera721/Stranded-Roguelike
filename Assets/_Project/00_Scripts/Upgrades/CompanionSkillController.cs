using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace StrandedRoguelike
{
    public sealed class CompanionSkillController : MonoBehaviour
    {
        [SerializeField] private GameObject poisonBottlePrefab;
        [SerializeField] private GameObject flameBottlePrefab;
        [SerializeField] private GameObject fireballPrefab;
        [SerializeField] private Sprite missileSprite;
        [SerializeField] private GameObject missileImpactVFXPrefab;
        [SerializeField, Min(0.5f)] private float skillRange = 9f;
        [SerializeField] private bool blockInBossRoom;
        [SerializeField] private Image[] patchSlots;
        [SerializeField] private Sprite flameBottleIcon;
        [SerializeField] private Sprite poisonBottleIcon;
        [SerializeField] private Sprite missileIcon;
        [SerializeField] private Sprite fireballIcon;

        private readonly List<CompanionSkillKind> unlockedSkills = new List<CompanionSkillKind>();
        private readonly HashSet<CompanionSkillKind> usedThisMap = new HashSet<CompanionSkillKind>();
        private InputAction skillAction;

        private void Awake()
        {
            skillAction = new InputAction("Companion Skill", InputActionType.Button, "<Keyboard>/q");
            skillAction.AddBinding("<Gamepad>/leftShoulder");
        }

        private void OnEnable()
        {
            skillAction.performed += OnSkill;
            skillAction.Enable();
        }

        private void OnDisable()
        {
            skillAction.performed -= OnSkill;
            skillAction.Disable();
        }

        private void OnDestroy()
        {
            skillAction.Dispose();
        }

        public void UnlockSkill(CompanionSkillKind skill)
        {
            if (!unlockedSkills.Contains(skill))
            {
                unlockedSkills.Add(skill);
            }

            RefreshPatchUI();
        }

        public void ResetMapUse()
        {
            usedThisMap.Clear();
            RefreshPatchUI();
        }

        public void SetBossRoomBlocked(bool blocked)
        {
            blockInBossRoom = blocked;
        }

        private void OnSkill(InputAction.CallbackContext context)
        {
            if (blockInBossRoom)
            {
                return;
            }

            CompanionSkillKind? skill = GetFirstUsableSkill();
            if (!skill.HasValue)
            {
                return;
            }

            EnemyHealth target = FindNearestEnemy();
            if (target == null)
            {
                return;
            }

            UseSkill(skill.Value, target.transform.position);
            usedThisMap.Add(skill.Value);
            RefreshPatchUI();
        }

        private CompanionSkillKind? GetFirstUsableSkill()
        {
            for (int i = 0; i < unlockedSkills.Count; i++)
            {
                if (!usedThisMap.Contains(unlockedSkills[i]))
                {
                    return unlockedSkills[i];
                }
            }

            return null;
        }

        private void UseSkill(CompanionSkillKind skill, Vector2 targetPosition)
        {
            switch (skill)
            {
                case CompanionSkillKind.FlameBottle:
                    ThrowBottle(flameBottlePrefab, targetPosition, BotProjectileKind.FlameBottle);
                    break;
                case CompanionSkillKind.PoisonBottle:
                    ThrowBottle(poisonBottlePrefab, targetPosition, BotProjectileKind.PoisonBottle);
                    break;
                case CompanionSkillKind.Missile:
                    MissileStrike.Spawn(targetPosition, 3.2f, 0.45f, 6f, 1.65f, 5, missileSprite, missileImpactVFXPrefab, true);
                    break;
                case CompanionSkillKind.Fireball:
                    FireFireball(targetPosition);
                    break;
            }
        }

        private void ThrowBottle(GameObject prefab, Vector2 targetPosition, BotProjectileKind kind)
        {
            GameObject projectileObject = prefab != null
                ? Instantiate(prefab, transform.position, Quaternion.identity)
                : CreateDefaultBottle(kind);
            projectileObject.SetActive(true);

            ArcProjectile projectile = projectileObject.GetComponent<ArcProjectile>();
            if (projectile == null)
            {
                projectile = projectileObject.AddComponent<ArcProjectile>();
            }

            ArcProjectileSettings settings = new ArcProjectileSettings
            {
                kind = kind,
                flightTime = 0.65f,
                arcHeight = 1.45f,
                impactRadius = 1.2f,
                impactDamage = 1,
                useShadow = true
            };

            projectile.Configure(settings);
            projectile.Launch(transform.position, targetPosition);
        }

        private void FireFireball(Vector2 targetPosition)
        {
            GameObject projectileObject = fireballPrefab != null
                ? Instantiate(fireballPrefab, transform.position, Quaternion.identity)
                : CreateDefaultFireball();

            projectileObject.SetActive(true);
            ArcProjectile projectile = projectileObject.GetComponent<ArcProjectile>();
            if (projectile == null)
            {
                projectile = projectileObject.AddComponent<ArcProjectile>();
            }

            projectile.Configure(new ArcProjectileSettings
            {
                kind = BotProjectileKind.Plain,
                flightTime = 0.35f,
                arcHeight = 0f,
                impactRadius = 0.95f,
                impactDamage = 2,
                useShadow = false
            });
            projectile.Launch(transform.position, targetPosition);
        }

        private EnemyHealth FindNearestEnemy()
        {
            EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            EnemyHealth nearest = null;
            float nearestSqr = skillRange * skillRange;

            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == null || enemies[i].isDead)
                {
                    continue;
                }

                float sqr = ((Vector2)enemies[i].transform.position - (Vector2)transform.position).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = enemies[i];
                }
            }

            return nearest;
        }

        private void RefreshPatchUI()
        {
            if (patchSlots == null)
            {
                return;
            }

            for (int i = 0; i < patchSlots.Length; i++)
            {
                if (patchSlots[i] == null)
                {
                    continue;
                }

                bool hasSkill = i < unlockedSkills.Count;
                patchSlots[i].enabled = hasSkill;

                if (hasSkill)
                {
                    patchSlots[i].sprite = GetIcon(unlockedSkills[i]);
                    patchSlots[i].color = usedThisMap.Contains(unlockedSkills[i])
                        ? new Color(1f, 1f, 1f, 0.35f)
                        : Color.white;
                }
            }
        }

        private Sprite GetIcon(CompanionSkillKind skill)
        {
            switch (skill)
            {
                case CompanionSkillKind.FlameBottle:
                    return flameBottleIcon != null ? flameBottleIcon : GetSpriteFromPrefab(flameBottlePrefab);
                case CompanionSkillKind.PoisonBottle:
                    return poisonBottleIcon != null ? poisonBottleIcon : GetSpriteFromPrefab(poisonBottlePrefab);
                case CompanionSkillKind.Missile:
                    return missileIcon != null ? missileIcon : missileSprite;
                case CompanionSkillKind.Fireball:
                    return fireballIcon != null ? fireballIcon : GetSpriteFromPrefab(fireballPrefab);
                default:
                    return null;
            }
        }

        private static Sprite GetSpriteFromPrefab(GameObject prefab)
        {
            return prefab != null && prefab.TryGetComponent(out SpriteRenderer renderer) ? renderer.sprite : null;
        }

        private static GameObject CreateDefaultFireball()
        {
            GameObject fireball = new GameObject("Companion Fireball");
            SpriteRenderer renderer = fireball.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateCircleSprite();
            renderer.color = new Color(1f, 0.3f, 0.05f, 1f);
            renderer.sortingOrder = 35;
            fireball.transform.localScale = Vector3.one * 0.5f;
            return fireball;
        }

        private static GameObject CreateDefaultBottle(BotProjectileKind kind)
        {
            GameObject bottle = new GameObject(kind == BotProjectileKind.PoisonBottle ? "Default Poison Bottle" : "Default Flame Bottle");
            SpriteRenderer renderer = bottle.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateCircleSprite();
            renderer.color = kind == BotProjectileKind.PoisonBottle
                ? new Color(0.25f, 1f, 0.25f, 1f)
                : new Color(1f, 0.35f, 0.08f, 1f);
            renderer.sortingOrder = 35;
            bottle.transform.localScale = new Vector3(0.35f, 0.55f, 1f);
            return bottle;
        }

        private static Sprite CreateCircleSprite()
        {
            const int size = 16;
            Texture2D texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;
            Vector2 center = new Vector2(size - 1, size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    texture.SetPixel(x, y, distance <= 6f ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}
