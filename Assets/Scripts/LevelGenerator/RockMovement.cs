using UnityEngine;

public class RockMovement : MonoBehaviour
{
    public float moveSpeed = 1.0f;
    public LayerMask hitLayer;  // 플레이어 레이어만 탐색

    private bool isHit = false;
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        Vector3 targetPos = transform.position + Vector3.down * moveSpeed * Time.deltaTime;

        transform.position = targetPos;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayer) == 0) return;

        other.gameObject.GetComponent<PlayerStats>().HitByRock();
        isHit = true;
    }
}
