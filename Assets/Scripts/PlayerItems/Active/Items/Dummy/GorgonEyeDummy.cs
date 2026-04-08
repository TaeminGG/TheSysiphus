using UnityEngine;

public class GorgonEyeDummy : MonoBehaviour
{
    public LayerMask hitLayer;
    public float ccDuration = 5.0f;
    public float duration = 1.0f;
    private float timer = 0;
    private void Update()
    {
        timer += Time.deltaTime;

        if(timer > duration)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayer) == 0) return;

        if(other.TryGetComponent<IEnemyMovement>(out var target))
        {
            target.CrowdControl(IEnemyMovement.CC.Stun, 5);
        }
    }
}
