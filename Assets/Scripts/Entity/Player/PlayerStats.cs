using System;
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class PlayerStats : MonoBehaviour, IDamageable
{
    private float maxHp = 100;
    private float currentHp;

    public float damage = 0;
    public float attackSpeed = 1f;

    public float invDuration = 0.5f;        // 피격 시 무적 시간
    private float invTimer = 0f;
    private bool isInv => invTimer > 0f;    // 피격무적 시간 확인

    private int gold = 0;                   // 보유 중인 골드량

    private int potionCount = 5;
    private int maxPotion = 5;
    public float healAmount = 30f;
    public float healCooltime = 1.0f;
    private float healTimer = 0f;

    public bool IsDead => currentHp <= 0;

    [HideInInspector]
    public event Action<float> OnDamaged;   // 피격 이벤트 (등록, 해제 잊지말기)
    public event Action OnDeath;            // 사망 이벤트
    public event Action OnKill;             // 적 처치 시
    public event Action OnDash;             // 대쉬 시
    public event Action OnHealEvent;        // 회복 시


    public Canvas canvas;
    public GameObject damageText;
    public Transform damagePos;             // 피격 시 데미지가 표시될 위치

    public GameObject healEffect;
    private PlayerCrowdControl crowdControl;

    private void Start()
    {
        currentHp = maxHp;
        healTimer = healCooltime;
        crowdControl = GetComponent<PlayerCrowdControl>();
        if (crowdControl == null)
            crowdControl = gameObject.AddComponent<PlayerCrowdControl>();

        OnDamaged += ShowDamage;
        OnHealEvent += ShowHealEffect;
    }

    private void Update()
    {
        if(invTimer > 0f)
        {
            invTimer -= Time.deltaTime;
        }

        healTimer += Time.deltaTime;
    }

    public void TakeDamage(float damage, Vector2 knockback)
    {
        if (IsDead || isInv) return;

        invTimer = invDuration;         // 피격 무적 시작
        currentHp = Mathf.Max(0, currentHp - damage);
        OnDamaged?.Invoke(damage);
        if (crowdControl != null)
            crowdControl.ApplyKnockback(knockback);

        if (IsDead)
            OnDeath?.Invoke();
    }

    public void HitByRock()
    {
        Debug.Log("Die");
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
    private void ShowHealEffect()
    {
        if (healEffect == null)
        {
            return;
        }

        Vector3 pos = damagePos.position;
        Vector3 randomPos = new Vector3(UnityEngine.Random.Range(pos.x - 0.5f, pos.x + 0.5f), pos.y, 0);

        Instantiate(healEffect, randomPos, Quaternion.identity, canvas.transform);
    }

    // 공격력 증감 시 호출
    public void AddAttackDamage(float amount)
    {
        damage += amount;
    }
    public void ReduceAttackDamage(float amount)
    {
        damage -= amount;
    }

    public float GetCurrentHp()
    {
        return currentHp;
    }

    public float GetMaxHp()
    {
        return maxHp;
    }

    public void AddMaxHp(float amount)
    {
        maxHp += amount;
    }

    public void ReduceMaxHp(float amount)
    {
        maxHp -= amount;
    }

    public void Heal(float amount)
    {
        if (currentHp + amount >= maxHp)
            currentHp = maxHp;
        else
            currentHp += amount;
        OnHealEvent?.Invoke();
    }

    public void AddMoveSpeed(float speed)
    {
        PlayerMove playerMove = GetComponent<PlayerMove>();

        playerMove.AddSpeed(speed);
    }
    public void ReduceMoveSpeed(float speed)
    {
        PlayerMove playerMove = GetComponent<PlayerMove>();

        playerMove.ReduceSpeed(speed);
    }

    public int GetGold()
    {
        return gold;
    }

    public void AddGold(int g)
    {
        gold += g;
    }
    
    public int GetPotionCount()
    {
        return potionCount;
    }
    public int GetMaxPotionCount()
    {
        return maxPotion;
    }
    public void ChargePotion(int amount)
    {
        if (potionCount + amount >= maxPotion)
            potionCount = maxPotion;
        else
            potionCount += amount;
    }

    public void Dash()
    {
        OnDash?.Invoke();
    }

    public void ApplyCrowdControl(PlayerCrowdControlRequest request)
    {
        if (crowdControl == null)
            crowdControl = GetComponent<PlayerCrowdControl>();

        if (crowdControl == null)
            crowdControl = gameObject.AddComponent<PlayerCrowdControl>();

        crowdControl.ApplyEffect(request);
    }

    public void ApplyStun(float duration)
    {
        ApplyCrowdControl(PlayerCrowdControlRequest.CreateStun(duration));
    }

    public void ApplySlow(float duration, float moveSpeedMultiplier)
    {
        ApplyCrowdControl(PlayerCrowdControlRequest.CreateSlow(duration, moveSpeedMultiplier));
    }

    public void ApplyKnockback(Vector2 knockback)
    {
        ApplyCrowdControl(PlayerCrowdControlRequest.CreateKnockback(knockback));
    }

    public void Buff(int idx, float amount, float duration)
    {
        // 공격력 증가
        if(idx == 0)
        {
            damage += amount;
            StartCoroutine(DamageBuff(amount, duration));
        }
    }

    private IEnumerator DamageBuff(float amount, float dur)
    {
        yield return new WaitForSeconds(dur);
        damage -= amount;
    }

    private void OnHeal(InputValue value)
    {
        if (crowdControl != null && !crowdControl.CanHeal)
            return;
        // 체력이 최대면 예외처리
        if (currentHp >= maxHp)
            return;

        if(potionCount > 0 && healTimer > healCooltime)
        {
            healTimer = 0;
            potionCount--;
            Heal(healAmount);
        }
    }
}
