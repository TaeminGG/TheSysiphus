using UnityEngine;
using UnityEngine.Tilemaps;

public class Projectile : MonoBehaviour
{
    [System.Serializable]
    public struct DirectionalVisualRotationOffsets
    {
        public float north;
        public float northEast;
        public float east;
        public float southEast;
        public float south;
        public float southWest;
        public float west;
        public float northWest;

        public float GetOffset(Vector2 direction)
        {
            if (direction.sqrMagnitude <= 0.001f)
                return south;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (angle < 0f)
                angle += 360f;

            if (angle >= 337.5f || angle < 22.5f)
                return east;
            if (angle < 67.5f)
                return northEast;
            if (angle < 112.5f)
                return north;
            if (angle < 157.5f)
                return northWest;
            if (angle < 202.5f)
                return west;
            if (angle < 247.5f)
                return southWest;
            if (angle < 292.5f)
                return south;

            return southEast;
        }
    }

    public float moveSpeed = 1.0f;
    public float lifeTime = 2.0f;
    [Header("Ground Blocking")]
    public bool blockOnGroundCollision = false;
    [Header("Visual Rotation")]
    [SerializeField] private SpriteRenderer visualRendererOverride;
    [SerializeField] private bool useVisualRotationOffsets = false;
    [SerializeField] private float visualRotationOffset = 0f;
    [SerializeField] private DirectionalVisualRotationOffsets directionalVisualRotationOffsets;
    [Header("Height Visual")]
    [SerializeField] private bool useHeightTrajectory = false;
    [SerializeField] private Sprite shadowSprite;
    [SerializeField] private Color shadowColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private float shadowScale = 1f;
    [SerializeField] private float shadowMinAlpha = 0.15f;
    [SerializeField] private float visualYOffsetPerZ = 0.25f;

    private float timer = 0f;
    private float floorZ = 0f;
    private float groundFloorZ = 0f;
    private float launchProjectileZ = 0f;
    private Grid grid;
    private Tilemap bmap;
    private float currentProjectileZ = 0f;
    private float targetProjectileZ = 0f;
    private float targetHeightDistance = 0f;
    private float traveledDistance = 0f;
    private AttackHitBox attackHitBox;
    private SpriteRenderer rootRenderer;
    private Transform renderRoot;
    private SpriteRenderer visualRenderer;
    private SpriteRenderer shadowRenderer;
    private Vector3 visualBaseLocalPosition;

    private void Awake()
    {
        attackHitBox = GetComponent<AttackHitBox>();
        rootRenderer = GetComponent<SpriteRenderer>();
        visualRenderer = ResolveVisualRenderer();
        if (visualRenderer != null)
            visualBaseLocalPosition = visualRenderer.transform.localPosition;

        if (useHeightTrajectory || useVisualRotationOffsets)
        {
            SetupVisuals();
            ApplyVisualRotation();
        }
    }

    private void Start()
    {
        if (blockOnGroundCollision)
            ResolveGroundMap();

        if (currentProjectileZ <= 0f)
            currentProjectileZ = floorZ;

        if (launchProjectileZ <= 0f)
            launchProjectileZ = currentProjectileZ;

        if (targetProjectileZ <= 0f)
            targetProjectileZ = currentProjectileZ;

        groundFloorZ = ResolveGroundFloorZ(transform.position);
        UpdateRootPosition(transform.position);

        SyncHitboxHeight();
        UpdateHeightVisuals();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if(timer > lifeTime )
        {
            Destroy(gameObject);
        }

        ApplyVisualRotation();
    }
    private void FixedUpdate()
    {
        float stepDistance = moveSpeed * Time.deltaTime;
        float nextTravelDistance = traveledDistance + stepDistance;
        float nextProjectileZ = ResolveProjectileZ(nextTravelDistance);

        Vector3 nextPosition = transform.position + (transform.up * stepDistance);
        float nextGroundFloorZ = ResolveGroundFloorZ(nextPosition);

        if (blockOnGroundCollision && nextGroundFloorZ > nextProjectileZ + 0.01f)
        {
            Destroy(gameObject);
            return;
        }

        groundFloorZ = nextGroundFloorZ;
        UpdateRootPosition(nextPosition);
        traveledDistance = nextTravelDistance;
        currentProjectileZ = nextProjectileZ;
        SyncHitboxHeight();
        UpdateHeightVisuals();
    }

    public void SetFloorZ(int z)
    {
        floorZ = z;
        groundFloorZ = z;

        SyncHitboxHeight();
        UpdateHeightVisuals();
    }

