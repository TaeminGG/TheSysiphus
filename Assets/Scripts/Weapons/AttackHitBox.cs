using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UIElements;

public class AttackHitBox : MonoBehaviour
{
    public LayerMask hitLayer;
    [SerializeField] private HitSparkController hitSparkController;
    private AttackData currentData;
    private Collider2D col;
    private List<Collider2D> hitTargets = new List<Collider2D>();

    private SpriteRenderer spriteRenderer;
    private Animator anim;

    public Transform visual;

    private float damage;
    private float floorZ = 0;
    private PlayerStats ownerStats;
    private bool applyPoisonToPlayerOnHit = false;
    private float poisonDurationOnPlayerHit = 0f;
    private float poisonDamagePerSecondOnPlayerHit = 0f;
    private bool applyBurnToPlayerOnHit = false;
    private float burnDurationOnPlayerHit = 0f;
    private float burnTickIntervalOnPlayerHit = 0f;
    private float burnDamagePerTickOnPlayerHit = 0f;
    private bool ignorePlayerZCheck = false;

    public event System.Action OnEnemyHitConfirmed;


    private void Awake()
    {
        EnsureReferences();
    }


    // 기본 공격속도 1.0f
    public void Setup(AttackData data, float attackSpeed, float playerAttackDamage, Vector2 direction, float z)
    {
        EnsureReferences();
        currentData = data;
        hitTargets.Clear();
        damage = playerAttackDamage;
        floorZ = z;
        ResetOnHitAilments();

        SpriteRenderer dataSprite = data.visual.GetComponent<SpriteRenderer>();
        Animator dataAnim = data.visual.GetComponent<Animator>();

        spriteRenderer.sprite = dataSprite.sprite;
        spriteRenderer.flipX = data.flipX;
        spriteRenderer.flipY = data.flipY;
        anim.ResetControllerState();
        anim.runtimeAnimatorController = dataAnim.runtimeAnimatorController;
        visual.localPosition = new Vector3(data.hitboxOffset.x, data.hitboxOffset.y);

        if(col is BoxCollider2D box)
        {
            box.size = data.hitboxSize;
            box.offset = data.hitboxOffset;
        }
        else if (col is CircleCollider2D circle)
        {
            circle.radius = Mathf.Max(data.hitboxSize.x, data.hitboxSize.y) * 0.5f;
            circle.offset = data.hitboxOffset;
        }

        // 입력받은 마우스 방향에 맞춰 회전
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        angle -= 90f;
        transform.rotation=Quaternion.Euler(0f,0f,angle);

        visual.rotation = Quaternion.Euler(0f,0f,angle - 90f);

        EnableHitBox();

        // 공격 속도에 비례해서 히트박스 지속시간 조절
        float asDuration = (data.attackDuration * 0.5f) / attackSpeed;
        StartCoroutine(AutoDisable(asDuration));
    }

    public void EnemySetup(AttackData data, float attackDamage, float duration, Vector3 pos, float angle, float z)
    {
        EnsureReferences();
        currentData = data;
        hitTargets.Clear();
        damage = attackDamage;
        floorZ = z;
        ResetOnHitAilments();

        if (col is BoxCollider2D box)
        {
            box.size = data.hitboxSize;
            box.offset = data.hitboxOffset;
        }
        else if (col is CircleCollider2D circle)
        {
            circle.radius = Mathf.Max(data.hitboxSize.x, data.hitboxSize.y) * 0.5f;
            circle.offset = data.hitboxOffset;
        }

        Vector3 targetPos = pos;
        targetPos.z = 0;
        transform.position = targetPos;
        transform.rotation = Quaternion.Euler(0f,0f,angle);

        EnableHitBox();
        StartCoroutine(AutoDisable(duration));
    }

    public void SetFloorZ(float z)
    {
        floorZ = z;
    }

    public void SetPlayerOwner(PlayerStats stats)
    {
        ownerStats = stats;
    }

    public void ConfigurePoisonOnPlayerHit(float duration, float damagePerSecond)
    {
        if (duration <= 0f || damagePerSecond <= 0f)
        {
            ResetPoisonOnHit();
            return;
        }

        applyPoisonToPlayerOnHit = true;
        poisonDurationOnPlayerHit = duration;
        poisonDamagePerSecondOnPlayerHit = damagePerSecond;
    }

