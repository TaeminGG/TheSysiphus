using System;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyStats : MonoBehaviour, IDamageable
{
    public float maxHp = 100f;
    private float currentHp = 0f;
    public float knockBackResist = 1f;      // 넉백 저항력 (넉백속도 및 넉백 거리에 영향, 1f 일 때 최대 낮을수록 저항함)

    public event Action<float> OnDamaged;
    public event Action OnDeath;

    public bool isKnockback = false;
    public Vector2 knockbackVector = Vector2.zero;

    private IEnemyAttack attack;
    private IEnemyMovement movement;

    public Canvas canvas;
    public EnemyHpBar hpbar;
    public GameObject damageText;
    public Transform damagePos;             // 피격 시 데미지가 표시될 위치

    public bool IsDead => currentHp <= 0;
    void Awake()
    {
        currentHp = maxHp;

        movement = GetComponent<IEnemyMovement>();
        attack = GetComponent<IEnemyAttack>();
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

        if(attack != null && attack.CanAttack)
        {
            attack.Attack();
        }

        hpbar.HpSetting(currentHp / maxHp);
    }

    // 피격
    public void TakeDamage(float damage, Vector2 knockback)
    {
        if (IsDead) return;

        // 체력 감소 및 피격 이벤트 실행
        currentHp -= damage;
        OnDamaged?.Invoke(damage);

        // 넉백 처리
        isKnockback = true;

        // 넉백 저항에 따라 넉백 Vector 조절
        knockbackVector = knockback * knockBackResist;

        if (IsDead)
            OnDeath?.Invoke();
    }

    private void EnemyDead()
    {
        OnDamaged = null;
        OnDeath = null;
        Destroy(gameObject);
        Debug.Log("isDead");
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
