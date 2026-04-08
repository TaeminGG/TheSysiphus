using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    public LayerMask hitLayer;

    public bool isGold = false;
    public int gold = 0;

    public int floorZ = 0;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetZ(int z)
    {
        floorZ = z;
        Vector3 pos = transform.position;
        pos.z = z;
        transform.position = pos;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayer) == 0) return;

        if ((hitLayer & (1 << LayerMask.NameToLayer("Player"))) != 0)
        {
            if (isGold)
            {
                if (other.TryGetComponent<PlayerStats>(out var player))
                {
                    player.AddGold(gold);
                }
            }
            else
            {

            }

            Destroy(gameObject);
        }
    }
}
