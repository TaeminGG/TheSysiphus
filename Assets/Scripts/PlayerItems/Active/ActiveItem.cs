using UnityEngine;

public abstract class ActiveItem : ScriptableObject
{
    [SerializeField] private string itemId;
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;
    public string ItemId => string.IsNullOrWhiteSpace(itemId) ? name : itemId;

    public abstract void OnUsed();
}
