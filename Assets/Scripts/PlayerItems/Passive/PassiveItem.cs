using UnityEngine;

public abstract class PassiveItem : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;

    public abstract void OnEquip(PlayerStats stats);

    public abstract void OnUnequip(PlayerStats stats);

}