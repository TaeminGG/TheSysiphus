using UnityEngine;
using System.Collections.Generic;

public class PassiveItemList : MonoBehaviour
{
    private List<PassiveItem> items = new List<PassiveItem>();
    private PlayerStats stats;

    public List<PassiveItem> testitems = new List<PassiveItem>();
    private void Start()
    {
        foreach (PassiveItem item in testitems)
        {
            AddItem(item);
        }
    }

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }
    public void AddItem(PassiveItem item)
    {
        if (item == null || items.Contains(item))
            return;

        items.Add(item);
        item.OnEquip(stats);
    }

    public void RemoveItem(PassiveItem item) 
    {
        if (items.Contains(item))
        {
            item.OnUnequip(stats);
            items.Remove(item);
        }
    }

    public List<PassiveItem> GetPassvieItems()
    {
        return items;
    }

    public void RestoreItems(IEnumerable<PassiveItem> restoredItems, PlayerStats restoredStats = null)
    {
        for (int i = items.Count - 1; i >= 0; i--)
            items[i]?.OnUnequip(stats);

        items.Clear();
        stats = restoredStats != null ? restoredStats : GetComponent<PlayerStats>();

        if (restoredItems == null)
            return;

        foreach (PassiveItem item in restoredItems)
            AddItem(item);
    }
}
