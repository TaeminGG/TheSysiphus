using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttackDirection : MonoBehaviour
{
    private Camera cam;
    private Vector2 attackDirection;

    private Vector2 mousePos;

    private void Awake()
    {
        cam = Camera.main;
    }


    void Update()
    {
        UpdateAttackDirection();
    }

    private void UpdateAttackDirection()
    {
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(mousePos);

        // 플레이어와 마우스 사이 방향
        attackDirection = (mouseWorldPos - transform.position).normalized;
    }

    public Vector2 GetAttackDirection()
    {
        return attackDirection;
    }
    public float GetAttackAngle()
    {
        return Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;
    }


    void OnCursor(InputValue value)
    {
        mousePos = value.Get<Vector2>();
    }
}
