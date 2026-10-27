using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrapTheCat.EditorTools
{
    public static class UIFontApplier
    {
        private const string FontPath = "Assets/Resources/Fonts/PixelifySans-VariableFont_wght SDF.asset";
        private const string UIPrefabFolder = "Assets/Resources/UI";

        [MenuItem("Tools/Trap The Cat/Apply PixelifySans To UI")]
        private static void Apply()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
            {
                Debug.LogError($"Could not load the UI font at {FontPath}.");
                return;
            }

            int changed = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                int sceneChanged = 0;
                foreach (GameObject root in scene.GetRootGameObjects())
                    sceneChanged += ApplyTo(root, font);
                if (sceneChanged > 0)
                    EditorSceneManager.MarkSceneDirty(scene);
                changed += sceneChanged;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { UIPrefabFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = PrefabUtility.LoadPrefabContents(path);
                int prefabChanged = ApplyTo(prefab, font);
                if (prefabChanged > 0)
                    PrefabUtility.SaveAsPrefabAsset(prefab, path);
                PrefabUtility.UnloadPrefabContents(prefab);
                changed += prefabChanged;
            }

            SetDefaultFont(font);
            Debug.Log($"Switched {changed} text component(s) to {font.name}. Save the scene to keep the change.");
        }

        private static int ApplyTo(GameObject root, TMP_FontAsset font)
        {
            int changed = 0;
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.font == font)
                    continue;

                Undo.RecordObject(text, "Apply UI Font");
                text.font = font;
                text.fontSharedMaterial = font.material;
                PrefabUtility.RecordPrefabInstancePropertyModifications(text);
                EditorUtility.SetDirty(text);
                changed++;
            }
            return changed;
        }

        private static void SetDefaultFont(TMP_FontAsset font)
        {
            TMP_Settings settings = TMP_Settings.instance;
            if (settings == null)
                return;

            SerializedObject serialized = new SerializedObject(settings);
            SerializedProperty defaultFont = serialized.FindProperty("m_defaultFontAsset");
            if (defaultFont == null || defaultFont.objectReferenceValue == font)
                return;

            defaultFont.objectReferenceValue = font;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }
    }
}
