using UnityEngine;

public class ItemDrop : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject item;

    [Header("Gold")]
    public bool isGold = false;
    public int dropGold = 0;

    private void Start()
    {
        GetComponent<EnemyStats>().OnDeath += SpawnItem;
    }

    private void SpawnItem()
    {
        if (item != null)
        {
            GameObject prefab = Instantiate(item, transform.position, Quaternion.identity);
            if (isGold)
            {
                prefab.GetComponent<DroppedItem>().gold = dropGold;
            }
            else
            {
                
            }

            prefab.GetComponent<DroppedItem>().SetZ(GetComponent<IEnemyMovement>().FloorZ);
        }
    }
}
