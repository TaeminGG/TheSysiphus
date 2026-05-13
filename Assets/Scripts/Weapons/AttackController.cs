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
    private float currentAttackSpeed = 1f;
    private bool hasReportedHitThisAttack = false;

    public AttackHitBox hitbox;
    public Animator anim;
    private PlayerMove playerMove;
    private PlayerDirectionalAnimator directionalAnimator;
    private PlayerCrowdControl crowdControl;
    private PlayerStats playerStats;

    public event System.Action OnAttackHit;

    private void Awake()
    {
        playerMove = GetComponent<PlayerMove>();
        directionalAnimator = GetComponent<PlayerDirectionalAnimator>();
        if (directionalAnimator == null)
            directionalAnimator = gameObject.AddComponent<PlayerDirectionalAnimator>();
        directionalAnimator.Initialize(anim, null);
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
                if (directionalAnimator != null)
                    directionalAnimator.EndAttack();
                else if (anim != null)
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
        currentAttackSpeed = AttackAnimationTiming.GetSafeAttackSpeed(attackSpeed);
        float attackDuration = GetScaledAttackDuration(data, currentAttackSpeed);
        attackTimer = attackDuration;
        comboTimer = 0f;

        Vector2 attackDir = GetComponent<PlayerAttackDirection>().GetAttackDirection();
        float animationPlaybackSpeed = GetAnimationPlaybackSpeed(data, attackDir, attackDuration);
        if (directionalAnimator != null)
        {
            directionalAnimator.PlayAttack(data.animTrigger, attackDir, animationPlaybackSpeed);
        }
        else if (anim != null)
        {
            anim.SetFloat("AttackSpeed", animationPlaybackSpeed);
            anim.SetTrigger(data.animTrigger);
        }

        float playerDamage = playerStats != null ? playerStats.damage : 0f;

        if (hitbox != null)
        {
            hitbox.SetPlayerOwner(playerStats);
            hitbox.Setup(data, currentAttackSpeed, playerDamage, attackDir, playerMove.floorZ);
        }
    }

    private float GetScaledAttackDuration(AttackData data, float speed)
    {
        return AttackAnimationTiming.GetScaledAttackDuration(data.attackDuration, speed);
    }

    private float GetAnimationPlaybackSpeed(AttackData data, Vector2 attackDir, float targetDuration)
    {
        if (directionalAnimator == null)
            return currentAttackSpeed;

        float clipLength = directionalAnimator.GetAttackClipLength(data.animationClipNamePrefix, data.animTrigger, attackDir);
        return AttackAnimationTiming.GetPlaybackSpeed(clipLength, targetDuration, currentAttackSpeed);
    }

    private void HandleTimer()
    {
        if (!isAttacking)
            return;

        AttackData data = combos[currentCombo];
        float comboWindow = AttackAnimationTiming.GetScaledComboWindow(data.comboInputWindow, currentAttackSpeed);

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

        if (directionalAnimator != null)
        {
            directionalAnimator.EndAttack();
            directionalAnimator.ResetAttackTrigger(data.animTrigger);
        }
        else if (anim != null)
        {
            anim.SetTrigger("AttackEnded");
            anim.ResetTrigger(data.animTrigger);
        }
        ResetCombo();
    }

    private void ResetCombo()
    {
        isAttacking = false;
        inputBuffered = false;
        currentCombo = 0;
        comboTimer = 0f;
        attackTimer = 0f;
        currentAttackSpeed = AttackAnimationTiming.GetSafeAttackSpeed(attackSpeed);
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
