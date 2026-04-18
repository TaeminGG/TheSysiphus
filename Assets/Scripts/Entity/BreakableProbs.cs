using UnityEngine;

public class BreakableProbs : MonoBehaviour
{
    [Header("Layer")]
    public LayerMask hitLayer;

    [Header("Prefab")]
    public GameObject dropItem;

    [Header("Gold")]
    public bool isGold = false;
    public int gold = 0;

    [Header("Z set")]
    public int floorZ = 0;
    public void Attacked()
    {
        if (dropItem == null || ShouldSuppressPassivePickupDrop())
        {
            Destroy(gameObject);
            return;
        }

        GameObject item = Instantiate(dropItem, transform.position, Quaternion.identity);
        PickupItem pickupItem = item.GetComponent<PickupItem>();
        pickupItem?.MarkPassiveReservationClaimed();

        if (isGold)
        {
            item.GetComponent<DroppedItem>().gold = gold;
        }

        item.GetComponent<DroppedItem>().SetZ(floorZ);

        Destroy(gameObject);
    }

    private bool ShouldSuppressPassivePickupDrop()
    {
        PickupItem pickupItem = dropItem.GetComponent<PickupItem>();
        if (pickupItem == null || !pickupItem.isPassive || pickupItem.item == null)
            return false;

        ItemUnlockRegistry registry = ItemUnlockRegistry.FindInstance();
        return registry != null && !registry.TryReservePassive(pickupItem.item);
    }
}
