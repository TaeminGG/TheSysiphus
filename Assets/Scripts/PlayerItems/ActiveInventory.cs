using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine.InputSystem;

public class ActiveInventory : MonoBehaviour
{
    private List<ActiveItem> items = new List<ActiveItem>();
    public int inventorySize = 1;

    public float cooltime = 1.0f;
    private float timer = 0f;
    private bool isUsed = false;

    public bool isChanged = false;

    public GameObject dropItem;
    private PlayerCrowdControl crowdControl;

    private void Awake()
    {
        crowdControl = GetComponent<PlayerCrowdControl>();
        if (crowdControl == null)
            crowdControl = gameObject.AddComponent<PlayerCrowdControl>();
    }

    private void Update()
    {
        if (isUsed)
        {
            timer += Time.deltaTime;
            if (timer > cooltime)
            {
                isUsed = false;
            }
        }
    }

    public void AddItem(ActiveItem item)
    {
        if (items.Count >= inventorySize)
        {
            Vector2 offset = Random.insideUnitCircle.normalized * 0.3f;
            Vector3 dropPosition = transform.position + (Vector3)offset;
            PickupItem dropped = Instantiate(dropItem, dropPosition, Quaternion.identity).GetComponent<PickupItem>();

            dropped.ConfigureDropItem(false, null, items[0]);
            dropped.SetZ(GetComponent<PlayerMove>().floorZ);
            RemoveItem(items[0]);
        }
        items.Insert(0, item);
    }
    public void RemoveItem(ActiveItem item)
    {
        if (items.Contains(item))
        {
            items.Remove(item);
        }
    }

    public void UseCurrentItem()
    {
        if (crowdControl != null && !crowdControl.CanUseItems)
            return;

        if (!isUsed && items.Count > 0)
        {
            isUsed = true;
            items[0].OnUsed();
            RemoveItem(items[0]);
        }
    }

    public void NextItem()
    {
        if(items.Count > 1)
        {
            ActiveItem temp = items[0];
            items.RemoveAt(0);
            items.Add(temp);

            isChanged = true;

            Debug.Log("Current Item : " + items[0].name);
        }

    }

    public void PrevItem()
    {
        if( items.Count > 1)
        {
            ActiveItem temp = items.Last();
            items.RemoveAt(items.Count - 1);
            items.Insert(0, temp);

            isChanged = true;

            Debug.Log("Current Item : " + items[0].name);
        }
    }

    public List<ActiveItem> GetItems()
    {
        return items;
    }

    private void OnActiveUse(InputValue value)
    {
        UseCurrentItem();
    }
    private void OnPrevious(InputValue value)
    {
        PrevItem();
    }

    public void OnNext(InputValue value)
    {
        NextItem();
    }
}
