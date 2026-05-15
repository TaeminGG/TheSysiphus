using UnityEngine;

public class PlayerDetectionRange : MonoBehaviour
{
    public IEnemyMovement movement;
    public LayerMask hitLayer;


    private void Awake()
    {
        ResolveMovement();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayer) == 0) return;
        if (!ResolveMovement()) return;

        movement.SetPlayerDetected(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayer) == 0) return;
        if (!ResolveMovement()) return;

        movement.SetPlayerDetected(false);
    }

    private bool ResolveMovement()
    {
        if (movement != null)
            return true;

        movement = GetComponentInParent<IEnemyMovement>();
        return movement != null;
    }
}
