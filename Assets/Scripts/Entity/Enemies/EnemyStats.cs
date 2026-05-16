using System;
using System.Collections;
using UnityEngine;

public class EnemyStats : MonoBehaviour, IDamageable, IStatusTickDamageable
{
    public float maxHp = 100f;
    private float currentHp = 0f;
    public float knockBackResist = 1f;      // 넉백 저항력 (넉백속도 및 넉백 거리에 영향, 1f 일 때 최대 낮을수록 저항함)

    public event Action<float> OnDamaged;
    public event Action OnDeath;

    private IEnemyAttack attack;
    private IEnemyMovement movement;
    private EnemyCrowdControl crowdControl;
    private EnemyDirectionalAnimator directionalAnimator;
    private StatusAilmentController statusAilmentController;

    public Canvas canvas;
    public EnemyHpBar hpbar;
    public GameObject damageText;
    public Transform damagePos;             // 피격 시 데미지가 표시될 위치

    [Header("Death Animation")]
    public Animator deathAnimator;
    public string deathAnimationTrigger = "";
    public string deathAnimationStateName = "Death";
    public float deathDestroyDelay = 0f;

    private bool isHandlingDeath;
    private bool hasFinalizedDeath;
    public bool IsDead => currentHp <= 0;
    void Awake()
    {
        currentHp = maxHp;

        movement = GetComponent<IEnemyMovement>();
        attack = GetComponent<IEnemyAttack>();
        directionalAnimator = GetComponent<EnemyDirectionalAnimator>();
        crowdControl = GetComponent<EnemyCrowdControl>();
        if (crowdControl == null)
            crowdControl = gameObject.AddComponent<EnemyCrowdControl>();
        statusAilmentController = GetComponent<StatusAilmentController>();
        if (statusAilmentController == null)
            statusAilmentController = gameObject.AddComponent<StatusAilmentController>();
    }

    void Start()
    {
        OnDeath += EnemyDead;
        OnDamaged += ShowDamage;
    }

    // Update is called once per frame
    void Update()
    {
        if(IsDead) return;

        hpbar.HpSetting(currentHp / maxHp);
    }

    // 피격
    public void TakeDamage(float damage, Vector2 knockback)
    {
        if (IsDead) return;

        // 체력 감소 및 피격 이벤트 실행
        currentHp -= damage;
        OnDamaged?.Invoke(damage);
        if (crowdControl != null)
            crowdControl.ApplyKnockback(knockback * knockBackResist);

        // 넉백 처리

        // 넉백 저항에 따라 넉백 Vector 조절

        if (IsDead)
            BeginDeathSequence();
    }

    public void TakeStatusTickDamage(float damage)
    {
        TakeDamage(damage, Vector2.zero);
    }

    public void Kill()
    {
        if (IsDead)
            return;

        currentHp = 0f;
        BeginDeathSequence();
    }

    public float GetCurrentHp()
    {
        return currentHp;
    }

    public void RestoreRunState(float savedMaxHp, float savedCurrentHp)
    {
        maxHp = Mathf.Max(1f, savedMaxHp);
        currentHp = Mathf.Clamp(savedCurrentHp, 0f, maxHp);
    }

    public void ApplyCrowdControl(EnemyCrowdControlRequest request)
    {
        if (crowdControl == null)
            crowdControl = GetComponent<EnemyCrowdControl>();

        if (crowdControl == null)
            crowdControl = gameObject.AddComponent<EnemyCrowdControl>();

        crowdControl.ApplyEffect(request);
    }

    public void ApplyStun(float duration)
    {
        ApplyCrowdControl(EnemyCrowdControlRequest.CreateStun(duration));
    }

    public void ApplySlow(float duration, float moveSpeedMultiplier)
    {
        ApplyCrowdControl(EnemyCrowdControlRequest.CreateSlow(duration, moveSpeedMultiplier));
    }

    public void ApplyKnockback(Vector2 knockback)
    {
        ApplyCrowdControl(EnemyCrowdControlRequest.CreateKnockback(knockback));
    }

    public void ApplyPoison(float duration, float damagePerSecond)
    {
        if (statusAilmentController == null)
            statusAilmentController = GetComponent<StatusAilmentController>() ?? gameObject.AddComponent<StatusAilmentController>();

        statusAilmentController.ApplyPoison(duration, damagePerSecond);
    }

