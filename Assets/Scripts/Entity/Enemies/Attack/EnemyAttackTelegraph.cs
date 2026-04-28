using UnityEngine;

public class EnemyAttackTelegraph : MonoBehaviour
{
    private enum TelegraphShape
    {
        Box,
        Circle
    }

    private const float CircleOuterRadiusNormalized = 0.98f;
    private const float CircleInnerRadiusNormalized = 0.8f;

    [Header("Visual")]
    [SerializeField] private Color telegraphColor = new Color(1f, 0.15f, 0.1f, 0.3f);
    [SerializeField] private float pulseSpeed = 5f;
    [SerializeField] private float projectileThickness = 0.2f;
    [SerializeField] private int sortingOrder = 20;

    private SpriteRenderer activeRenderer;
    private GameObject telegraphObject;
    private TelegraphShape currentShape;
    private Color activeBaseColor;

    private static Sprite squareSprite;
    private static Sprite circleSprite;

    private void Update()
    {
        if (activeRenderer == null || !activeRenderer.enabled)
            return;

        Color color = activeBaseColor;
        float pulse = 0.75f + Mathf.PingPong(Time.time * pulseSpeed, 0.25f);
        color.a = activeBaseColor.a * pulse;
        activeRenderer.color = color;
    }

    public void ShowHitBoxTelegraph(Vector3 origin, AttackData attackData, float angle, int floorZ)
    {
        ShowHitBoxTelegraph(origin, attackData, angle, floorZ, telegraphColor);
    }

    public void ShowHitBoxTelegraph(Vector3 origin, AttackData attackData, float angle, int floorZ, Color color)
    {
        if (attackData == null)
            return;

        Vector3 center = origin + (Quaternion.Euler(0f, 0f, angle) * (Vector3)attackData.hitboxOffset);
        Vector3 scale = new Vector3(attackData.hitboxSize.x, attackData.hitboxSize.y, 1f);
        ShowTelegraph(TelegraphShape.Box, center, Quaternion.Euler(0f, 0f, angle), scale, floorZ, color);
    }

    public void ShowSweptHitBoxTelegraph(Vector3 origin, AttackData attackData, float angle, float sweepDistance, int floorZ)
    {
        ShowSweptHitBoxTelegraph(origin, attackData, angle, sweepDistance, floorZ, telegraphColor);
    }

    public void ShowSweptHitBoxTelegraph(Vector3 origin, AttackData attackData, float angle, float sweepDistance, int floorZ, Color color)
    {
        if (attackData == null)
            return;

        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        float clampedSweepDistance = Mathf.Max(0f, sweepDistance);
        if (clampedSweepDistance <= 0.001f)
        {
            ShowHitBoxTelegraph(origin, attackData, angle, floorZ, color);
            return;
        }

        Vector3 startCenter = origin + (rotation * (Vector3)attackData.hitboxOffset);
        Vector3 sweepDirection = rotation * Vector3.up;
        Vector3 center = startCenter + (sweepDirection * (clampedSweepDistance * 0.5f));
        Vector3 scale = new Vector3(attackData.hitboxSize.x, attackData.hitboxSize.y + clampedSweepDistance, 1f);
        ShowTelegraph(TelegraphShape.Box, center, rotation, scale, floorZ, color);
    }

    public void ShowProjectileTelegraph(Vector3 origin, Vector2 direction, int floorZ, Projectile projectile)
    {
        ShowProjectileTelegraph(origin, direction, floorZ, projectile, telegraphColor);
    }

    public void ShowProjectileTelegraph(Vector3 origin, Vector2 direction, int floorZ, Projectile projectile, Color color)
    {
        if (projectile == null || direction.sqrMagnitude <= 0.001f)
            return;

        float distance = projectile.GetPreviewTravelDistance(origin, direction, floorZ);
        if (distance <= 0.01f)
        {
            Hide();
            return;
        }

        ShowLineTelegraph(origin, direction, distance, floorZ, color);
    }

    public void ShowCircleTelegraph(Vector3 origin, float radius, int floorZ)
    {
        ShowCircleTelegraph(origin, radius, floorZ, telegraphColor);
    }

    public void ShowCircleTelegraph(Vector3 origin, float radius, int floorZ, Color color)
    {
        Vector3 scale = Vector3.one * Mathf.Max(0.1f, radius * 2f);
        ShowTelegraph(TelegraphShape.Circle, origin, Quaternion.identity, scale, floorZ, color);
    }

    public void Hide()
    {
        if (activeRenderer == null)
            return;

        activeRenderer.enabled = false;
    }

    public void ShowLineTelegraph(Vector3 origin, Vector2 direction, float distance, int floorZ, Color color)
    {
        ShowLineTelegraph(origin, direction, distance, floorZ, color, floorZ);
    }

    public void ShowLineTelegraph(Vector3 origin, Vector2 direction, float distance, int floorZ, Color color, float displayZ)
    {
        if (direction.sqrMagnitude <= 0.001f || distance <= 0.01f)
        {
            Hide();
            return;
        }

        Vector3 center = origin + (Vector3)(direction.normalized * (distance * 0.5f));
        center.z = displayZ;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        Vector3 scale = new Vector3(projectileThickness, distance, 1f);
        ShowTelegraph(TelegraphShape.Box, center, Quaternion.Euler(0f, 0f, angle), scale, Mathf.RoundToInt(displayZ), color);
    }

    private void ShowTelegraph(TelegraphShape shape, Vector3 position, Quaternion rotation, Vector3 scale, int floorZ, Color color)
    {
        EnsureRenderer(shape);

        telegraphObject.transform.position = new Vector3(position.x, position.y, floorZ);
        telegraphObject.transform.rotation = rotation;
        telegraphObject.transform.localScale = scale;

        activeBaseColor = color;
        activeRenderer.color = activeBaseColor;
        activeRenderer.enabled = true;
    }

    private void EnsureRenderer(TelegraphShape shape)
    {
        if (telegraphObject == null)
        {
            telegraphObject = new GameObject("EnemyAttackTelegraph");
            telegraphObject.transform.SetParent(transform);
            telegraphObject.transform.localPosition = Vector3.zero;
            activeRenderer = telegraphObject.AddComponent<SpriteRenderer>();
            activeRenderer.sortingOrder = sortingOrder;
        }

        if (activeRenderer == null)
            activeRenderer = telegraphObject.GetComponent<SpriteRenderer>();

        if (currentShape == shape && activeRenderer.sprite != null)
            return;

        currentShape = shape;
        activeRenderer.sprite = shape == TelegraphShape.Circle ? GetCircleSprite() : GetSquareSprite();
        activeRenderer.drawMode = shape == TelegraphShape.Circle ? SpriteDrawMode.Simple : SpriteDrawMode.Sliced;
        activeRenderer.size = Vector2.one;
    }

    private static Sprite GetSquareSprite()
    {
        if (squareSprite != null)
            return squareSprite;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        squareSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f,
            0u,
            SpriteMeshType.FullRect);
        return squareSprite;
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null)
            return circleSprite;

        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float maxRadius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float normalizedDistance = Vector2.Distance(new Vector2(x, y), center) / maxRadius;
                float alpha = 0f;

                if (normalizedDistance <= CircleInnerRadiusNormalized)
                    alpha = 0.18f;
                else if (normalizedDistance <= CircleOuterRadiusNormalized)
                    alpha = 0.55f;

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        circleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size,
            0u,
            SpriteMeshType.FullRect);
        return circleSprite;
    }
}
