using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class AttackController : MonoBehaviour
{
    public float attackSpeed = 1.0f;        // 공격속도 ( 곱하기 : 1.0은 x1,  1.5는 x1.5 )

    private AttackData[] combos;

    private int currentCombo = 0;
    public bool isAttacking = false;
    public bool inputBuffered = false;     // 다음 공격 가능
    private float comboTimer = 0f;
    private float attackTimer = 0f;

    public AttackHitBox hitbox;
    public Animator anim;
    private PlayerMove playerMove;

    private bool isAnimPlayed = false;

    private void Awake()
    {
        playerMove = GetComponent<PlayerMove>();
    }

    private void Start()
    {
        attackSpeed = GetComponent<PlayerStats>().attackSpeed;
    }

    private void Update()
    {
        attackSpeed = GetComponent<PlayerStats>().attackSpeed;
        HandleTimer();
    }

    // 무기 장착 (변경)
    public void EquipWeapon(WeaponData weapon)
    {
        ResetCombo();
        combos = weapon.combos;
    }

    //
    public void OnAttackInput()
    {
        Debug.Log("is Left Clicked");
        // 무기 없으면 공격 불가
        if (combos == null || combos.Length == 0)
            return;

        if (!isAttacking)
        {
            StartAttack(0);
        }
        else
        {
            if (comboTimer > 0f && currentCombo < combos.Length - 1)
            {
                inputBuffered = true;
            }
        }
    }

    private void StartAttack(int combo)
    {
        playerMove.AttackLookAt();      // 플레이어가 바라보는 방향 설정

        currentCombo = combo;
        inputBuffered = false;
        isAttacking = true;         // 공격 중으로 상태 변경

        AttackData data = combos[currentCombo];

        // 공격 속도에 비례해서 attackDuration, comboInputwindow 감소
        float asDuration = data.attackDuration / attackSpeed;
        float asComboWindow = data.comboInputWindow / attackSpeed;

        attackTimer = asDuration;
        comboTimer = 0f;

        // 애니메이션 속도 공격속도에 비례해서 적용
        anim.SetFloat("AttackSpeed", attackSpeed);
        anim.SetTrigger(data.animTrigger);
        Vector2 attackDir = GetComponent<PlayerAttackDirection>().GetAttackDirection();
        float playerDamage = GetComponent<PlayerStats>().damage;
        hitbox.Setup(data, attackSpeed, playerDamage, attackDir, GetComponent<PlayerMove>().floorZ);
    }

    private void HandleTimer()
    {
        if (!isAttacking)
            return;

        AttackData data = combos[currentCombo];
        float asComboWindow = data.comboInputWindow / attackSpeed;

        attackTimer -= Time.deltaTime;

        // 다음 공격 허용 시간 계산
        if (attackTimer <= asComboWindow && comboTimer == 0f)
        { 
            comboTimer = asComboWindow; 
        }

        if(comboTimer > 0f)
        {
            comboTimer -= Time.deltaTime;
        }

        // 공격 종료
        if(attackTimer <= 0f)
        {
            if (inputBuffered && currentCombo < combos.Length - 1)
            {
                // 입력이 들어왔으면 다음 공격으로
                StartAttack(currentCombo + 1);
            }
            else
            {
                anim.SetTrigger("AttackEnded");
                anim.ResetTrigger(data.animTrigger);
                ResetCombo();
            }
        }
    }

    private void ResetCombo()
    {
        isAttacking = false;
        inputBuffered = false;
        currentCombo = 0;
        comboTimer = 0f;
        attackTimer = 0f;
        hitbox.DisableHitBox();
    }

    void OnAttack(InputValue value)
    {
        if(GetComponent<PlayerMove>().IsPlayerOnGround())
            OnAttackInput();
    }
}
