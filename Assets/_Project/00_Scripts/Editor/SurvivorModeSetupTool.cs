#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StrandedRoguelike.EditorTools
{
    public static class SurvivorModeSetupTool
    {
        [MenuItem("Stranded Roguelike/Survivor/Setup Survivor Mode In Current Scene")]
        public static void SetupSurvivorMode()
        {
            PlayerMovement playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
            if (playerMovement == null)
            {
                Debug.LogWarning("PlayerMovement was not found in the current scene.");
                return;
            }

            GameObject player = playerMovement.gameObject;
            Undo.RegisterFullObjectHierarchyUndo(player, "Setup Survivor Player");

            SurvivorWeaponController weaponController = player.GetComponent<SurvivorWeaponController>();
            if (weaponController == null)
            {
                weaponController = Undo.AddComponent<SurvivorWeaponController>(player);
            }

            SurvivorExperience experience = player.GetComponent<SurvivorExperience>();
            if (experience == null)
            {
                experience = Undo.AddComponent<SurvivorExperience>(player);
            }

            GameObject bootstrapObject = GameObject.Find("Survivor Mode Bootstrap");
            if (bootstrapObject == null)
            {
                bootstrapObject = new GameObject("Survivor Mode Bootstrap");
                Undo.RegisterCreatedObjectUndo(bootstrapObject, "Create Survivor Mode Bootstrap");
            }

            SurvivorModeBootstrap bootstrap = bootstrapObject.GetComponent<SurvivorModeBootstrap>();
            if (bootstrap == null)
            {
                bootstrap = Undo.AddComponent<SurvivorModeBootstrap>(bootstrapObject);
            }

            SerializedObject serializedBootstrap = new SerializedObject(bootstrap);
            serializedBootstrap.FindProperty("player").objectReferenceValue = player.transform;
            serializedBootstrap.FindProperty("disableManualSlashAttack").boolValue = true;
            serializedBootstrap.FindProperty("disableDodge").boolValue = true;
            serializedBootstrap.FindProperty("faceMovementDirection").boolValue = true;
            serializedBootstrap.FindProperty("ensureWeaponController").boolValue = true;
            serializedBootstrap.FindProperty("ensureExperience").boolValue = true;
            serializedBootstrap.FindProperty("showStartWeaponChoice").boolValue = true;
            serializedBootstrap.FindProperty("enableLevelUpUpgrades").boolValue = true;
            serializedBootstrap.ApplyModifiedProperties();

            GameObject spawnerObject = GameObject.Find("Survivor Enemy Spawner");
            if (spawnerObject == null)
            {
                spawnerObject = new GameObject("Survivor Enemy Spawner");
                Undo.RegisterCreatedObjectUndo(spawnerObject, "Create Survivor Enemy Spawner");
            }

            SurvivorEnemySpawner spawner = spawnerObject.GetComponent<SurvivorEnemySpawner>();
            if (spawner == null)
            {
                spawner = Undo.AddComponent<SurvivorEnemySpawner>(spawnerObject);
            }

            AssignSpawnerDefaults(spawner, player.transform);
            SetupUpgradePanels(weaponController, experience, bootstrap);

            EditorUtility.SetDirty(player);
            EditorUtility.SetDirty(bootstrapObject);
            EditorUtility.SetDirty(spawnerObject);
            Debug.Log("Survivor mode setup complete. Assign weapon sprites/projectile sprites on SurvivorWeaponController, then press Play.");
        }

        private static void AssignSpawnerDefaults(SurvivorEnemySpawner spawner, Transform player)
        {
            List<GameObject> enemyPrefabs = FindEnemyPrefabs();
            SerializedObject serializedSpawner = new SerializedObject(spawner);
            serializedSpawner.FindProperty("player").objectReferenceValue = player;

            SerializedProperty enemiesProperty = serializedSpawner.FindProperty("enemies");
            enemiesProperty.arraySize = Mathf.Min(4, enemyPrefabs.Count);

            for (int i = 0; i < enemiesProperty.arraySize; i++)
            {
                SerializedProperty entry = enemiesProperty.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("prefab").objectReferenceValue = enemyPrefabs[i];
                entry.FindPropertyRelative("maxHealth").intValue = 3 + i * 2;
                entry.FindPropertyRelative("moveSpeed").floatValue = 1.8f + i * 0.18f;
                entry.FindPropertyRelative("contactDamage").intValue = 1;
                entry.FindPropertyRelative("experienceValue").intValue = 1 + i;
                entry.FindPropertyRelative("weight").intValue = Mathf.Max(1, 5 - i);
                entry.FindPropertyRelative("prewarmCount").intValue = 24;
                entry.FindPropertyRelative("maxPoolSize").intValue = 180;
            }

            serializedSpawner.ApplyModifiedProperties();
        }

        private static List<GameObject> FindEnemyPrefabs()
        {
            string[] folders =
            {
                "Assets/_Project/05_Prefabs/Enemies",
                "Assets/_Project/Generated/Enemies"
            };

            string[] guids = AssetDatabase.FindAssets("t:Prefab", folders);
            List<GameObject> result = new List<GameObject>();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                if (prefab.GetComponentInChildren<EnemyHealth>(true) != null || prefab.GetComponentInChildren<Rigidbody2D>(true) != null)
                {
                    result.Add(prefab);
                }
            }

            return result;
        }

        private static void SetupUpgradePanels(SurvivorWeaponController weaponController, SurvivorExperience experience, SurvivorModeBootstrap bootstrap)
        {
            GameObject panel = FindSceneObjectIncludingInactive("Upgrade_Panel");
            if (panel == null)
            {
                return;
            }

            panel.SetActive(true);

            UpgradePanelController oldUpgradePanel = panel.GetComponent<UpgradePanelController>();
            if (oldUpgradePanel != null)
            {
                oldUpgradePanel.enabled = false;
            }

            SurvivorStartWeaponChoicePanel startPanel = panel.GetComponent<SurvivorStartWeaponChoicePanel>();
            if (startPanel == null)
            {
                startPanel = Undo.AddComponent<SurvivorStartWeaponChoicePanel>(panel);
            }

            SerializedObject serializedPanel = new SerializedObject(startPanel);
            serializedPanel.FindProperty("weaponController").objectReferenceValue = weaponController;
            serializedPanel.FindProperty("panelRoot").objectReferenceValue = panel;
            serializedPanel.FindProperty("showOnStart").boolValue = true;
            serializedPanel.ApplyModifiedProperties();

            SurvivorLevelUpUpgradePanel levelUpPanel = panel.GetComponent<SurvivorLevelUpUpgradePanel>();
            if (levelUpPanel == null)
            {
                levelUpPanel = Undo.AddComponent<SurvivorLevelUpUpgradePanel>(panel);
            }

            SerializedObject serializedLevelPanel = new SerializedObject(levelUpPanel);
            serializedLevelPanel.FindProperty("experience").objectReferenceValue = experience;
            serializedLevelPanel.FindProperty("weaponController").objectReferenceValue = weaponController;
            serializedLevelPanel.FindProperty("panelRoot").objectReferenceValue = panel;
            serializedLevelPanel.ApplyModifiedProperties();

            SerializedObject serializedBootstrap = new SerializedObject(bootstrap);
            serializedBootstrap.FindProperty("startWeaponPanelRoot").objectReferenceValue = panel;
            serializedBootstrap.FindProperty("startWeaponChoicePanel").objectReferenceValue = startPanel;
            serializedBootstrap.FindProperty("levelUpUpgradePanel").objectReferenceValue = levelUpPanel;
            serializedBootstrap.ApplyModifiedProperties();

            EditorUtility.SetDirty(panel);
        }

        private static GameObject FindSceneObjectIncludingInactive(string objectName)
        {
            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i].name == objectName && objects[i].scene.IsValid())
                {
                    return objects[i];
                }
            }

            return null;
        }
    }
}
#endif
