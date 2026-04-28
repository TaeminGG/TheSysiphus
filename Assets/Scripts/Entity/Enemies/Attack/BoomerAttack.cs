using UnityEngine;

public class BoomerAttack : MonoBehaviour, IEnemyAttack
{
    public float range = 1.1f;
    public float fuseTime = 1f;
    public float telegraphRadius = 1f;
    public Color telegraphColor = new Color(1f, 0.35f, 0.15f, 0.4f);
    public GameObject explosionHitBoxPrefab;
    public AttackData explosionAttackData;

    private IEnemyMovement movement;
    private EnemyCrowdControl crowdControl;
    private PlayerMove playerMove;
    private EnemyStats enemyStats;
    private EnemyAttackTelegraph telegraph;

    private float timer = 0f;
    private bool exploded = false;

    public bool isAttacking = false;
    public bool IsBusy => isAttacking;
    public bool CanAttack => AttackRange();

    private void Start()
    {
        movement = GetComponent<IEnemyMovement>();
        crowdControl = GetComponent<EnemyCrowdControl>();
        playerMove = GameObject.Find("Player")?.GetComponent<PlayerMove>();
        enemyStats = GetComponent<EnemyStats>();
        telegraph = GetComponent<EnemyAttackTelegraph>();
        if (telegraph == null)
            telegraph = gameObject.AddComponent<EnemyAttackTelegraph>();
    }

    private void Update()
    {
        if (crowdControl != null && crowdControl.ShouldCancelCurrentAttack)
        {
            ResetAttackState();
            return;
        }

        if (!isAttacking && crowdControl != null && !crowdControl.CanAttack)
            return;

        if (CanAttack && !isAttacking)
            isAttacking = true;

        if (!isAttacking)
            return;

        timer += Time.deltaTime;
        if (!exploded && movement != null)
            telegraph.ShowCircleTelegraph(transform.position, telegraphRadius, movement.FloorZ, telegraphColor);

        if (!exploded && timer >= fuseTime)
        {
            exploded = true;
            telegraph?.Hide();
            Attack();
        }
    }

    public void Attack()
    {
        if (explosionHitBoxPrefab != null && explosionAttackData != null && movement != null)
        {
            GameObject hitBoxObject = Instantiate(explosionHitBoxPrefab, transform.position, Quaternion.identity);
            if (hitBoxObject.TryGetComponent(out AttackHitBox hitBox))
                hitBox.EnemySetup(explosionAttackData, explosionAttackData.damage, explosionAttackData.attackDuration, transform.position, 0f, movement.FloorZ);
        }

        enemyStats?.Kill();
    }

    private bool AttackRange()
    {
        if (playerMove == null)
            return false;

        if (crowdControl != null && !crowdControl.CanAttack)
            return false;

        if (isAttacking)
            return false;

        return Vector2.Distance(playerMove.transform.position, transform.position) <= range;
    }

    private void ResetAttackState()
    {
        telegraph?.Hide();
        timer = 0f;
        isAttacking = false;
        exploded = false;
    }
}
