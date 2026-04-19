using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PauseMenuAuthoringUtility
{
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/UI/Rebuild Pause Menu Editable Layout")]
    public static void RebuildPauseMenuEditableLayout()
    {
        Scene scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
        GamePauseController pauseController = Object.FindFirstObjectByType<GamePauseController>();
        if (pauseController == null)
            throw new MissingReferenceException("GamePauseController was not found in SampleScene.");

        SerializedObject serializedController = new SerializedObject(pauseController);
        SerializedProperty pauseMenuProperty = serializedController.FindProperty("pauseMenuRoot");
        GameObject pauseMenuRoot = pauseMenuProperty != null
            ? pauseMenuProperty.objectReferenceValue as GameObject
            : GameObject.Find("PauseMenu");

        if (pauseMenuRoot == null)
            throw new MissingReferenceException("PauseMenu root was not found.");

        PauseItemCollectionMenu menu = pauseMenuRoot.GetComponent<PauseItemCollectionMenu>();
        if (menu == null)
            menu = Undo.AddComponent<PauseItemCollectionMenu>(pauseMenuRoot);

        menu.RebuildEditableLayout();

        EditorUtility.SetDirty(menu);
        EditorUtility.SetDirty(pauseMenuRoot);
        EditorUtility.SetDirty(pauseController);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    public static void RebuildSampleScenePauseMenu()
    {
        RebuildPauseMenuEditableLayout();
    }
}
