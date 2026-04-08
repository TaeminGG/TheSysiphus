using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.U2D;
using System.Collections;

public class SpecterMovement : MonoBehaviour, IEnemyMovement
{
    public float moveSpeed = 1.0f;
    public int floorZ = 0;

    public int FloorZ => floorZ;

    public bool isDetected = false;    // 플레이어 포착 여부

    private Vector3 targetPos;

    public Transform sprite;
    private FloatingEnemy floatingEnemy;
    private EnemyStats stat;
    private SpecterAttack attack;
    private Transform player;
    private Grid grid;
    private Animator anim;

    private bool isArrive = true;      // 넉백 위치에 도착했는 지 여부
    private float knockbackDistance = 0;    // 기존 위치와 넉백 위치 사이 거리
    private Vector3 knockback = Vector3.zero;
    private Vector3 prevPos = Vector3.zero;

    [HideInInspector]
    public bool isStuned = false;
    void Awake()
    {
        floatingEnemy = GetComponent<FloatingEnemy>();
        stat = GetComponent<EnemyStats>();
        attack = GetComponent<SpecterAttack>();
        anim = sprite.GetComponent<Animator>();
    }

    void Start()
    {
        targetPos = transform.position;
        player = GameObject.Find("Player").transform;
        grid = FindAnyObjectByType<Grid>();
    }

    void Update()
    {
        floorZ = player.GetComponent<PlayerMove>().floorZ;
    }

    void FixedUpdate()
    {
        Move();
    }

    void LateUpdate()
    {
        if (targetPos.x < transform.position.x)
        {
            // 넉백, 공격 중에는 flip실행 X
            if (isArrive && !attack.isAttacking)
                sprite.GetComponent<SpriteRenderer>().flipX = true;
        }
        else
        {
            if (isArrive && !attack.isAttacking)
                sprite.GetComponent<SpriteRenderer>().flipX = false;
        }
    }

    public void Move()
    {
        PlayerMove playerMove = player.GetComponent<PlayerMove>();

        // 플레이어 포착 시 이동
        if (!isDetected || isStuned)
        {
            targetPos = transform.position;
            return;
        }

        // 넉백 상태 확인
        if (stat.isKnockback)
        {
            isArrive = false;
            stat.isKnockback = false;
            knockback = stat.knockbackVector;
            prevPos = transform.position;
            knockbackDistance = Vector3.Distance(prevPos, prevPos + knockback);
        }
        else
        {
            if (isArrive && !attack.isAttacking)
            {
                // 목표 지점까지 이동
                if (Vector3.Distance(transform.position, targetPos) > 0.05f)
                {
                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        targetPos,
                        moveSpeed * Time.deltaTime
                    );
                    return;
                }

                targetPos = transform.position + (playerMove.transform.position - transform.position).normalized;
            }
        }

        // 넉백 처리
        if (!isArrive)
        {
            targetPos = transform.position + (knockback.normalized * 10f * stat.knockBackResist * Time.deltaTime);

            transform.position = targetPos;
            if (Vector3.Distance(prevPos, transform.position) >= knockbackDistance)
            {
                isArrive = true;
            }
        }
    }

    public void CrowdControl(IEnemyMovement.CC cc, float t)
    {
        if (cc == IEnemyMovement.CC.Slow)
        {

        }
        if (cc == IEnemyMovement.CC.Stun)
        {
            isStuned = true;
            StartCoroutine(Stun(t));
        }
    }

    private IEnumerator Stun(float dur)
    {
        yield return new WaitForSeconds(dur);
        isStuned = false;
    }


    public void SetPlayerDetected(bool val)
    {
        isDetected = val;
    }
}