    public void SetProjectileStartZ(float z)
    {
        launchProjectileZ = z;
        currentProjectileZ = z;
        if (targetProjectileZ <= 0f)
            targetProjectileZ = z;

        SyncHitboxHeight();
        UpdateHeightVisuals();
    }

    public void EnableGroundBlocking(bool enabled)
    {
        blockOnGroundCollision = enabled;
        if (blockOnGroundCollision)
            ResolveGroundMap();
    }

    public float GetMaxTravelDistance()
    {
        return moveSpeed * lifeTime;
    }

    public float GetPreviewTravelDistance(Vector3 startPosition, Vector2 direction, int z, float stepDistance = 0.1f)
    {
        float maxDistance = GetMaxTravelDistance();
        if (!blockOnGroundCollision)
            return maxDistance;

        ResolveGroundMap();
        return CalculateTravelDistance(grid, bmap, startPosition, direction, z, z, maxDistance, maxDistance, stepDistance);
    }

    public float GetPreviewTravelDistance(Vector3 startPosition, Vector2 direction, float startZ, float targetZ, float targetDistance, float stepDistance = 0.1f)
    {
        float maxDistance = GetMaxTravelDistance();
        if (!blockOnGroundCollision)
            return maxDistance;

        ResolveGroundMap();
        return CalculateTravelDistance(grid, bmap, startPosition, direction, startZ, targetZ, Mathf.Max(0.01f, targetDistance), maxDistance, stepDistance);
    }

    public static float CalculateTravelDistance(Grid grid, Tilemap bmap, Vector3 startPosition, Vector2 direction, float startZ, float targetZ, float targetDistance, float maxDistance, float stepDistance = 0.1f)
    {
        if (grid == null || bmap == null || direction.sqrMagnitude <= 0.001f)
            return maxDistance;

        float safeStep = Mathf.Max(0.05f, stepDistance);
        Vector2 normalizedDirection = direction.normalized;

        for (float travelled = safeStep; travelled <= maxDistance; travelled += safeStep)
        {
            Vector3 samplePosition = startPosition + (Vector3)(normalizedDirection * travelled);
            float sampleZ = ResolveProjectileZ(startZ, targetZ, targetDistance, travelled);
            if (IsBlocked(grid, bmap, samplePosition, sampleZ))
                return Mathf.Max(0f, travelled - safeStep);
        }

        return maxDistance;
    }

    public static bool IsBlocked(Grid grid, Tilemap bmap, Vector3 worldPosition, float projectileZ)
    {
        return GetFloorZAtWorldPosition(grid, bmap, worldPosition) > projectileZ + 0.01f;
    }