    public void ConfigureBurnOnPlayerHit(float duration, float tickInterval, float damagePerTick)
    {
        if (duration <= 0f || tickInterval <= 0f || damagePerTick <= 0f)
        {
            ResetBurnOnHit();
            return;
        }

        applyBurnToPlayerOnHit = true;
        burnDurationOnPlayerHit = duration;
        burnTickIntervalOnPlayerHit = tickInterval;
        burnDamagePerTickOnPlayerHit = damagePerTick;
    }

    public void ConfigurePlayerZCheck(bool ignoreZCheck)
    {
        ignorePlayerZCheck = ignoreZCheck;
    }

    private IEnumerator AutoDisable(float delay)
    {
        yield return new WaitForSeconds(delay);
        DisableHitBox();
    }

    public void EnableHitBox()
    {
        EnsureReferences();
        if (col != null)
            col.enabled = true;
    }

    public void DisableHitBox()
    {
        EnsureReferences();
        if (col != null)
            col.enabled = false;
    }

    private void EnsureReferences()
    {
        if (col == null)
            col = GetComponent<Collider2D>();

        if (visual == null)
            return;

        if (spriteRenderer == null)
            spriteRenderer = visual.GetComponent<SpriteRenderer>();

        if (anim == null)
            anim = visual.GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleTriggerContact(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        HandleTriggerContact(other);
    }

    private void HandleTriggerContact(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayer) == 0) return;
        if (hitTargets.Contains(other)) return;

        hitTargets.Add(other);

        if ((hitLayer & (1 << LayerMask.NameToLayer("Player"))) != 0
            && ignorePlayerZCheck
            && TryHandleIgnoredPlayerZHit(other))
        {
            return;
        }

        // 피격 주체 체크
        if((hitLayer & (1 << LayerMask.NameToLayer("Player"))) != 0)
        {
            if(other.TryGetComponent<IDamageable>(out var target))
            {
                // Z 공격 범위에 플레이어가 있어야 공격판정 적용
                float zUpper = currentData.zUpperRange;
                float zLower = currentData.zLowerRange;

                other.TryGetComponent<PlayerMove>(out var player);

                float playerZvalue = player.playerZ;

                if (floorZ + zUpper >= playerZvalue)
                {
                    if (floorZ - zLower <= playerZvalue)
                    {
                        // 계수 적용된 최종 데미지 계산
                        float finalDamage = currentData.GetFinalDamage(damage);

                        Vector2 knockback = (other.transform.position - transform.position).normalized
                                            * currentData.knockbackForce;
                        bool wasDead = target.IsDead;
                        target.TakeDamage(finalDamage, knockback);
                        if (other.TryGetComponent(out PlayerStats hitPlayerStats))
                            ApplyConfiguredAilmentsToPlayer(hitPlayerStats);
                        if (!wasDead && target.IsDead)
                            ownerStats?.NotifyKill();

                        OnEnemyHitConfirmed?.Invoke();
                        PlayHitSpark(other, knockback);
                        PlayHitCameraFeedback();
                        PlayHitStop();
                    }
                }
            }
        }
        else
        {
            if (other.TryGetComponent<IDamageable>(out var target))
            {
                // Z 공격 범위에 해당하는 적만 공격
                float zUpper = currentData.zUpperRange;
                float zLower = currentData.zLowerRange;

                other.TryGetComponent<IEnemyMovement>(out var targetEnemy);
                if (targetEnemy == null)
                    return;

                float enemyFloorZ = targetEnemy.FloorZ;

                if (floorZ + zUpper >= enemyFloorZ)
                {
                    if (floorZ - zLower <= enemyFloorZ)
                    {
                        // 계수 적용된 최종 데미지 계산
                        float finalDamage = currentData.GetFinalDamage(damage);

                        Vector2 knockback = (other.transform.position - transform.position).normalized
                                            * currentData.knockbackForce;
                        bool wasDead = target.IsDead;
                        target.TakeDamage(finalDamage, knockback);
                        if (!wasDead && target.IsDead)
                            ownerStats?.NotifyKill();
                        OnEnemyHitConfirmed?.Invoke();
                        PlayHitSpark(other, knockback);
                        PlayHitCameraFeedback();
                        PlayHitStop();
                    }
                }
            }
        }

