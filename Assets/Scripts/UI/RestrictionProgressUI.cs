using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RestrictionProgressUI : MonoBehaviour
{
    [Header("World References")]
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private Transform player;
    [SerializeField] private RockMovement rocks;

    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform trackRect;
    [SerializeField] private RectTransform playerMarker;
    [SerializeField] private RectTransform rocksMarker;
    [SerializeField] private RectTransform dangerFill;
    [SerializeField] private Image dangerFillImage;
    [SerializeField] private Image trackImage;
    [SerializeField] private Image playerMarkerImage;
    [SerializeField] private Image rocksMarkerImage;

    [Header("Layout")]
    [SerializeField] private float verticalPadding = 12f;
    [SerializeField] private float hiddenAlpha = 0f;

    [Header("Danger")]
    [SerializeField] private float safeDistance = 18f;
    [SerializeField] private float dangerDistance = 5f;
    [SerializeField] private Color safeFillColor = new Color(0.35f, 0.82f, 0.9f, 0.18f);
    [SerializeField] private Color dangerFillColor = new Color(0.95f, 0.18f, 0.12f, 0.82f);
    [SerializeField] private Color normalTrackColor = new Color(0.04f, 0.045f, 0.055f, 0.62f);
    [SerializeField] private Color warningTrackColor = new Color(0.35f, 0.03f, 0.025f, 0.78f);
    [SerializeField] private float warningPulseSpeed = 5f;
    [SerializeField] private float warningMarkerScale = 1.16f;

    private Vector3 playerBaseScale = Vector3.one;
    private Vector3 rocksBaseScale = Vector3.one;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (playerMarker != null)
            playerBaseScale = playerMarker.localScale;

        if (rocksMarker != null)
            rocksBaseScale = rocksMarker.localScale;
    }

    private void OnEnable()
    {
        ResolveReferences();
        Refresh(true);
    }

    private void Update()
    {
        Refresh(false);
    }

    private void ResolveReferences()
    {
        if (levelManager == null)
            levelManager = FindFirstObjectByType<LevelManager>();

        if (levelManager != null)
        {
            if (player == null)
                player = levelManager.player;

            if (rocks == null)
                rocks = levelManager.rockMovement;
        }

        if (player == null)
        {
            PlayerMove playerMove = FindFirstObjectByType<PlayerMove>();
            if (playerMove != null)
                player = playerMove.transform;
        }

        if (rocks == null)
            rocks = FindFirstObjectByType<RockMovement>(FindObjectsInactive.Include);
    }

    private void Refresh(bool forceResolve)
    {
        if (forceResolve || player == null || rocks == null)
            ResolveReferences();

        bool canShow = player != null
            && rocks != null
            && trackRect != null
            && playerMarker != null
            && rocksMarker != null
            && rocks.gameObject.activeInHierarchy
            && IsRunStarted();

        if (canvasGroup != null)
            canvasGroup.alpha = canShow ? 1f : hiddenAlpha;

        if (!canShow)
            return;

        float playerProgress = CalculatePlayerProgress();
        float rocksProgress = rocks.Progress01;

        float playerY = GetMarkerY(playerProgress);
        float rocksY = GetMarkerY(rocksProgress);

        SetMarkerY(playerMarker, playerY);
        SetMarkerY(rocksMarker, rocksY);
        UpdateDangerFill(playerY, rocksY);
        UpdateDangerVisuals();
    }

    private float CalculatePlayerProgress()
    {
        float startY = rocks.ProgressStartY;
        float goalY = rocks.TargetY;

        if (levelManager != null && levelManager.HasStageGoal)
            goalY = levelManager.StageGoalY;

        if (Mathf.Approximately(startY, goalY))
            return 1f;

        return Mathf.Clamp01(Mathf.InverseLerp(startY, goalY, player.position.y));
    }

    private float GetMarkerY(float progress)
    {
        float halfHeight = trackRect.rect.height * 0.5f;
        float bottom = -halfHeight + verticalPadding;
        float top = halfHeight - verticalPadding;
        return Mathf.Lerp(top, bottom, Mathf.Clamp01(progress));
    }

    private bool IsRunStarted()
    {
        return levelManager == null || levelManager.HasStarted;
    }

    private static void SetMarkerY(RectTransform marker, float y)
    {
        Vector2 anchoredPosition = marker.anchoredPosition;
        anchoredPosition.y = y;
        marker.anchoredPosition = anchoredPosition;
    }

    private void UpdateDangerFill(float playerY, float rocksY)
    {
        if (dangerFill == null)
            return;

        float bottom = Mathf.Min(playerY, rocksY);
        float top = Mathf.Max(playerY, rocksY);
        float height = Mathf.Max(2f, top - bottom);

        dangerFill.anchoredPosition = new Vector2(dangerFill.anchoredPosition.x, bottom + height * 0.5f);
        dangerFill.sizeDelta = new Vector2(dangerFill.sizeDelta.x, height);
    }

    private void UpdateDangerVisuals()
    {
        float distance = Mathf.Abs(player.position.y - rocks.transform.position.y);
        float danger01 = 1f - Mathf.InverseLerp(dangerDistance, safeDistance, distance);
        danger01 = Mathf.Clamp01(danger01);

        if (dangerFillImage != null)
            dangerFillImage.color = Color.Lerp(safeFillColor, dangerFillColor, danger01);

        float pulse = Mathf.Sin(Time.unscaledTime * warningPulseSpeed) * 0.5f + 0.5f;
        float warning01 = Mathf.SmoothStep(0f, 1f, danger01);

        if (trackImage != null)
            trackImage.color = Color.Lerp(normalTrackColor, warningTrackColor, warning01 * pulse);

        float markerScale = Mathf.Lerp(1f, warningMarkerScale, warning01 * pulse);

        if (playerMarker != null)
            playerMarker.localScale = playerBaseScale * markerScale;

        if (rocksMarker != null)
            rocksMarker.localScale = rocksBaseScale * markerScale;

        if (playerMarkerImage != null)
            playerMarkerImage.color = Color.Lerp(Color.white, new Color(0.7f, 1f, 1f, 1f), warning01);

        if (rocksMarkerImage != null)
            rocksMarkerImage.color = Color.Lerp(new Color(0.86f, 0.18f, 0.12f, 1f), Color.red, warning01);
    }
}
