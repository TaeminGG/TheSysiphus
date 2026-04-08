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
}