    public void ApplyBurn(float duration, float tickInterval, float damagePerTick)
    {
        if (statusAilmentController == null)
            statusAilmentController = GetComponent<StatusAilmentController>() ?? gameObject.AddComponent<StatusAilmentController>();

        statusAilmentController.ApplyBurn(duration, tickInterval, damagePerTick);
    }

    private void EnemyDead()
    {
        if (hasFinalizedDeath)
            return;

        hasFinalizedDeath = true;

        RunSaveIdentity identity = GetComponent<RunSaveIdentity>();
        if (identity != null)
            RunSaveRuntimeState.MarkEnemyDead(identity.SaveId);

        statusAilmentController?.ClearAllAilments();
        OnDamaged = null;
        OnDeath = null;
        Debug.Log("isDead");
        Destroy(gameObject);
    }

    private void BeginDeathSequence()
    {
        if (isHandlingDeath)
            return;

        isHandlingDeath = true;
        statusAilmentController?.ClearAllAilments();
        if (directionalAnimator == null)
            directionalAnimator = GetComponent<EnemyDirectionalAnimator>();

        directionalAnimator?.LockDeathDirection();
        DisableDeathInteractions();
        PlayDeathAnimation();

        if (ShouldDelayDeathFinalization())
            StartCoroutine(CompleteDeathSequenceAfterAnimation());
        else
            OnDeath?.Invoke();
    }

    private void PlayDeathAnimation()
    {
        if (deathAnimator == null || string.IsNullOrWhiteSpace(deathAnimationTrigger))
            return;

        deathAnimator.ResetTrigger(deathAnimationTrigger);
        deathAnimator.SetTrigger(deathAnimationTrigger);
    }

    private IEnumerator CompleteDeathSequenceAfterAnimation()
    {
        yield return WaitForDeathAnimation();
        OnDeath?.Invoke();
    }

    private bool ShouldDelayDeathFinalization()
    {
        return deathDestroyDelay > 0f || (deathAnimator != null && !string.IsNullOrWhiteSpace(deathAnimationTrigger));
    }

    private IEnumerator WaitForDeathAnimation()
    {
        if (deathAnimator == null || string.IsNullOrWhiteSpace(deathAnimationTrigger))
        {
            if (deathDestroyDelay > 0f)
                yield return new WaitForSeconds(deathDestroyDelay);

            yield break;
        }

        yield return null;

        if (!string.IsNullOrWhiteSpace(deathAnimationStateName))
        {
            float enterTimeout = 0.25f;
            while (enterTimeout > 0f && !deathAnimator.GetCurrentAnimatorStateInfo(0).IsName(deathAnimationStateName))
            {
                enterTimeout -= Time.deltaTime;
                yield return null;
            }

            AnimatorStateInfo stateInfo = deathAnimator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName(deathAnimationStateName))
            {
                float fallbackTimeout = deathDestroyDelay > 0f ? deathDestroyDelay + 0.25f : stateInfo.length + 0.25f;
                while (fallbackTimeout > 0f)
                {
                    stateInfo = deathAnimator.GetCurrentAnimatorStateInfo(0);
                    if (!deathAnimator.IsInTransition(0) && stateInfo.IsName(deathAnimationStateName) && stateInfo.normalizedTime >= 1f)
                        yield break;

                    fallbackTimeout -= Time.deltaTime;
                    yield return null;
                }

                yield break;
            }
        }

        if (deathDestroyDelay > 0f)
            yield return new WaitForSeconds(deathDestroyDelay);
    }

    private void DisableDeathInteractions()
    {
        if (attack is Behaviour attackBehaviour)
            attackBehaviour.enabled = false;

        if (movement is Behaviour movementBehaviour)
            movementBehaviour.enabled = false;

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        if (canvas != null)
            canvas.enabled = false;
    }

    private void ShowDamage(float damage)
    {
        if (damageText == null)
        {
            return;
        }

        Vector3 pos = damagePos.position;
        Vector3 randomPos = new Vector3(UnityEngine.Random.Range(pos.x - 0.5f, pos.x + 0.5f), pos.y, 0);

        GameObject text = GameObject.Instantiate(damageText, randomPos, Quaternion.identity, canvas.transform);

        text.GetComponent<DamageText>().Setup(damage);

    }

    void OnDestroy()
    {
        OnDamaged = null;
        OnDeath = null;
    }
}
