using UnityEngine;

public abstract class PassiveItem : ScriptableObject
{
    [SerializeField] private string itemId;
    [Tooltip("Default rarity used when a PickupItem reads rarity directly from this passive item asset")]
    [SerializeField] private ItemRarity rarity = ItemRarity.Common;
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;
    public string ItemId => string.IsNullOrWhiteSpace(itemId) ? name : itemId;
    public ItemRarity Rarity => rarity;

    public abstract void OnEquip(PlayerStats stats);

    public abstract void OnUnequip(PlayerStats stats);

}
