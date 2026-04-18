using System.Collections;
using System.Collections.Generic;
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
        if (!TryResolveLoot(out bool isPassive, out PassiveItem passiveItem, out ActiveItem activeItem))
            yield break;

        Vector3 start = transform.position;
        start.z = floorZ;

        Vector3 end = GetLandingPosition(start);
        GameObject spawnedObject = Instantiate(pickupItemPrefab, start, Quaternion.identity);
        PickupItem pickupItem = spawnedObject.GetComponent<PickupItem>();

        if (pickupItem == null)
        {
            ReleasePassiveReservation(isPassive, passiveItem);

            if (spawnedObject != null)
                Destroy(spawnedObject);

            yield break;
        }

        pickupItem.ConfigureDropItem(isPassive, passiveItem, activeItem, isPassive);
        pickupItem.SetZ(floorZ);

        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            if (spawnedObject == null)
                yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);
            float arc = Mathf.Sin(t * Mathf.PI) * popHeight;
            Vector3 pos = Vector3.Lerp(start, end, t);
            pos.y += arc;
            pos.z = floorZ;
            spawnedObject.transform.position = pos;
            yield return null;
        }

        if (spawnedObject != null)
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

    private bool TryResolveLoot(out bool isPassive, out PassiveItem passiveItem, out ActiveItem activeItem)
    {
        ItemUnlockRegistry registry = ItemUnlockRegistry.FindInstance();
        if (TryResolveLootFromPool(registry, out isPassive, out passiveItem, out activeItem))
            return true;

        List<ItemSpawnPool.RollResult> fallbackEntries = GetFallbackEntries(registry);
        while (fallbackEntries.Count > 0)
        {
            int selectedIndex = SelectWeightedIndex(fallbackEntries);
            if (selectedIndex < 0)
                break;

            ItemSpawnPool.RollResult selectedResult = fallbackEntries[selectedIndex];
            if (!selectedResult.IsPassive)
            {
                isPassive = false;
                passiveItem = null;
                activeItem = selectedResult.ActiveItem;
                return activeItem != null;
            }

            if (TryReservePassive(registry, selectedResult.PassiveItem))
            {
                isPassive = true;
                passiveItem = selectedResult.PassiveItem;
                activeItem = null;
                return true;
            }

            fallbackEntries.RemoveAt(selectedIndex);
        }

        isPassive = false;
        passiveItem = null;
        activeItem = null;
        return false;
    }

    private bool TryResolveLootFromPool(ItemUnlockRegistry registry, out bool isPassive, out PassiveItem passiveItem, out ActiveItem activeItem)
    {
        isPassive = false;
        passiveItem = null;
        activeItem = null;

        if (lootPool == null)
            return false;

        List<ItemSpawnPool.RollResult> unlockedEntries = lootPool.GetUnlockedEntries(registry);
        while (unlockedEntries.Count > 0)
        {
            int selectedIndex = SelectWeightedIndex(unlockedEntries);
            if (selectedIndex < 0)
                break;

            ItemSpawnPool.RollResult selectedResult = unlockedEntries[selectedIndex];
            if (!selectedResult.IsPassive)
            {
                isPassive = false;
                passiveItem = null;
                activeItem = selectedResult.ActiveItem;
                return activeItem != null;
            }

            if (TryReservePassive(registry, selectedResult.PassiveItem))
            {
                isPassive = true;
                passiveItem = selectedResult.PassiveItem;
                activeItem = null;
                return true;
            }

            unlockedEntries.RemoveAt(selectedIndex);
        }

        return false;
    }

    private static int SelectWeightedIndex(List<ItemSpawnPool.RollResult> entries)
    {
        float totalWeight = 0f;
        for (int i = 0; i < entries.Count; i++)
            totalWeight += Mathf.Max(0.01f, entries[i].Weight);

        if (totalWeight <= 0f)
            return -1;

        float roll = Random.value * totalWeight;
        float cumulativeWeight = 0f;

        for (int i = 0; i < entries.Count; i++)
        {
            cumulativeWeight += Mathf.Max(0.01f, entries[i].Weight);
            if (roll <= cumulativeWeight)
                return i;
        }

        return entries.Count - 1;
    }

    private static bool TryReservePassive(ItemUnlockRegistry registry, PassiveItem passiveItem)
    {
        return passiveItem != null && (registry == null || registry.TryReservePassive(passiveItem));
    }

    private static void ReleasePassiveReservation(bool isPassive, PassiveItem passiveItem)
    {
        if (!isPassive || passiveItem == null)
            return;

        ItemUnlockRegistry registry = ItemUnlockRegistry.FindInstance();
        registry?.ReleasePassiveReservation(passiveItem);
    }

    private List<ItemSpawnPool.RollResult> GetFallbackEntries(ItemUnlockRegistry registry)
    {
        List<ItemSpawnPool.RollResult> entries = new List<ItemSpawnPool.RollResult>();

        if (passiveItems != null)
        {
            for (int i = 0; i < passiveItems.Length; i++)
            {
                PassiveItem passiveItem = passiveItems[i];
                if (passiveItem == null)
                    continue;

                if (registry != null && registry.IsUnavailable(passiveItem))
                    continue;

                entries.Add(new ItemSpawnPool.RollResult
                {
                    IsPassive = true,
                    PassiveItem = passiveItem,
                    ActiveItem = null,
                    Price = 0,
                    Weight = 1f
                });
            }
        }

        if (activeItems != null)
        {
            for (int i = 0; i < activeItems.Length; i++)
            {
                ActiveItem activeItem = activeItems[i];
                if (activeItem == null)
                    continue;

                entries.Add(new ItemSpawnPool.RollResult
                {
                    IsPassive = false,
                    PassiveItem = null,
                    ActiveItem = activeItem,
                    Price = 0,
                    Weight = 1f
                });
            }
        }

        return entries;
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
