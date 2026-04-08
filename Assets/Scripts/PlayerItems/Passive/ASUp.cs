using UnityEngine;

[CreateAssetMenu(fileName = "ASItem", menuName = "Scriptable Objects/PassiveItem/ASUp")]
public class ASUp : PassiveItem
{
    public float bonus = 5f;

    public override void OnEquip(PlayerStats stats)
    {
        stats.attackSpeed += bonus;
    }
    public override void OnUnequip(PlayerStats stats)
    {
        stats.attackSpeed -= bonus;
    }
}
