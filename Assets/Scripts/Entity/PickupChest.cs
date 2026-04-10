using System.Collections;
using UnityEngine;

public class PickupChest : MonoBehaviour
{
    [Header("Layer")]
    public LayerMask hitLayer;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    public Sprite openedSprite;
    public Canvas canvas;

    [Header("Loot")]
    public GameObject pickupItemPrefab;
    public PassiveItem[] passiveItems;
    public ActiveItem[] activeItems;

    [Header("Drop")]
    public Vector2 dropDirection = new Vector2(-1, -1).normalized;
    public float dropDistance = 0.8f;
    public float popHeight = 0.6f;
    public float popDuration = 0.35f;
    public int floorZ = 0;

    private PlayerMove playerMove;
    private bool isPlayerInRange = false;
    private bool isOpened = false;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        canvas.enabled = false;
        transform.position = new Vector3(transform.position.x, transform.position.y, floorZ + 1);
    }

    private void OnDisable()
    {
        PlayerMove.OnPickup -= TryOpen;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isOpened) return;
        if (((1 << other.gameObject.layer) & hitLayer) == 0) return;
        if (!other.TryGetComponent<PlayerMove>(out var enteringPlayer)) return;

        canvas.enabled = true;

        playerMove = enteringPlayer;
        isPlayerInRange = true;

        PlayerMove.OnPickup -= TryOpen;
        PlayerMove.OnPickup += TryOpen;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayer) == 0) return;
        if (!other.TryGetComponent<PlayerMove>(out var exitingPlayer)) return;
        if (playerMove != exitingPlayer) return;

        canvas.enabled = false;

        isPlayerInRange = false;
        playerMove = null;
        PlayerMove.OnPickup -= TryOpen;
    }

    public void SetZ(int z)
    {
        floorZ = z;
        Vector3 pos = transform.position;
        pos.z = z;
        transform.position = pos;
    }

    private void TryOpen()
    {
        if (isOpened) return;
        if (!isPlayerInRange || playerMove == null) return;
        if (playerMove.floorZ != floorZ) return;
        if (pickupItemPrefab == null) return;

        canvas.enabled = false;
        isOpened = true;
        PlayerMove.OnPickup -= TryOpen;

        if (openedSprite != null && spriteRenderer != null)
            spriteRenderer.sprite = openedSprite;

        StartCoroutine(SpawnPickupItem());
    }

    private IEnumerator SpawnPickupItem()
    {
        Vector3 start = transform.position;
        start.z = floorZ;

        Vector3 end = GetLandingPosition(start);
        GameObject spawned = Instantiate(pickupItemPrefab, start, Quaternion.identity);

        if (spawned.TryGetComponent(out PickupItem pickupItem))
        {
            ApplyLoot(pickupItem);
            pickupItem.SetZ(floorZ);
        }

        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);
            float arc = Mathf.Sin(t * Mathf.PI) * popHeight;
            Vector3 pos = Vector3.Lerp(start, end, t);
            pos.y += arc;
            pos.z = floorZ;
            spawned.transform.position = pos;
            yield return null;
        }

        spawned.transform.position = end;
    }

    private Vector3 GetLandingPosition(Vector3 start)
    {
        Vector2 direction = dropDirection.sqrMagnitude > 0.0001f ? dropDirection.normalized : Vector2.down;
        Vector3 candidate = start + (Vector3)(direction * dropDistance);
        candidate.z = floorZ;

        if (IsSameFloor(candidate))
            return candidate;

        return start;
    }

    private bool IsSameFloor(Vector3 position)
    {
        if (playerMove == null || !playerMove.TryGetComponent(out SurroundTileCheck tileCheck))
            return true;

        int groundZ = tileCheck.GetGroundZLevel(position);
        int landingFloorZ = groundZ < 0 ? 0 : groundZ + 2;
        return landingFloorZ == floorZ;
    }

    private void ApplyLoot(PickupItem pickupItem)
    {
        bool hasPassive = passiveItems != null && passiveItems.Length > 0;
        bool hasActive = activeItems != null && activeItems.Length > 0;

        if (!hasPassive && !hasActive)
            return;

        bool dropPassive = hasPassive && (!hasActive || Random.value < 0.5f);
        if (dropPassive)
        {
            pickupItem.isPassive = true;
            pickupItem.item = passiveItems[Random.Range(0, passiveItems.Length)];
            return;
        }

        pickupItem.isPassive = false;
        pickupItem.activeItem = activeItems[Random.Range(0, activeItems.Length)];
    }
}