    private void ResolveGroundMap()
    {
        if (grid == null)
            grid = FindFirstObjectByType<Grid>();

        if (grid == null || bmap != null)
            return;

        Tilemap[] maps = grid.GetComponentsInChildren<Tilemap>();
        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i].gameObject.name == "bmap")
            {
                bmap = maps[i];
                break;
            }
        }
    }

    public void ConfigureHeightShot(float targetZ, float targetDistance)
    {
        useHeightTrajectory = true;
        targetProjectileZ = targetZ;
        targetHeightDistance = Mathf.Max(0.01f, targetDistance);

        if (visualRenderer == null || renderRoot == null)
            SetupVisuals();

        UpdateHeightVisuals();
    }

    private void SetupVisuals()
    {
        if (visualRenderer == null)
            visualRenderer = ResolveVisualRenderer();
        if (visualRenderer == null)
            return;

        if (renderRoot == null)
        {
            GameObject renderRootObject = new GameObject("ProjectileRenderRoot");
            renderRootObject.transform.SetParent(transform, false);
            renderRoot = renderRootObject.transform;
            renderRoot.localPosition = Vector3.zero;
            renderRoot.rotation = Quaternion.identity;
        }

        if (rootRenderer != null && visualRenderer == rootRenderer)
        {
            GameObject visualObject = new GameObject("ProjectileVisual");
            visualObject.transform.SetParent(renderRoot, false);
            visualRenderer = visualObject.AddComponent<SpriteRenderer>();
            CopyRendererSettings(rootRenderer, visualRenderer);
            visualBaseLocalPosition = visualRenderer.transform.localPosition;
            rootRenderer.enabled = false;
        }
        else if (visualRenderer.transform.parent != renderRoot)
        {
            visualRenderer.transform.SetParent(renderRoot, false);
            visualBaseLocalPosition = visualRenderer.transform.localPosition;
        }

        if (useHeightTrajectory && shadowRenderer == null && shadowSprite != null)
        {
            GameObject shadowObject = new GameObject("ProjectileShadow");
            shadowObject.transform.SetParent(renderRoot, false);
            shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = shadowSprite;
            shadowRenderer.color = shadowColor;
            shadowRenderer.sortingLayerID = visualRenderer.sortingLayerID;
            shadowRenderer.sortingOrder = visualRenderer.sortingOrder - 1;
            shadowRenderer.transform.localScale = Vector3.one * shadowScale;
        }
    }

    private void CopyRendererSettings(SpriteRenderer source, SpriteRenderer target)
    {
        target.sprite = source.sprite;
        target.sharedMaterial = source.sharedMaterial;
        target.color = source.color;
        target.flipX = source.flipX;
        target.flipY = source.flipY;
        target.sortingLayerID = source.sortingLayerID;
        target.sortingOrder = source.sortingOrder;
        target.drawMode = source.drawMode;
        target.size = source.size;
    }

    private void ApplyVisualRotation()
    {
        if (renderRoot == null && !useVisualRotationOffsets)
            return;

        if (renderRoot != null)
            renderRoot.rotation = Quaternion.identity;

        if (visualRenderer == null)
            return;

        visualRenderer.transform.rotation = GetVisualRotation();
    }

    private SpriteRenderer ResolveVisualRenderer()
    {
        if (visualRendererOverride != null)
            return visualRendererOverride;

        if (rootRenderer != null)
            return rootRenderer;

        return GetComponentInChildren<SpriteRenderer>();
    }

    private Quaternion GetVisualRotation()
    {
        if (!useVisualRotationOffsets)
            return transform.rotation;

        Vector2 direction = transform.up;
        float directionOffset = directionalVisualRotationOffsets.GetOffset(direction);
        return transform.rotation * Quaternion.Euler(0f, 0f, visualRotationOffset + directionOffset);
    }

    private static float ResolveProjectileZ(float startZ, float targetZ, float targetDistance, float travelledDistance)
    {
        if (targetDistance <= 0.001f)
            return targetZ;

        float progress = Mathf.Clamp01(travelledDistance / targetDistance);
        return Mathf.Lerp(startZ, targetZ, progress);
    }

    private float ResolveProjectileZ(float travelledDistance)
    {
        if (!useHeightTrajectory)
            return floorZ;

        return ResolveProjectileZ(launchProjectileZ <= 0f ? floorZ : launchProjectileZ, targetProjectileZ, targetHeightDistance, travelledDistance);
    }

    private void SyncHitboxHeight()
    {
        attackHitBox?.SetFloorZ(currentProjectileZ);
    }

    private void UpdateHeightVisuals()
    {
        if (!useHeightTrajectory || visualRenderer == null)
            return;

        float heightOffset = Mathf.Max(0f, currentProjectileZ - groundFloorZ);
        visualRenderer.transform.localPosition = visualBaseLocalPosition + new Vector3(0f, currentProjectileZ * visualYOffsetPerZ, currentProjectileZ);
        ApplyVisualRotation();

        if (shadowRenderer != null)
        {
            shadowRenderer.transform.localPosition = new Vector3(0f, 0f, groundFloorZ);
            shadowRenderer.transform.localRotation = Quaternion.identity;
            float normalizedHeight = Mathf.Clamp01(heightOffset / 6f);
            Color color = shadowColor;
            color.a = Mathf.Lerp(shadowColor.a, shadowMinAlpha, normalizedHeight);
            shadowRenderer.color = color;

            float scale = Mathf.Lerp(shadowScale, shadowScale * 0.8f, normalizedHeight);
            shadowRenderer.transform.localScale = Vector3.one * scale;
        }
    }

    private static int GetFloorZAtWorldPosition(Grid grid, Tilemap bmap, Vector3 worldPosition)
    {
        if (grid == null || bmap == null)
            return 0;

        Vector3Int cellPosition = grid.WorldToCell(worldPosition);

        for (int depth = 4; depth >= 1; depth--)
        {
            Vector3Int sampleCell = cellPosition + new Vector3Int(-depth, -depth, 0);
            sampleCell.z = depth * 2 - 2;

            if (bmap.HasTile(sampleCell))
                return sampleCell.z + 2;
        }

        return 0;
    }

    private float ResolveGroundFloorZ(Vector3 worldPosition)
    {
        if (!useHeightTrajectory)
            return floorZ;

        ResolveGroundMap();
        return GetFloorZAtWorldPosition(grid, bmap, worldPosition);
    }

    private void UpdateRootPosition(Vector3 xyPosition)
    {
        transform.position = new Vector3(xyPosition.x, xyPosition.y, 0f);
    }
}
