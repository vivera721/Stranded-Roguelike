#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StrandedRoguelike.Editor
{
    public static class PlayerSetup
    {
        private const string Root = "Assets/_Project";
        private const string Generated = Root + "/Generated/Player";
        private const string PrefabPath = Root + "/05_Prefabs/Player.prefab";
        private const string ArtRoot = Root + "/_Arts/Stranded 04 - Hero sprite/With Sword";

        private const string IdleDown = ArtRoot + "/Down/04 Stranded - Pack 4 back up-Idle Down.png";
        private const string IdleUp = ArtRoot + "/Up/04 Stranded - Pack 4 back up-Idle Up.png";
        private const string IdleSide = ArtRoot + "/Right Left/Idle left right.png";
        private const string MoveDown = ArtRoot + "/Down/04 Stranded - Pack 4 back up-Move Down.png";
        private const string MoveUp = ArtRoot + "/Up/04 Stranded - Pack 4 back up-Move Up.png";
        private const string MoveSide = ArtRoot + "/Right Left/R move.png";
        private const string AttackNoneVfx = ArtRoot + "/None_VFX_Attack_None_BG.png";

        [InitializeOnLoadMethod]
        private static void CreatePlayerOnFirstImport()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                if (AssetDatabase.LoadAssetAtPath<AnimationClip>($"{Generated}/Player_Attack_DownRight.anim") == null ||
                    AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null ||
                    GameObject.Find("Player") == null)
                {
                    CreatePlayer();
                }
                else
                {
                    EnsureExistingPlayerComponents();
                }

                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                {
                    EditorSceneManager.SaveOpenScenes();
                }
            };
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode &&
                AssetDatabase.LoadAssetAtPath<AnimationClip>($"{Generated}/Player_Attack_DownRight.anim") == null)
            {
                EditorApplication.delayCall += CreatePlayer;
            }
        }

        [MenuItem("Tools/Stranded Roguelike/Create 4-Direction Player")]
        public static void CreatePlayer()
        {
            EnsureFolder(Root, "Generated");
            EnsureFolder(Root + "/Generated", "Player");

            AnimationClip idleDown = CreateClip("Player_Idle_Down", IdleDown, 6f);
            AnimationClip idleUp = CreateClip("Player_Idle_Up", IdleUp, 6f);
            AnimationClip idleSide = CreateClip("Player_Idle_Side", IdleSide, 6f);
            AnimationClip moveDown = CreateClip("Player_Move_Down", MoveDown, 10f);
            AnimationClip moveUp = CreateClip("Player_Move_Up", MoveUp, 10f);
            AnimationClip moveSide = CreateClip("Player_Move_Side", MoveSide, 10f);
            AnimationClip attackDown = CreateClip(
                "Player_Attack_Down", AttackNoneVfx, 12f, false, 0, 5);
            AnimationClip attackDownRight = CreateClip(
                "Player_Attack_DownRight", AttackNoneVfx, 12f, false, 5, 5);
            AnimationClip attackSide = CreateClip(
                "Player_Attack_Side", AttackNoneVfx, 12f, false, 10, 5);
            AnimationClip attackUp = CreateClip(
                "Player_Attack_Up", AttackNoneVfx, 12f, false, 20, 5);
            AnimationClip attackUpRight = CreateClip(
                "Player_Attack_UpRight", AttackNoneVfx, 12f, false, 25, 5);

            AnimatorController controller = CreateController(
                idleDown, idleUp, idleSide,
                moveDown, moveUp, moveSide,
                attackDown, attackDownRight, attackSide, attackUp, attackUpRight);

            GameObject existing = GameObject.Find("Player");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            GameObject player = new GameObject("Player");
            SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprites(IdleDown).First();

            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            CapsuleCollider2D collider = player.AddComponent<CapsuleCollider2D>();
            collider.direction = CapsuleDirection2D.Vertical;
            collider.size = new Vector2(0.35f, 0.55f);
            collider.offset = new Vector2(0f, -0.05f);

            Animator animator = player.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            player.AddComponent<PlayerMovement>();
            player.AddComponent<PlayerHealth>();
            player.AddComponent<SlashVFX>();
            player.AddComponent<PlayerAttack>();

            PrefabUtility.SaveAsPrefabAssetAndConnect(
                player,
                PrefabPath,
                InteractionMode.AutomatedAction);

            Selection.activeGameObject = player;
            EditorSceneManager.MarkSceneDirty(player.scene);
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("4방향 이동 및 검 공격 Player 생성 완료");
        }

        private static void EnsureExistingPlayerComponents()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                return;
            }

            if (player.GetComponent<PlayerMovement>() == null)
            {
                player.AddComponent<PlayerMovement>();
                EditorSceneManager.MarkSceneDirty(player.scene);
            }

            if (player.GetComponent<PlayerHealth>() == null)
            {
                player.AddComponent<PlayerHealth>();
                EditorSceneManager.MarkSceneDirty(player.scene);
            }
        }

        private static AnimationClip CreateClip(
            string name,
            string spritePath,
            float frameRate,
            bool loop = true,
            int startIndex = 0,
            int frameCount = -1)
        {
            string clipPath = $"{Generated}/{name}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = name };
                AssetDatabase.CreateAsset(clip, clipPath);
            }

            clip.frameRate = frameRate;
            Sprite[] allSprites = LoadSprites(spritePath);
            Sprite[] sprites = frameCount < 0
                ? allSprites
                : allSprites.Skip(startIndex).Take(frameCount).ToArray();
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

        private static AnimatorController CreateController(
            AnimationClip idleDown,
            AnimationClip idleUp,
            AnimationClip idleSide,
            AnimationClip moveDown,
            AnimationClip moveUp,
            AnimationClip moveSide,
            AnimationClip attackDown,
            AnimationClip attackDownRight,
            AnimationClip attackSide,
            AnimationClip attackUp,
            AnimationClip attackUpRight)
        {
            string path = $"{Generated}/Player.controller";
            AssetDatabase.DeleteAsset(path);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idle = machine.AddState("Idle");
            AnimatorState move = machine.AddState("Move");
            AnimatorState attack = machine.AddState("Attack");
            idle.motion = CreateDirectionalTree(controller, "Idle Direction", idleDown, idleUp, idleSide);
            move.motion = CreateDirectionalTree(controller, "Move Direction", moveDown, moveUp, moveSide);
            attack.motion = CreateDirectionalTree(
                controller,
                "Attack Direction",
                attackDown,
                attackDownRight,
                attackUp,
                attackUpRight,
                attackSide);
            machine.defaultState = idle;

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
            return controller;
        }

        private static BlendTree CreateDirectionalTree(
            AnimatorController controller,
            string name,
            AnimationClip down,
            AnimationClip up,
            AnimationClip side)
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

        private static BlendTree CreateDirectionalTree(
            AnimatorController controller,
            string name,
            AnimationClip down,
            AnimationClip downRight,
            AnimationClip up,
            AnimationClip upRight,
            AnimationClip side)
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

            float diagonal = 0.7071068f;
            tree.AddChild(down, Vector2.down);
            tree.AddChild(downRight, new Vector2(diagonal, -diagonal));
            tree.AddChild(downRight, new Vector2(-diagonal, -diagonal));
            tree.AddChild(side, Vector2.right);
            tree.AddChild(side, Vector2.left);
            tree.AddChild(upRight, new Vector2(diagonal, diagonal));
            tree.AddChild(upRight, new Vector2(-diagonal, diagonal));
            tree.AddChild(up, Vector2.up);
            return tree;
        }

        private static Sprite[] LoadSprites(string path)
        {
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => SpriteIndex(sprite.name))
                .ToArray();

            if (sprites.Length == 0)
            {
                throw new FileNotFoundException($"슬라이스된 스프라이트를 찾지 못했습니다: {path}");
            }

            return sprites;
        }

        private static int SpriteIndex(string name)
        {
            int underscore = name.LastIndexOf('_');
            return underscore >= 0 && int.TryParse(name[(underscore + 1)..], out int index)
                ? index
                : 0;
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
