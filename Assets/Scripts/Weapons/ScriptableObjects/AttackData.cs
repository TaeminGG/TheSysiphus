using UnityEngine;

[CreateAssetMenu(fileName = "AttackStats", menuName = "Scriptable Objects/AttackData")]
public class AttackData : ScriptableObject
{
    public string animTrigger;          // 해당 공격에서 재생 할 애니메이션
    public float damage;                // 공격력
    public float damageScaling;         // 공격력 계수
    public Vector2 hitboxSize;          // 공격 범위 (사각형)
    public Vector2 hitboxOffset;        // 공격 오프셋
    public float attackDuration;        // 공격 지속시간 
    public float comboInputWindow;      // 다음 공격(콤보) 까지 입력 제한 시간
    public float knockbackForce;        // 넉백량
    public float zUpperRange;           // Z(높이)값 위쪽 공격 범위
    public float zLowerRange;           // Z(높이)값 아래 공격 범위

    public float GetFinalDamage(float playerDamage)
    {
        return damage + Mathf.RoundToInt(playerDamage * damageScaling);
    }
}
