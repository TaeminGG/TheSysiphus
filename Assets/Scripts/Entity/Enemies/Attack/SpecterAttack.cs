using UnityEngine;

public class SpecterAttack : MonoBehaviour, IEnemyAttack
{
    private Animator anim;
    private SpecterMovement movement;
    private PlayerMove playerMove;

    private float floorZ = 0;

    public float range = 1.0f;

    public float readyTime = 0.5f;          // 공격 준비 시간
    private float damage = 5.0f;            // 공격 데미지
    private float duration = 0.5f;          // 히트박스 지속시간
    public float coolTime = 1.5f;           // 탐지 후(공격 준비 시작 후) 다음 공격까지 대기시간.
    private float timer = 0f;

    public bool isAttacking = false;
    private bool isAttacked = false;

    public GameObject projectile;           // 발사체 gameobject
    private AttackHitBox hitBox;
    public AttackData attackData;           // 발사체 data

    void Start()
    {
        movement = GetComponent<SpecterMovement>();
        playerMove = GameObject.Find("Player").GetComponent<PlayerMove>();
        floorZ = movement.floorZ;
    }

    void Update()
    {
        if (CanAttack && !isAttacking)
            isAttacking = true;

        if (isAttacking)
        {
            timer += Time.deltaTime;

            if ((timer >= readyTime) && !isAttacked)
            {
                isAttacked = true;
                Attack();
            }

            if (timer >= coolTime)
            {
                timer = 0;
                isAttacking = false;
                isAttacked = false;
            }
        }
    }

    public void Attack()
    {
        duration = attackData.attackDuration;
        damage = attackData.damage;

        Vector3 direction = (playerMove.transform.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        angle -= 90.0f;

        hitBox = Instantiate(projectile, transform.position, Quaternion.Euler(0, 0, angle)).GetComponent<AttackHitBox>();
        hitBox.EnemySetup(attackData, damage, duration, transform.position, angle, floorZ);
    }

    public bool CanAttack => AttackRange();

    private bool AttackRange()
    {
        // 스턴 상태면 공격 불가
        if (movement.isStuned)
            return false;

        if (!isAttacking)
        {
            float diff = Vector2.Distance(playerMove.transform.position, transform.position);

            if (diff <= range)
            {
                return true;
            }
        }

        return false;
    }
}