        if ((hitLayer & (1 << LayerMask.NameToLayer("Probs"))) != 0)
        {
            if (other.TryGetComponent<BreakableProbs>(out var target))
            {
                // Z 공격 범위에 플레이어가 있어야 공격판정 적용
                float zUpper = currentData.zUpperRange;
                float zLower = currentData.zLowerRange;

                float zValue = target.floorZ;

                if (floorZ + zUpper >= zValue)
                {
                    if (floorZ - zLower <= zValue)
                    {
                        target.Attacked();
                    }
                }
            }
        }
    }

    private bool TryHandleIgnoredPlayerZHit(Collider2D other)
    {
        if (!other.TryGetComponent<IDamageable>(out var target))
            return false;

        float finalDamage = currentData.GetFinalDamage(damage);
        Vector2 knockback = (other.transform.position - transform.position).normalized * currentData.knockbackForce;
        bool wasDead = target.IsDead;
        target.TakeDamage(finalDamage, knockback);
        if (other.TryGetComponent(out PlayerStats hitPlayerStats))
            ApplyConfiguredAilmentsToPlayer(hitPlayerStats);
        if (!wasDead && target.IsDead)
            ownerStats?.NotifyKill();

        OnEnemyHitConfirmed?.Invoke();
        PlayHitSpark(other, knockback);
        PlayHitCameraFeedback();
        PlayHitStop();
        return true;
    }

    private void PlayHitCameraFeedback()
    {
        if (currentData == null || currentData.onHitCameraPreset == null)
        {
            return;
        }

        CameraFeedbackService.Instance?.Play(currentData.onHitCameraPreset);
    }

    private void PlayHitStop()
    {
        if (currentData == null || currentData.onHitStopDuration <= 0f)
        {
            return;
        }

        HitStopController.Instance?.Play(
            currentData.onHitStopDuration,
            currentData.onHitStopTimeScale,
            currentData.onHitStopPriority);
    }

    private void PlayHitSpark(Collider2D other, Vector2 knockback)
    {
        if (currentData == null || currentData.onHitSparkPrefab == null)
        {
            return;
        }

        if (hitSparkController == null)
        {
            hitSparkController = FindFirstObjectByType<HitSparkController>();
        }

        if (hitSparkController == null)
        {
            return;
        }

        HitSparkSettings settings = currentData.onHitSparkSettings;

        if (settings.direction.sqrMagnitude <= 0.001f)
        {
            settings.direction = knockback.normalized;
        }

        hitSparkController.Play(currentData.onHitSparkPrefab, other.bounds.center, settings);
    }

    // 흰색 스프라이트 생성
    private Sprite CreateWhiteSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private void OnDestroy()
    {
        OnEnemyHitConfirmed = null;
    }

    private void ResetOnHitAilments()
    {
        ResetPoisonOnHit();
        ResetBurnOnHit();
        ignorePlayerZCheck = false;
    }

    private void ResetPoisonOnHit()
    {
        applyPoisonToPlayerOnHit = false;
        poisonDurationOnPlayerHit = 0f;
        poisonDamagePerSecondOnPlayerHit = 0f;
    }

    private void ResetBurnOnHit()
    {
        applyBurnToPlayerOnHit = false;
        burnDurationOnPlayerHit = 0f;
        burnTickIntervalOnPlayerHit = 0f;
        burnDamagePerTickOnPlayerHit = 0f;
    }

    private void ApplyConfiguredAilmentsToPlayer(PlayerStats hitPlayerStats)
    {
        if (hitPlayerStats == null)
            return;

        if (applyPoisonToPlayerOnHit)
            hitPlayerStats.ApplyPoison(poisonDurationOnPlayerHit, poisonDamagePerSecondOnPlayerHit);

        if (applyBurnToPlayerOnHit)
            hitPlayerStats.ApplyBurn(burnDurationOnPlayerHit, burnTickIntervalOnPlayerHit, burnDamagePerTickOnPlayerHit);
    }

}
