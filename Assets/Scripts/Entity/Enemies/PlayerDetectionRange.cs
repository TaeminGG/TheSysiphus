using UnityEngine;

public class PlayerDetectionRange : MonoBehaviour
{
    public IEnemyMovement movement;
    public LayerMask hitLayer;


    private void Start()
    {
        movement = transform.parent.GetComponent<IEnemyMovement>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayer) == 0) return;

        movement.SetPlayerDetected(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayer) == 0) return;

        movement.SetPlayerDetected(false);
    }
}
