using UnityEngine;

public abstract class ActiveItem : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;

    public abstract void OnUsed();
}
