using UnityEngine;

public class BreakableProbs : MonoBehaviour
{
    [Header("Layer")]
    public LayerMask hitLayer;

    [Header("Prefab")]
    public GameObject dropItem;

    [Header("Gold")]
    public bool isGold = false;
    public int gold = 0;

    [Header("Z set")]
    public int floorZ = 0;
    public void Attacked()
    {
        GameObject item = Instantiate(dropItem, transform.position, Quaternion.identity);

        if (isGold)
        {
            item.GetComponent<DroppedItem>().gold = gold;
        }

        item.GetComponent<DroppedItem>().SetZ(floorZ);

        Destroy(gameObject);
    }
}
