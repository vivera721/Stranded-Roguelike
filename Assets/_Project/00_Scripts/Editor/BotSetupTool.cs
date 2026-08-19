#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace StrandedRoguelike.EditorTools
{
    public static class BotSetupTool
    {
        private const string PoisonBottlePath = "Assets/_Project/05_Prefabs/Poison Bottle.prefab";
        private const string FlameBottlePath = "Assets/_Project/05_Prefabs/Flame Bottle.prefab";

        [MenuItem("Stranded Roguelike/Bots/Setup Selected As Gun Bot")]
        public static void SetupSelectedAsGunBot()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogWarning("Select a Gun Bot object first.");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(selected, "Setup Gun Bot");

            CompanionFollower follower = selected.GetComponent<CompanionFollower>();
            if (follower == null)
            {
                follower = Undo.AddComponent<CompanionFollower>(selected);
            }

            GunBotController gunBot = selected.GetComponent<GunBotController>();
            if (gunBot == null)
            {
                gunBot = Undo.AddComponent<GunBotController>(selected);
            }

            SerializedObject serializedGunBot = new SerializedObject(gunBot);
            serializedGunBot.FindProperty("poisonBottlePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PoisonBottlePath);
            serializedGunBot.FindProperty("flameBottlePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(FlameBottlePath);
            serializedGunBot.ApplyModifiedProperties();

            EditorUtility.SetDirty(selected);
            Debug.Log($"Setup Gun Bot on {selected.name}");
        }

        [MenuItem("Stranded Roguelike/Bots/Setup Selected As Companion Bot")]
        public static void SetupSelectedAsCompanionBot()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogWarning("Select a Companion Bot object first.");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(selected, "Setup Companion Bot");

            CompanionFollower follower = selected.GetComponent<CompanionFollower>();
            if (follower == null)
            {
                follower = Undo.AddComponent<CompanionFollower>(selected);
            }

            CompanionBotController companionBot = selected.GetComponent<CompanionBotController>();
            if (companionBot == null)
            {
                companionBot = Undo.AddComponent<CompanionBotController>(selected);
            }

            EditorUtility.SetDirty(selected);
            Debug.Log($"Setup Companion Bot on {selected.name}");
        }
    }
}
#endif
