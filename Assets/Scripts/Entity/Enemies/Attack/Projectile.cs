using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float moveSpeed = 1.0f;
    public float lifeTime = 2.0f;
    private float timer = 0f;

    private void Update()
    {
        timer += Time.deltaTime;

        if(timer > lifeTime )
        {
            Destroy(gameObject);
        }
    }
    private void FixedUpdate()
    {
        transform.position += transform.up * moveSpeed * Time.deltaTime;
    }
}
