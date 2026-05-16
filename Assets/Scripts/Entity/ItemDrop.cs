using UnityEngine;

public class ItemDrop : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject item;

    [Header("Gold")]
    public bool isGold = false;
    public int dropGold = 0;

    private void Start()
    {
        GetComponent<EnemyStats>().OnDeath += SpawnItem;
    }

    private void SpawnItem()
    {
        if (item == null || ShouldSuppressPassivePickupDrop())
            return;

        GameObject prefab = Instantiate(item, transform.position, Quaternion.identity);
        RunSaveIdentity sourceIdentity = GetComponent<RunSaveIdentity>();
        if (sourceIdentity != null)
            RunSaveIdentity.Ensure(prefab, $"{sourceIdentity.SaveId}:drop", item.name);

        PickupItem pickupItem = prefab.GetComponent<PickupItem>();
        pickupItem?.MarkPassiveReservationClaimed();

        if (isGold)
        {
            prefab.GetComponent<DroppedItem>().gold = dropGold;
        }

        prefab.GetComponent<DroppedItem>().SetZ(GetComponent<IEnemyMovement>().FloorZ);
    }

    private bool ShouldSuppressPassivePickupDrop()
    {
        PickupItem pickupItem = item.GetComponent<PickupItem>();
        if (pickupItem == null || !pickupItem.isPassive || pickupItem.item == null)
            return false;

        ItemUnlockRegistry registry = ItemUnlockRegistry.FindInstance();
        return registry != null && !registry.TryReservePassive(pickupItem.item);
    }
}
