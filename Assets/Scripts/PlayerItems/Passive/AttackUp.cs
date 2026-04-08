using UnityEngine;

[CreateAssetMenu(fileName = "AttackItem", menuName = "Scriptable Objects/PassiveItem/AttackUp")]
public class AttackUp : PassiveItem
{
    public float bonus = 5f;

    public override void OnEquip(PlayerStats stats)
    {
        stats.damage += bonus;
    }
    public override void OnUnequip(PlayerStats stats)
    {
        stats.damage -= bonus;
    }
}
