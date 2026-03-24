using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable
{
    private float maxHp = 100;
    private float currentHp;

    public float damage = 0;
    public float attackSpeed = 1f;

    public float invDuration = 0.5f;        // 피격 시 무적 시간
    private float invTimer = 0f;
    private bool isInv => invTimer > 0f;    // 피격무적 여부

    public bool IsDead => currentHp <= 0;

    public event Action<float> OnDamaged;     // 피격 이벤트 (등록, 해제 잊지말기)
    public event Action OnDeath;            // 사망 이벤트

    private void Start()
    {
        currentHp = maxHp;
    }

    private void Update()
    {
        if(invTimer > 0f)
        {
            invTimer -= Time.deltaTime;
        }
    }

    public void TakeDamage(float damage, Vector2 knockback)
    {
        if (IsDead || isInv) return;

        invTimer = invDuration;         // 피격 무적 시작
        currentHp = Mathf.Max(0, currentHp - damage);
        OnDamaged.Invoke(damage);

        if (IsDead)
            OnDeath?.Invoke();
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

}
