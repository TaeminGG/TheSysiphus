using UnityEngine;

public class UndeadAttack : MonoBehaviour, IEnemyAttack
{
    private Animator anim;
    private UndeadMovement movement;
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
    private Vector2 lockedAttackDirection = Vector2.down;
    private float lockedAttackAngle = 180f;

    public AttackHitBox hitBox;
    public AttackData attackData;
    private EnemyAttackTelegraph telegraph;

    void Start()
    {
        movement = GetComponent<UndeadMovement>();
        crowdControl = GetComponent<EnemyCrowdControl>();
        playerMove = GameObject.Find("Player").GetComponent<PlayerMove>();
        floorZ = movement.floorZ;
        telegraph = GetComponent<EnemyAttackTelegraph>();
        if (telegraph == null)
            telegraph = gameObject.AddComponent<EnemyAttackTelegraph>();
    }

    void Update()
    {
        if (crowdControl != null && crowdControl.ShouldCancelCurrentAttack)
        {
            ResetAttackState();
            return;
        }

        if (!isAttacking && crowdControl != null && !crowdControl.CanAttack)
            return;

        if (CanAttack && !isAttacking)
        {
            CacheAttackDirection();
            isAttacking = true;
        }

        if (isAttacking)
        {
            timer += Time.deltaTime;

            if (!isAttacked)
                UpdateTelegraph();

            if ((timer >= readyTime) && !isAttacked)
            {
                isAttacked = true;
                telegraph.Hide();
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
        hitBox.EnemySetup(attackData, damage, duration, transform.position, lockedAttackAngle, floorZ);
    }

    public bool CanAttack => AttackRange();

    private void UpdateTelegraph()
    {
        if (telegraph == null || attackData == null || playerMove == null)
            return;

        telegraph.ShowHitBoxTelegraph(transform.position, attackData, lockedAttackAngle, Mathf.RoundToInt(floorZ));
    }

    private bool AttackRange()
    {
        if (crowdControl != null && !crowdControl.CanAttack)
            return false;

        if (playerMove.floorZ == floorZ && !isAttacking)
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
        telegraph?.Hide();
        timer = 0f;
        isAttacking = false;
        isAttacked = false;
        lockedAttackDirection = Vector2.down;
        lockedAttackAngle = 180f;
    }

    private void CacheAttackDirection()
    {
        if (playerMove == null)
            return;

        Vector2 direction = playerMove.transform.position - transform.position;
        if (direction.sqrMagnitude <= 0.001f)
            direction = Vector2.down;

        lockedAttackDirection = direction.normalized;
        lockedAttackAngle = Mathf.Atan2(lockedAttackDirection.y, lockedAttackDirection.x) * Mathf.Rad2Deg - 90f;
    }
}
