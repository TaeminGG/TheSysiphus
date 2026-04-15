using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStats : MonoBehaviour, IDamageable
{
    private float maxHp = 100f;
    private float currentHp;

    public float damage = 0f;
    public float attackSpeed = 1f;

    public float invDuration = 0.5f;
    private float invTimer = 0f;
    private bool isInv => invTimer > 0f;

    private int gold = 0;

    private int potionCount = 5;
    private int maxPotion = 5;
    public float healAmount = 30f;
    public float healCooltime = 1f;
    private float healTimer = 0f;

    public bool IsDead => currentHp <= 0f;

    [HideInInspector] public event Action<float> OnDamaged;
    public event Action OnDeath;
    public event Action OnKill;
    public event Action OnDash;
    public event Action OnHealEvent;
    public event Action<int> OnGoldChanged;

    public Canvas canvas;
    public GameObject damageText;
    public Transform damagePos;

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
        OnGoldChanged?.Invoke(gold);
    }

    private void Update()
    {
        if (invTimer > 0f)
            invTimer -= Time.deltaTime;

        healTimer += Time.deltaTime;
    }

    public void TakeDamage(float amount, Vector2 knockback)
    {
        if (IsDead || isInv)
            return;

        invTimer = invDuration;
        currentHp = Mathf.Max(0f, currentHp - amount);
        OnDamaged?.Invoke(amount);

        if (crowdControl != null)
            crowdControl.ApplyKnockback(knockback);

        if (IsDead)
            OnDeath?.Invoke();
    }

    public void HitByRock()
    {
        Debug.Log("Die");
    }

    private void ShowDamage(float amount)
    {
        if (damageText == null || damagePos == null || canvas == null)
            return;

        Vector3 pos = damagePos.position;
        Vector3 randomPos = new Vector3(UnityEngine.Random.Range(pos.x - 0.5f, pos.x + 0.5f), pos.y, 0f);

        GameObject text = Instantiate(damageText, randomPos, Quaternion.identity, canvas.transform);
        text.GetComponent<DamageText>().Setup(amount);
    }

    private void ShowHealEffect()
    {
        if (healEffect == null || damagePos == null || canvas == null)
            return;

        Vector3 pos = damagePos.position;
        Vector3 randomPos = new Vector3(UnityEngine.Random.Range(pos.x - 0.5f, pos.x + 0.5f), pos.y, 0f);

        Instantiate(healEffect, randomPos, Quaternion.identity, canvas.transform);
    }

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
        currentHp = Mathf.Min(maxHp, currentHp + amount);
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

    public void AddGold(int amount)
    {
        gold += Mathf.Max(0, amount);
        OnGoldChanged?.Invoke(gold);
    }

    public bool CanAfford(int amount)
    {
        return gold >= Mathf.Max(0, amount);
    }

    public bool TrySpendGold(int amount)
    {
        int clampedAmount = Mathf.Max(0, amount);
        if (gold < clampedAmount)
            return false;

        gold -= clampedAmount;
        OnGoldChanged?.Invoke(gold);
        return true;
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
        potionCount = Mathf.Min(maxPotion, potionCount + amount);
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

    public void ApplyPetrify(float duration)
    {
        ApplyCrowdControl(PlayerCrowdControlRequest.CreatePetrify(duration));
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
        if (idx != 0)
            return;

        damage += amount;
        StartCoroutine(DamageBuff(amount, duration));
    }

    private IEnumerator DamageBuff(float amount, float duration)
    {
        yield return new WaitForSeconds(duration);
        damage -= amount;
    }

    private void OnHeal(InputValue value)
    {
        if (crowdControl != null && !crowdControl.CanHeal)
            return;

        if (currentHp >= maxHp)
            return;

        if (potionCount <= 0 || healTimer <= healCooltime)
            return;

        healTimer = 0f;
        potionCount--;
        Heal(healAmount);
    }
}
