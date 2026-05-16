using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    public LayerMask hitLayer;

    public bool isGold = false;
    public int gold = 0;

    public int floorZ = 0;
    private RunSaveIdentity runSaveIdentity;

    private void Awake()
    {
        runSaveIdentity = RunSaveIdentity.Ensure(gameObject);
    }

    void Start()
    {
        
    }

    public void SetZ(int z)
    {
        floorZ = z;
        Vector3 pos = transform.position;
        pos.z = z;
        transform.position = pos;
    }

    public void RestoreRunState(bool goldDrop, int goldAmount, int z)
    {
        isGold = goldDrop;
        gold = Mathf.Max(0, goldAmount);
        SetZ(z);
    }

    public string GetSaveId()
    {
        runSaveIdentity ??= RunSaveIdentity.Ensure(gameObject);
        return runSaveIdentity != null ? runSaveIdentity.SaveId : gameObject.name;
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

            RunSaveRuntimeState.MarkPickupConsumed(GetSaveId());
            Destroy(gameObject);
        }
    }
}
