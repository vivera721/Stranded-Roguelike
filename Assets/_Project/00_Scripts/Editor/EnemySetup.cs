#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace StrandedRoguelike.Editor
{
    public static class EnemySetup
    {
        private const string Root = "Assets/_Project";
        private const string Generated = Root + "/Generated/Enemies";
        private const string PrefabRoot = Root + "/05_Prefabs/Enemies";
        private const string TribeRoot = Root + "/_Arts/Stranded Enemy Pack/Enemies";
        private const string InsectRoot = Root + "/_Arts/STRANDED - Insect Enemy Pack";

        [InitializeOnLoadMethod]
        private static void CreateEnemiesOnFirstImport()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                if (AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/Tribe_Warrior.prefab") == null)
                {
                    CreateEnemies();
                }
            };
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode &&
                AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/Tribe_Warrior.prefab") == null)
            {
                EditorApplication.delayCall += CreateEnemies;
            }
        }

        [MenuItem("Tools/Stranded Roguelike/Create Enemies")]
        public static void CreateEnemies()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/05_Prefabs/Projectiles/Sojourn_Bullet_Yellow.prefab") == null)
            {
                BulletVisualSetup.CreateBulletVisuals();
            }

            EnsureFolder(Root, "Generated");
            EnsureFolder(Root + "/Generated", "Enemies");
            EnsureFolder(Root, "05_Prefabs");
            EnsureFolder(Root + "/05_Prefabs", "Enemies");

            CreateTribeWarrior();
            CreateTribeHunter();
            CreateTribeTamedBeast();
            CreateInsect(
                "Small_Insect",
                InsectRoot + "/Small Bug/Small insect - idle move.png",
                InsectRoot + "/Small Bug/Small Insect-Attack.png",
                EnemyAttackKind.Melee,
                1.2f,
                1.2f,
                2.6f,
                2,
                1);
            CreateInsect(
                "Medium_Insect",
                InsectRoot + "/Medium Bug/Medium Insect-idleMove.png",
                InsectRoot + "/Medium Bug/Medium Insect-Attack.png",
                EnemyAttackKind.MeleeArea,
                1.6f,
                1.4f,
                2.2f,
                4,
                1);
            CreateInsect(
                "Medium2_Insect",
                InsectRoot + "/Medium bug 2/Medium2 bug-Idle Move.png",
                InsectRoot + "/Medium bug 2/Medium2 bug-Attack.png",
                EnemyAttackKind.RangedProjectile,
                5.5f,
                4.2f,
                2.0f,
                4,
                1,
                projectileCount: 5);
            CreateInsect(
                "Big_Insect",
                InsectRoot + "/Big Bug/Big Insect-moveidle.png",
                InsectRoot + "/Big Bug/Big Insect-Attack.png",
                EnemyAttackKind.RangedArea,
                4.5f,
                3.4f,
                1.6f,
                8,
                2);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Enemy prefabs created.");
        }

        private static void CreateTribeWarrior()
        {
            string folder = TribeRoot + "/Tribe Warrior";
            AnimationClip idleDown = CreateClip("Tribe_Warrior_Idle_Down", folder + "/Tribe Warrior-idle down.png", 6f);
            AnimationClip idleUp = CreateClip("Tribe_Warrior_Idle_Up", folder + "/Tribe Warrior-idle up.png", 6f);
            AnimationClip idleSide = CreateClip("Tribe_Warrior_Idle_Side", folder + "/Tribe Warrior-Idle.png", 6f);
            AnimationClip moveDown = CreateClip("Tribe_Warrior_Move_Down", folder + "/Tribe Warrior-Walk Down.png", 10f);
            AnimationClip moveUp = CreateClip("Tribe_Warrior_Move_Up", folder + "/Tribe Warrior-Walk up.png", 10f);
            AnimationClip moveSide = CreateClip("Tribe_Warrior_Move_Side", folder + "/Tribe Warrior-Walk.png", 10f);
            AnimationClip attackDown = CreateClip("Tribe_Warrior_Attack_Down", folder + "/Tribe Warrior-attack down.png", 12f, false);
            AnimationClip attackUp = CreateClip("Tribe_Warrior_Attack_Up", folder + "/Tribe Warrior-Attack Up.png", 12f, false);
            AnimationClip attackSide = CreateClip("Tribe_Warrior_Attack_Side", folder + "/Tribe Warrior-attack.png", 12f, false);

            AnimatorController controller = CreateFourDirectionController(
                "Tribe_Warrior",
                idleDown,
                idleUp,
                idleSide,
                moveDown,
                moveUp,
                moveSide,
                attackDown,
                attackUp,
                attackSide);

            CreateEnemyPrefab("Tribe_Warrior", controller, idleDown, EnemyAttackKind.Melee, 1.25f, 1.0f, 2.1f, 4, 1);
        }

        private static void CreateTribeHunter()
        {
            string folder = TribeRoot + "/Tribe Hunter";
            AnimationClip idleDown = CreateClip("Tribe_Hunter_Idle_Down", folder + "/Tribe Hunter-Idle Down.png", 6f);
            AnimationClip idleUp = CreateClip("Tribe_Hunter_Idle_Up", folder + "/Tribe Hunter-Idle Up.png", 6f);
            AnimationClip idleSide = CreateClip("Tribe_Hunter_Idle_Side", folder + "/Tribe Hunter-idle.png", 6f);
            AnimationClip moveDown = CreateClip("Tribe_Hunter_Move_Down", folder + "/Tribe Hunter-Walk Down.png", 10f);
            AnimationClip moveUp = CreateClip("Tribe_Hunter_Move_Up", folder + "/Tribe Hunter-Walk Up.png", 10f);
            AnimationClip moveSide = CreateClip("Tribe_Hunter_Move_Side", folder + "/Tribe Hunter-walk.png", 10f);
            AnimationClip attackDown = CreateClip("Tribe_Hunter_Shoot_Down", folder + "/Tribe Hunter-Shoot down.png", 12f, false);
            AnimationClip attackUp = CreateClip("Tribe_Hunter_Shoot_Up", folder + "/Tribe Hunter-Shoot Up.png", 12f, false);
            AnimationClip attackSide = CreateClip("Tribe_Hunter_Shoot_Side", folder + "/Tribe Hunter-Shoot.png", 12f, false);

            AnimatorController controller = CreateFourDirectionController(
                "Tribe_Hunter",
                idleDown,
                idleUp,
                idleSide,
                moveDown,
                moveUp,
                moveSide,
                attackDown,
                attackUp,
                attackSide);

            CreateEnemyPrefab("Tribe_Hunter", controller, idleDown, EnemyAttackKind.RangedProjectile, 6f, 4.5f, 1.9f, 3, 1, projectileCount: 3, spreadAngle: 18f);
        }

        private static void CreateTribeTamedBeast()
        {
            string folder = TribeRoot + "/Tribe Tamed Beast";
            AnimationClip idleDown = CreateClip("Tribe_Tamed_Beast_Idle_Down", folder + "/Tribe Tamed Beast-Down Idle.png", 6f);
            AnimationClip idleUp = CreateClip("Tribe_Tamed_Beast_Idle_Up", folder + "/Tribe Tamed Beast-Up Idle.png", 6f);
            AnimationClip idleSide = CreateClip("Tribe_Tamed_Beast_Idle_Side", folder + "/Tribe Tamed Beast-Idle.png", 6f);
            AnimationClip moveDown = CreateClip("Tribe_Tamed_Beast_Move_Down", folder + "/Tribe Tamed Beast-Move Down.png", 10f);
            AnimationClip moveUp = CreateClip("Tribe_Tamed_Beast_Move_Up", folder + "/Tribe Tamed Beast-Move Up.png", 10f);
            AnimationClip moveSide = CreateClip("Tribe_Tamed_Beast_Move_Side", folder + "/Tribe Tamed Beast-move lr.png", 10f);
            AnimationClip attackDown = CreateClip("Tribe_Tamed_Beast_Attack_Down", folder + "/Tribe Tamed Beast-Attack Down.png", 12f, false);
            AnimationClip attackUp = CreateClip("Tribe_Tamed_Beast_Attack_Up", folder + "/Tribe Tamed Beast-Attack Up.png", 12f, false);
            AnimationClip attackSide = CreateClip("Tribe_Tamed_Beast_Attack_Side", folder + "/Tribe Tamed Beast - Attack Left Right.png", 12f, false);

            AnimatorController controller = CreateFourDirectionController(
                "Tribe_Tamed_Beast",
                idleDown,
                idleUp,
                idleSide,
                moveDown,
                moveUp,
                moveSide,
                attackDown,
                attackUp,
                attackSide);

            CreateEnemyPrefab("Tribe_Tamed_Beast", controller, idleDown, EnemyAttackKind.MeleeArea, 1.5f, 1.1f, 1.8f, 7, 2);
        }

        private static void CreateInsect(
            string name,
            string idleMovePath,
            string attackPath,
            EnemyAttackKind attackKind,
            float attackRange,
            float stopDistance,
            float moveSpeed,
            int health,
            int damage,
            int projectileCount = 1,
            float spreadAngle = 25f)
        {
            AnimationClip idleMove = CreateClip(name + "_IdleMove", idleMovePath, 8f);
            AnimationClip attack = CreateClip(name + "_Attack", attackPath, 12f, false);
            AnimatorController controller = CreateSingleDirectionController(name, idleMove, attack);
            CreateEnemyPrefab(name, controller, idleMove, attackKind, attackRange, stopDistance, moveSpeed, health, damage, projectileCount, spreadAngle);
        }

        private static void CreateEnemyPrefab(
            string name,
            RuntimeAnimatorController controller,
            AnimationClip firstClip,
            EnemyAttackKind attackKind,
            float attackRange,
            float stopDistance,
            float moveSpeed,
            int health,
            int damage,
            int projectileCount = 1,
            float spreadAngle = 25f)
        {
            GameObject enemy = new GameObject(name);
            SpriteRenderer renderer = enemy.AddComponent<SpriteRenderer>();
            renderer.sprite = FirstSprite(firstClip);
            renderer.sortingOrder = 10;

            Rigidbody2D body = enemy.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            CapsuleCollider2D collider = enemy.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.55f, 0.65f);
            collider.offset = new Vector2(0f, -0.05f);

            Animator animator = enemy.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            EnemyHealth enemyHealth = enemy.AddComponent<EnemyHealth>();
            SetSerialized(enemyHealth, "maxHealth", health);

            EnemyController enemyController = enemy.AddComponent<EnemyController>();
            GameObject bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/05_Prefabs/Projectiles/Sojourn_Bullet_Yellow.prefab");
            SetSerialized(enemyController, "attackKind", attackKind);
            SetSerialized(enemyController, "attackRange", attackRange);
            SetSerialized(enemyController, "stopDistance", stopDistance);
            SetSerialized(enemyController, "moveSpeed", moveSpeed);
            SetSerialized(enemyController, "damage", damage);
            SetSerialized(enemyController, "projectileCount", projectileCount);
            SetSerialized(enemyController, "spreadAngle", spreadAngle);
            SetSerialized(enemyController, "radialProjectilePattern", projectileCount >= 8);
            SetSerialized(enemyController, "projectilePrefab", bulletPrefab);

            PrefabUtility.SaveAsPrefabAsset(enemy, $"{PrefabRoot}/{name}.prefab");
            Object.DestroyImmediate(enemy);
        }

        private static AnimatorController CreateFourDirectionController(
            string name,
            AnimationClip idleDown,
            AnimationClip idleUp,
            AnimationClip idleSide,
            AnimationClip moveDown,
            AnimationClip moveUp,
            AnimationClip moveSide,
            AnimationClip attackDown,
            AnimationClip attackUp,
            AnimationClip attackSide)
        {
            AnimatorController controller = CreateBaseController(name);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            AnimatorState idle = machine.AddState("Idle");
            AnimatorState move = machine.AddState("Move");
            AnimatorState attack = machine.AddState("Attack");
            idle.motion = CreateDirectionalTree(controller, name + "_Idle_Tree", idleDown, idleUp, idleSide);
            move.motion = CreateDirectionalTree(controller, name + "_Move_Tree", moveDown, moveUp, moveSide);
            attack.motion = CreateDirectionalTree(controller, name + "_Attack_Tree", attackDown, attackUp, attackSide);
            machine.defaultState = idle;

            AddCommonTransitions(machine, idle, move, attack);
            return controller;
        }

        private static AnimatorController CreateSingleDirectionController(
            string name,
            AnimationClip idleMove,
            AnimationClip attackClip)
        {
            AnimatorController controller = CreateBaseController(name);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            AnimatorState idle = machine.AddState("IdleMove");
            AnimatorState attack = machine.AddState("Attack");
            idle.motion = idleMove;
            attack.motion = attackClip;
            machine.defaultState = idle;

            AnimatorStateTransition toAttack = machine.AddAnyStateTransition(attack);
            toAttack.hasExitTime = false;
            toAttack.duration = 0f;
            toAttack.canTransitionToSelf = false;
            toAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

            AnimatorStateTransition attackToIdle = attack.AddTransition(idle);
            attackToIdle.hasExitTime = true;
            attackToIdle.exitTime = 1f;
            attackToIdle.duration = 0f;
            return controller;
        }

        private static AnimatorController CreateBaseController(string name)
        {
            string path = $"{Generated}/{name}.controller";
            AssetDatabase.DeleteAsset(path);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            return controller;
        }

        private static void AddCommonTransitions(AnimatorStateMachine machine, AnimatorState idle, AnimatorState move, AnimatorState attack)
        {
            AnimatorStateTransition toMove = idle.AddTransition(move);
            toMove.hasExitTime = false;
            toMove.duration = 0f;
            toMove.AddCondition(AnimatorConditionMode.Greater, 0.01f, "Speed");

            AnimatorStateTransition toIdle = move.AddTransition(idle);
            toIdle.hasExitTime = false;
            toIdle.duration = 0f;
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.01f, "Speed");

            AnimatorStateTransition toAttack = machine.AddAnyStateTransition(attack);
            toAttack.hasExitTime = false;
            toAttack.duration = 0f;
            toAttack.canTransitionToSelf = false;
            toAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

            AnimatorStateTransition attackToIdle = attack.AddTransition(idle);
            attackToIdle.hasExitTime = true;
            attackToIdle.exitTime = 1f;
            attackToIdle.duration = 0f;
        }

        private static BlendTree CreateDirectionalTree(AnimatorController controller, string name, AnimationClip down, AnimationClip up, AnimationClip side)
        {
            BlendTree tree = new BlendTree
            {
                name = name,
                blendType = BlendTreeType.SimpleDirectional2D,
                blendParameter = "MoveX",
                blendParameterY = "MoveY",
                useAutomaticThresholds = false
            };

            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.AddChild(down, Vector2.down);
            tree.AddChild(up, Vector2.up);
            tree.AddChild(side, Vector2.right);
            tree.AddChild(side, Vector2.left);
            return tree;
        }

        private static AnimationClip CreateClip(string name, string spritePath, float frameRate, bool loop = true)
        {
            string clipPath = $"{Generated}/{name}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = name };
                AssetDatabase.CreateAsset(clip, clipPath);
            }

            clip.frameRate = frameRate;
            Sprite[] sprites = LoadSprites(spritePath);
            ObjectReferenceKeyframe[] frames = new ObjectReferenceKeyframe[sprites.Length];

            for (int i = 0; i < sprites.Length; i++)
            {
                frames[i] = new ObjectReferenceKeyframe
                {
                    time = i / frameRate,
                    value = sprites[i]
                };
            }

            EditorCurveBinding binding = new EditorCurveBinding
            {
                path = string.Empty,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static Sprite[] LoadSprites(string path)
        {
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => SpriteIndex(sprite.name))
                .ToArray();

            if (sprites.Length == 0)
            {
                throw new FileNotFoundException($"No sliced sprites found: {path}");
            }

            return sprites;
        }

        private static Sprite FirstSprite(AnimationClip clip)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            ObjectReferenceKeyframe[] frames = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
            return frames[0].value as Sprite;
        }

        private static int SpriteIndex(string name)
        {
            int underscore = name.LastIndexOf('_');
            return underscore >= 0 && int.TryParse(name[(underscore + 1)..], out int index)
                ? index
                : 0;
        }

        private static void SetSerialized(Object target, string propertyName, object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                return;
            }

            switch (value)
            {
                case int intValue:
                    property.intValue = intValue;
                    break;

                case float floatValue:
                    property.floatValue = floatValue;
                    break;

                case bool boolValue:
                    property.boolValue = boolValue;
                    break;

                case EnemyAttackKind attackKind:
                    property.enumValueIndex = (int)attackKind;
                    break;

                case Object objectValue:
                    property.objectReferenceValue = objectValue;
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
#endif
