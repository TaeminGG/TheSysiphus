using System.Collections;
using UnityEngine;

public class PickupChest : MonoBehaviour, IPlayerInteractable
{
    [Header("Layer")]
    public LayerMask hitLayer;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    public Sprite openedSprite;
    public Canvas canvas;
    [SerializeField] private int interactionPriority = 20;

    [Header("Loot")]
    public ItemSpawnPool lootPool;
    public GameObject pickupItemPrefab;
    public PassiveItem[] passiveItems;
    public ActiveItem[] activeItems;

    [Header("Drop")]
    public Vector2 dropDirection = new Vector2(-1f, -1f).normalized;
    public float dropDistance = 0.8f;
    public float popHeight = 0.6f;
    public float popDuration = 0.35f;
    public int floorZ = 0;

    private PlayerMove playerMove;
    private bool isPlayerInRange = false;
    private bool isOpened = false;

    public int InteractionPriority => interactionPriority;
    public Vector3 InteractionPosition => transform.position;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (canvas != null)
            canvas.enabled = false;

        transform.position = new Vector3(transform.position.x, transform.position.y, floorZ + 1);
    }

    private void OnDisable()
    {
        UnregisterInteractable();
    }

    public bool CanInteract(PlayerMove player)
    {
        return !isOpened && isPlayerInRange && playerMove == player && player != null && player.floorZ == floorZ;
    }

    public void Interact(PlayerMove player)
    {
        if (!CanInteract(player) || pickupItemPrefab == null)
            return;

        if (canvas != null)
            canvas.enabled = false;

        isOpened = true;
        UnregisterInteractable();

        if (openedSprite != null && spriteRenderer != null)
            spriteRenderer.sprite = openedSprite;

        StartCoroutine(SpawnPickupItem());
    }

    public void SetInteractionFocus(PlayerMove player, bool isFocused)
    {
        RefreshCanvas();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isOpened || ((1 << other.gameObject.layer) & hitLayer) == 0)
            return;

        if (!other.TryGetComponent(out PlayerMove enteringPlayer))
            return;

        RefreshInteractionState(enteringPlayer);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (isOpened || ((1 << other.gameObject.layer) & hitLayer) == 0)
            return;

        if (!other.TryGetComponent(out PlayerMove stayingPlayer))
            return;

        RefreshInteractionState(stayingPlayer);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayer) == 0)
            return;

        if (!other.TryGetComponent(out PlayerMove exitingPlayer))
            return;

        if (playerMove != exitingPlayer)
            return;

        isPlayerInRange = false;

        if (canvas != null)
            canvas.enabled = false;

        UnregisterInteractable();
    }

    public void SetZ(int z)
    {
        floorZ = z;
        Vector3 pos = transform.position;
        pos.z = z;
        transform.position = pos;
    }

    private IEnumerator SpawnPickupItem()
    {
        Vector3 start = transform.position;
        start.z = floorZ;

        Vector3 end = GetLandingPosition(start);
        GameObject spawnedObject = Instantiate(pickupItemPrefab, start, Quaternion.identity);
        PickupItem pickupItem = spawnedObject.GetComponent<PickupItem>();

        if (pickupItem != null)
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
            spawnedObject.transform.position = pos;
            yield return null;
        }

        spawnedObject.transform.position = end;
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
        if (lootPool != null && lootPool.TryRollChestLoot(ItemUnlockRegistry.FindInstance(), out ItemSpawnPool.RollResult rollResult))
        {
            pickupItem.isPassive = rollResult.IsPassive;
            pickupItem.item = rollResult.PassiveItem;
            pickupItem.activeItem = rollResult.ActiveItem;
            return;
        }

        bool hasPassive = passiveItems != null && passiveItems.Length > 0;
        bool hasActive = activeItems != null && activeItems.Length > 0;

        if (!hasPassive && !hasActive)
            return;

        bool dropPassive = hasPassive && (!hasActive || Random.value < 0.5f);
        if (dropPassive)
        {
            pickupItem.isPassive = true;
            pickupItem.item = passiveItems[Random.Range(0, passiveItems.Length)];
        }
        else
        {
            pickupItem.isPassive = false;
            pickupItem.activeItem = activeItems[Random.Range(0, activeItems.Length)];
        }
    }

    private void RefreshInteractionState(PlayerMove targetPlayer)
    {
        playerMove = targetPlayer;
        isPlayerInRange = true;

        RefreshCanvas();

        if (CanInteract(targetPlayer))
        {
            targetPlayer.RegisterInteractable(this);
            return;
        }

        targetPlayer.UnregisterInteractable(this);
    }

    private void RefreshCanvas()
    {
        if (canvas != null)
            canvas.enabled = !isOpened && isPlayerInRange && playerMove != null && playerMove.floorZ == floorZ;
    }

    private void UnregisterInteractable()
    {
        if (playerMove != null)
            playerMove.UnregisterInteractable(this);
    }
}
