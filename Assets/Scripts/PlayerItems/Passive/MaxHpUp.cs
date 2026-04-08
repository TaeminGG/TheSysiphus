using UnityEngine;

[CreateAssetMenu(fileName = "HpItem", menuName = "Scriptable Objects/PassiveItem/MaxHpUp")]
public class MaxHpUp : PassiveItem
{
    public float bonus = 5f;

    public override void OnEquip(PlayerStats stats)
    {
        stats.AddMaxHp(bonus);
        stats.Heal(bonus);
    }
    public override void OnUnequip(PlayerStats stats)
    {
        stats.ReduceMaxHp(bonus);
    }
}
