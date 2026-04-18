using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class AttackController : MonoBehaviour
{
    public float attackSpeed = 1.0f;

    private AttackData[] combos;

    private int currentCombo = 0;
    public bool isAttacking = false;
    public bool inputBuffered = false;
    private float comboTimer = 0f;
    private float attackTimer = 0f;
    private bool hasReportedHitThisAttack = false;

    public AttackHitBox hitbox;
    public Animator anim;
    private PlayerMove playerMove;
    private PlayerCrowdControl crowdControl;
    private PlayerStats playerStats;

    public event System.Action OnAttackHit;

    private void Awake()
    {
        playerMove = GetComponent<PlayerMove>();
        playerStats = GetComponent<PlayerStats>();
        crowdControl = GetComponent<PlayerCrowdControl>();
        if (crowdControl == null)
            crowdControl = gameObject.AddComponent<PlayerCrowdControl>();

        if (hitbox != null)
            hitbox.OnEnemyHitConfirmed += HandleHitboxEnemyHit;
    }

    private void Start()
    {
        attackSpeed = GetComponent<PlayerStats>().attackSpeed;
    }

    private void Update()
    {
        attackSpeed = GetComponent<PlayerStats>().attackSpeed;
        if (crowdControl != null && !crowdControl.CanAttack)
        {
            if (isAttacking)
            {
                anim.SetTrigger("AttackEnded");
                ResetCombo();
            }

            return;
        }

        HandleTimer();
    }

    public void EquipWeapon(WeaponData weapon)
    {
        ResetCombo();
        combos = weapon.combos;
    }

    public void OnAttackInput()
    {
        Debug.Log("is Left Clicked");
        if (combos == null || combos.Length == 0)
            return;

        if (!isAttacking)
        {
            StartAttack(0);
        }
        else if (comboTimer > 0f && currentCombo < combos.Length - 1)
        {
            inputBuffered = true;
        }
    }

    private void StartAttack(int combo)
    {
        playerMove.AttackLookAt();

        currentCombo = combo;
        inputBuffered = false;
        isAttacking = true;
        hasReportedHitThisAttack = false;

        AttackData data = combos[currentCombo];
        attackTimer = data.attackDuration / attackSpeed;
        comboTimer = 0f;

        anim.SetFloat("AttackSpeed", attackSpeed);
        anim.SetTrigger(data.animTrigger);

        Vector2 attackDir = GetComponent<PlayerAttackDirection>().GetAttackDirection();
        float playerDamage = playerStats != null ? playerStats.damage : 0f;

        if (hitbox != null)
        {
            hitbox.SetPlayerOwner(playerStats);
            hitbox.Setup(data, attackSpeed, playerDamage, attackDir, playerMove.floorZ);
        }
    }

    private void HandleTimer()
    {
        if (!isAttacking)
            return;

        AttackData data = combos[currentCombo];
        float comboWindow = data.comboInputWindow / attackSpeed;

        attackTimer -= Time.deltaTime;

        if (attackTimer <= comboWindow && comboTimer == 0f)
            comboTimer = comboWindow;

        if (comboTimer > 0f)
            comboTimer -= Time.deltaTime;

        if (attackTimer > 0f)
            return;

        if (inputBuffered && currentCombo < combos.Length - 1)
        {
            StartAttack(currentCombo + 1);
            return;
        }

        anim.SetTrigger("AttackEnded");
        anim.ResetTrigger(data.animTrigger);
        ResetCombo();
    }

    private void ResetCombo()
    {
        isAttacking = false;
        inputBuffered = false;
        currentCombo = 0;
        comboTimer = 0f;
        attackTimer = 0f;
        hasReportedHitThisAttack = false;

        if (hitbox != null)
            hitbox.DisableHitBox();
    }

    private void HandleHitboxEnemyHit()
    {
        if (!isAttacking || hasReportedHitThisAttack)
            return;

        hasReportedHitThisAttack = true;
        OnAttackHit?.Invoke();
    }

    private void OnDestroy()
    {
        if (hitbox != null)
            hitbox.OnEnemyHitConfirmed -= HandleHitboxEnemyHit;

        OnAttackHit = null;
    }

    private void OnAttack(InputValue value)
    {
        if (crowdControl != null && !crowdControl.CanAttack)
            return;

        if (GetComponent<PlayerMove>().IsPlayerOnGround())
            OnAttackInput();
    }
}
