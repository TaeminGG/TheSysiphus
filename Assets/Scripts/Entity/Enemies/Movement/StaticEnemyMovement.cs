using UnityEngine;

public class StaticEnemyMovement : MonoBehaviour, IEnemyMovement
{
    public int floorZ = 0;
    public Transform sprite;

    private EnemyCrowdControl crowdControl;
    private bool isDetected = false;

    public int FloorZ => floorZ;

    private void Awake()
    {
        crowdControl = GetComponent<EnemyCrowdControl>();
        if (crowdControl == null)
            crowdControl = gameObject.AddComponent<EnemyCrowdControl>();
    }

    private void LateUpdate()
    {
        if (sprite == null)
            return;

        Vector3 spritePos = sprite.localPosition;
        spritePos.z = floorZ;
        sprite.localPosition = spritePos;
    }

    public void Move()
    {
        if (crowdControl != null && crowdControl.IsKnockbackActive)
            crowdControl.ClearKnockback();
    }

    public void SetPlayerDetected(bool val)
    {
        isDetected = val;
    }

    public void ApplyCrowdControl(EnemyCrowdControlRequest request)
    {
        if (crowdControl == null)
            crowdControl = GetComponent<EnemyCrowdControl>() ?? gameObject.AddComponent<EnemyCrowdControl>();

        crowdControl.ApplyEffect(request);
    }

    private void FixedUpdate()
    {
        if (!isDetected)
            return;

        Move();
    }
}
