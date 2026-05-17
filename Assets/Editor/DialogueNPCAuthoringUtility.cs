using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class DialogueNPCAuthoringUtility
{
    private const string PrefabPath = "Assets/Prefabs/NPC/DialogueNPC.prefab";
    private const string ControllerPath = "Assets/Sprites/NPC/Tantalus/Idle3_30.controller";
    private const string SpritePath = "Assets/Sprites/NPC/Tantalus/Idle3.png";

    [MenuItem("Tools/NPC/Rebuild Dialogue NPC Prefab")]
    public static void RebuildDialogueNPCPrefab()
    {
        GameObject root = new GameObject("DialogueNPC");
        try
        {
            root.transform.position = new Vector3(0f, 0f, 0f);

            BoxCollider2D trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(10f, 8f);

            DialogueNPC npc = root.AddComponent<DialogueNPC>();
            npc.hitLayer = ResolvePlayerLayerMask();
            npc.floorZ = 0;
            npc.dialogueDuration = 2.5f;
            npc.firstEncounterLines = new[]
            {
                "처음 보는 얼굴이군.",
                "대기장소에서는 숨을 고르는 것도 실력이지.",
                "준비가 끝나면 다시 말을 걸어라."
            };
            npc.repeatLines = new[]
            {
                "천천히 둘러봐라.",
                "필요한 건 출발 전에 확인해 둬.",
                "다시 도전할 준비는 됐나?"
            };

            MerchantDialogueUI dialogueUI = CreateDialogueCanvas(root.transform);
            npc.dialogueUI = dialogueUI;
            npc.interactionCanvas = CreateInteractionCanvas(root.transform);

            CreateSprite(root.transform);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Dialogue NPC prefab rebuilt at {PrefabPath}");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static LayerMask ResolvePlayerLayerMask()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
            return 1 << playerLayer;

        return 64;
    }

    private static MerchantDialogueUI CreateDialogueCanvas(Transform parent)
    {
        GameObject canvasObject = new GameObject("Canvas_dialog", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MerchantDialogueUI));
        canvasObject.layer = 5;
        canvasObject.transform.SetParent(parent, false);

        RectTransform rect = canvasObject.GetComponent<RectTransform>();
        rect.localPosition = new Vector3(0f, 2f, 10f);
        rect.localScale = new Vector3(0.05f, 0.05f, 1f);
        rect.sizeDelta = new Vector2(100f, 40f);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.referencePixelsPerUnit = 32f;
        scaler.dynamicPixelsPerUnit = 32f;

        CreatePanelImage(rect, "Image", new Vector2(100f, 40f));
        TextMeshProUGUI text = CreateText(rect, "dialog", "대화 내용", new Vector2(90f, 26f), new Vector2(0f, -6f), 8f);
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;

        MerchantDialogueUI dialogueUI = canvasObject.GetComponent<MerchantDialogueUI>();
        dialogueUI.canvas = canvas;
        dialogueUI.dialogueText = text;
        dialogueUI.defaultDuration = 2.5f;

        return dialogueUI;
    }

    private static Canvas CreateInteractionCanvas(Transform parent)
    {
        GameObject canvasObject = new GameObject("Canvas_interactive", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.layer = 5;
        canvasObject.transform.SetParent(parent, false);

        RectTransform rect = canvasObject.GetComponent<RectTransform>();
        rect.localPosition = new Vector3(-0.88f, 0.609f, 0f);
        rect.localScale = new Vector3(0.05f, 0.05f, 1f);
        rect.sizeDelta = new Vector2(10f, 10f);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.referencePixelsPerUnit = 32f;
        scaler.dynamicPixelsPerUnit = 32f;

        CreatePanelImage(rect, "Image", new Vector2(10f, 10f));
        TextMeshProUGUI keyText = CreateText(rect, "E", "E", new Vector2(10f, 10f), new Vector2(0.3f, -0.7f), 10f);
        keyText.alignment = TextAlignmentOptions.Center;
        keyText.textWrappingMode = TextWrappingModes.NoWrap;

        return canvas;
    }

    private static void CreatePanelImage(RectTransform parent, string name, Vector2 size)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.layer = 5;
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.7058824f);
    }

    private static TextMeshProUGUI CreateText(RectTransform parent, string name, string textValue, Vector2 size, Vector2 position, float fontSize)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.layer = 5;
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = textValue;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.raycastTarget = true;

        return text;
    }

    private static void CreateSprite(Transform parent)
    {
        GameObject spriteObject = new GameObject("DialogueNPCSprite", typeof(SpriteRenderer), typeof(Animator));
        spriteObject.transform.SetParent(parent, false);
        spriteObject.transform.localScale = new Vector3(3f, 3f, 1f);

        SpriteRenderer spriteRenderer = spriteObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);

        Animator animator = spriteObject.GetComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
    }
}
