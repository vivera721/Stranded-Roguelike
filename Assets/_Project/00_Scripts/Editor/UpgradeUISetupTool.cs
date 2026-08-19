#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace StrandedRoguelike.EditorTools
{
    public static class UpgradeUISetupTool
    {
        [MenuItem("Stranded Roguelike/Upgrades/Setup Upgrade UI In Current Scene")]
        public static void SetupUpgradeUI()
        {
            GameObject panel = FindSceneObject("Upgrade_Panel");
            GameObject group = FindSceneObject("UpgradeGroup");

            if (panel == null)
            {
                panel = group;
            }

            if (panel == null)
            {
                Debug.LogWarning("Upgrade_Panel or UpgradeGroup object was not found in the current scene.");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(panel, "Setup Upgrade UI");

            UpgradePanelController controller = panel.GetComponent<UpgradePanelController>();
            if (controller == null)
            {
                controller = Undo.AddComponent<UpgradePanelController>(panel);
            }

            UpgradeCardUI[] cards =
            {
                EnsureCard("Upgrade_Panel_1"),
                EnsureCard("Upgrade_Panel_2"),
                EnsureCard("Upgrade_Panel_3")
            };

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("upgradeGroup").objectReferenceValue = panel;
            SerializedProperty cardsProperty = serializedController.FindProperty("cards");
            cardsProperty.arraySize = cards.Length;

            for (int i = 0; i < cards.Length; i++)
            {
                cardsProperty.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            }

            serializedController.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
            Debug.Log("Upgrade UI setup complete. Press U in Play Mode to test the panel.");
        }

        private static UpgradeCardUI EnsureCard(string objectName)
        {
            GameObject cardObject = FindSceneObject(objectName);
            if (cardObject == null)
            {
                Debug.LogWarning($"{objectName} was not found.");
                return null;
            }

            UpgradeCardUI card = cardObject.GetComponent<UpgradeCardUI>();
            if (card == null)
            {
                card = Undo.AddComponent<UpgradeCardUI>(cardObject);
            }

            Button button = cardObject.GetComponent<Button>();
            if (button == null)
            {
                button = Undo.AddComponent<Button>(cardObject);
            }

            return card;
        }

        private static GameObject FindSceneObject(string objectName)
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
