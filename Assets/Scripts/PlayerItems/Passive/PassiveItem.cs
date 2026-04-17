using UnityEngine;

public abstract class PassiveItem : ScriptableObject
{
    [SerializeField] private string itemId;
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;
    public string ItemId => string.IsNullOrWhiteSpace(itemId) ? name : itemId;

    public abstract void OnEquip(PlayerStats stats);

    public abstract void OnUnequip(PlayerStats stats);

}
