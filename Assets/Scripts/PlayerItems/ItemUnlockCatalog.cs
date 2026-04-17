using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemUnlockCatalog", menuName = "Scriptable Objects/Items/Unlock Catalog")]
public class ItemUnlockCatalog : ScriptableObject
{
    [SerializeField] private List<PassiveItem> passiveItems = new List<PassiveItem>();
    [SerializeField] private List<ActiveItem> activeItems = new List<ActiveItem>();

    private Dictionary<string, PassiveItem> passiveLookup;
    private Dictionary<string, ActiveItem> activeLookup;

    public IReadOnlyList<PassiveItem> PassiveItems => passiveItems;
    public IReadOnlyList<ActiveItem> ActiveItems => activeItems;

    public bool TryGetPassiveItem(string itemId, out PassiveItem item)
    {
        EnsureLookup();
        return passiveLookup.TryGetValue(itemId, out item);
    }

    public bool TryGetActiveItem(string itemId, out ActiveItem item)
    {
        EnsureLookup();
        return activeLookup.TryGetValue(itemId, out item);
    }

    private void OnEnable()
    {
        InvalidateLookup();
    }

    private void OnValidate()
    {
        InvalidateLookup();
    }

    private void EnsureLookup()
    {
        if (passiveLookup != null && activeLookup != null)
            return;

        passiveLookup = new Dictionary<string, PassiveItem>();
        activeLookup = new Dictionary<string, ActiveItem>();

        for (int i = 0; i < passiveItems.Count; i++)
        {
            PassiveItem item = passiveItems[i];
            if (item == null || string.IsNullOrWhiteSpace(item.ItemId) || passiveLookup.ContainsKey(item.ItemId))
                continue;

            passiveLookup.Add(item.ItemId, item);
        }

        for (int i = 0; i < activeItems.Count; i++)
        {
            ActiveItem item = activeItems[i];
            if (item == null || string.IsNullOrWhiteSpace(item.ItemId) || activeLookup.ContainsKey(item.ItemId))
                continue;

            activeLookup.Add(item.ItemId, item);
        }
    }

    private void InvalidateLookup()
    {
        passiveLookup = null;
        activeLookup = null;
    }
}
