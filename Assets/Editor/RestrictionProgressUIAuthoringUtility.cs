using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RestrictionProgressUIAuthoringUtility
{
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string RootName = "RestrictionProgressUI";

    [MenuItem("Tools/UI/Rebuild Restriction Progress UI")]
    public static void RebuildSampleSceneRestrictionProgressUI()
    {
        Scene scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
        GameObject canvasObject = GameObject.Find("MainCanvas");
        Canvas canvas = canvasObject != null ? canvasObject.GetComponent<Canvas>() : null;

        if (canvas == null)
        {
            Debug.LogError("RestrictionProgressUIAuthoringUtility: MainCanvas was not found.");
            return;
        }

        Transform existing = canvas.transform.Find(RootName);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject root = CreateRect(RootName, canvas.transform, new Vector2(48f, 360f));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(1f, 0.5f);
        rootRect.anchorMax = new Vector2(1f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = new Vector2(-42f, 0f);

        CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        RestrictionProgressUI progressUI = root.AddComponent<RestrictionProgressUI>();

        GameObject track = CreateImage("TrackBackground", root.transform, new Vector2(12f, 340f), new Color(0.04f, 0.045f, 0.055f, 0.62f));
        RectTransform trackRect = track.GetComponent<RectTransform>();
        trackRect.anchoredPosition = Vector2.zero;

        GameObject dangerFill = CreateImage("DangerFill", track.transform, new Vector2(8f, 24f), new Color(0.95f, 0.18f, 0.12f, 0.45f));
        RectTransform dangerFillRect = dangerFill.GetComponent<RectTransform>();
        dangerFillRect.anchorMin = new Vector2(0.5f, 0.5f);
        dangerFillRect.anchorMax = new Vector2(0.5f, 0.5f);
        dangerFillRect.pivot = new Vector2(0.5f, 0.5f);

        GameObject safeLine = CreateImage("SafeLine", track.transform, new Vector2(3f, 316f), new Color(0.72f, 0.78f, 0.82f, 0.58f));
        safeLine.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        GameObject playerMarker = CreateImage("PlayerMarker", root.transform, new Vector2(18f, 18f), new Color(0.82f, 1f, 1f, 1f));
        RectTransform playerMarkerRect = playerMarker.GetComponent<RectTransform>();
        playerMarkerRect.anchoredPosition = new Vector2(-13f, 0f);

        GameObject rocksMarker = CreateImage("RocksMarker", root.transform, new Vector2(18f, 18f), new Color(0.86f, 0.18f, 0.12f, 1f));
        RectTransform rocksMarkerRect = rocksMarker.GetComponent<RectTransform>();
        rocksMarkerRect.anchoredPosition = new Vector2(13f, 0f);
        rocksMarkerRect.localRotation = Quaternion.Euler(0f, 0f, 45f);

        LevelManager levelManager = Object.FindFirstObjectByType<LevelManager>();
        RockMovement rocks = Object.FindFirstObjectByType<RockMovement>(FindObjectsInactive.Include);
        PlayerMove playerMove = Object.FindFirstObjectByType<PlayerMove>(FindObjectsInactive.Include);

        SerializedObject serialized = new SerializedObject(progressUI);
        serialized.FindProperty("levelManager").objectReferenceValue = levelManager;
        serialized.FindProperty("player").objectReferenceValue = playerMove != null ? playerMove.transform : null;
        serialized.FindProperty("rocks").objectReferenceValue = rocks;
        serialized.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
        serialized.FindProperty("trackRect").objectReferenceValue = trackRect;
        serialized.FindProperty("playerMarker").objectReferenceValue = playerMarkerRect;
        serialized.FindProperty("rocksMarker").objectReferenceValue = rocksMarkerRect;
        serialized.FindProperty("dangerFill").objectReferenceValue = dangerFillRect;
        serialized.FindProperty("dangerFillImage").objectReferenceValue = dangerFill.GetComponent<Image>();
        serialized.FindProperty("trackImage").objectReferenceValue = track.GetComponent<Image>();
        serialized.FindProperty("playerMarkerImage").objectReferenceValue = playerMarker.GetComponent<Image>();
        serialized.FindProperty("rocksMarkerImage").objectReferenceValue = rocksMarker.GetComponent<Image>();
        serialized.ApplyModifiedPropertiesWithoutUndo();

        if (levelManager != null)
        {
            SerializedObject levelSerialized = new SerializedObject(levelManager);
            levelSerialized.FindProperty("useStageGoalY").boolValue = true;
            levelSerialized.FindProperty("deriveStageGoalYFromBossRoom").boolValue = true;
            levelSerialized.FindProperty("stageGoalY").floatValue = -250f;
            levelSerialized.FindProperty("setRocksTargetToStageGoal").boolValue = true;
            levelSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("RestrictionProgressUIAuthoringUtility: rebuilt restriction progress UI in SampleScene.");
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 size)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = LayerMask.NameToLayer("UI");
        gameObject.transform.SetParent(parent, false);

        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        return gameObject;
    }

    private static GameObject CreateImage(string name, Transform parent, Vector2 size, Color color)
    {
        GameObject gameObject = CreateRect(name, parent, size);
        gameObject.AddComponent<CanvasRenderer>();

        Image image = gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return gameObject;
    }
}
