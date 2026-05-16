using UnityEngine;

public class BreakableProbs : MonoBehaviour
{
    [Header("Layer")]
    public LayerMask hitLayer;

    [Header("Prefab")]
    public GameObject dropItem;
    public GameObject breakEffect;
    public float breakEffectLifetime = 0.5f;

    [Header("Gold")]
    public bool isGold = false;
    public int gold = 0;

    [Header("Z set")]
    public int floorZ = 0;
    public void Attacked()
    {
        PlayBreakEffect();
        RunSaveIdentity sourceIdentity = GetComponent<RunSaveIdentity>();

        if (dropItem == null || ShouldSuppressPassivePickupDrop())
        {
            ConsumeOrDestroy();
            return;
        }

        GameObject item = Instantiate(dropItem, transform.position, Quaternion.identity);
        if (sourceIdentity != null)
            RunSaveIdentity.Ensure(item, $"{sourceIdentity.SaveId}:drop", dropItem.name);

        PickupItem pickupItem = item.GetComponent<PickupItem>();
        pickupItem?.MarkPassiveReservationClaimed();

        if (isGold)
        {
            item.GetComponent<DroppedItem>().gold = gold;
        }

        item.GetComponent<DroppedItem>().SetZ(floorZ);

        ConsumeOrDestroy();
    }

    private void ConsumeOrDestroy()
    {
        RunSaveWorldObject worldObject = GetComponent<RunSaveWorldObject>();
        if (worldObject != null)
        {
            worldObject.MarkConsumed();
            return;
        }

        Destroy(gameObject);
    }

    private void PlayBreakEffect()
    {
        if (breakEffect == null)
            return;

        GameObject effect = Instantiate(breakEffect, transform.position, Quaternion.identity);
        TemporaryAttackLifetime lifetime = effect.GetComponent<TemporaryAttackLifetime>();
        if (lifetime != null)
            lifetime.Begin(breakEffectLifetime);
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
