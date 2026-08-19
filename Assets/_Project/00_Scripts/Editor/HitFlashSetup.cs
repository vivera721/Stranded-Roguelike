#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace StrandedRoguelike.Editor
{
    public static class HitFlashSetup
    {
        private const string HitFlashMaterialPath = "Assets/_Project/06_Materials/HitFlash_Mat.mat";
        private const string PlayerPrefabPath = "Assets/_Project/05_Prefabs/Player.prefab";
        private const string EnemyPrefabFolder = "Assets/_Project/05_Prefabs/Enemies";

        [InitializeOnLoadMethod]
        private static void AssignOnImport()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    AssignHitFlashMaterial();
                }
            };
        }

        [MenuItem("Tools/Stranded Roguelike/Assign HitFlash Material")]
        public static void AssignHitFlashMaterial()
        {
            Material hitFlashMaterial = AssetDatabase.LoadAssetAtPath<Material>(HitFlashMaterialPath);
            if (hitFlashMaterial == null)
            {
                Debug.LogWarning($"HitFlash material not found: {HitFlashMaterialPath}");
                return;
            }

            AssignPrefab(PlayerPrefabPath, hitFlashMaterial);

            string[] enemyPrefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { EnemyPrefabFolder });
            for (int i = 0; i < enemyPrefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(enemyPrefabGuids[i]);
                AssignPrefab(path, hitFlashMaterial);
            }

            AssetDatabase.SaveAssets();
        }

        private static void AssignPrefab(string prefabPath, Material material)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return;
            }

            HitFlash[] hitFlashes = prefab.GetComponentsInChildren<HitFlash>(true);
            if (hitFlashes.Length == 0)
            {
                return;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            HitFlash[] contentsHitFlashes = contents.GetComponentsInChildren<HitFlash>(true);
            bool changed = false;

            for (int i = 0; i < contentsHitFlashes.Length; i++)
            {
                SerializedObject serializedObject = new SerializedObject(contentsHitFlashes[i]);
                SerializedProperty flashMat = serializedObject.FindProperty("flashMat");

                if (flashMat != null && flashMat.objectReferenceValue == null)
                {
                    flashMat.objectReferenceValue = material;
                    serializedObject.ApplyModifiedProperties();
                    changed = true;
                }
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }

            PrefabUtility.UnloadPrefabContents(contents);
        }
    }
}
#endif
