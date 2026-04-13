using UnityEngine;

public class SpecterAttack : MonoBehaviour, IEnemyAttack
{
    private Animator anim;
    private SpecterMovement movement;
    private EnemyCrowdControl crowdControl;
    private PlayerMove playerMove;

    private float floorZ = 0;

    public float range = 1.0f;

    public float readyTime = 0.5f;
    private float damage = 5.0f;
    private float duration = 0.5f;
    public float coolTime = 1.5f;
    private float timer = 0f;

    public bool isAttacking = false;
    private bool isAttacked = false;

    public GameObject projectile;
    private AttackHitBox hitBox;
    public AttackData attackData;

    void Start()
    {
        movement = GetComponent<SpecterMovement>();
        crowdControl = GetComponent<EnemyCrowdControl>();
        playerMove = GameObject.Find("Player").GetComponent<PlayerMove>();
        floorZ = movement.floorZ;
    }

    void Update()
    {
        if (crowdControl != null && !crowdControl.CanAttack)
        {
            ResetAttackState();
            return;
        }

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
                ResetAttackState();
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
        if (crowdControl != null && !crowdControl.CanAttack)
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

    private void ResetAttackState()
    {
        timer = 0f;
        isAttacking = false;
        isAttacked = false;
    }
}
