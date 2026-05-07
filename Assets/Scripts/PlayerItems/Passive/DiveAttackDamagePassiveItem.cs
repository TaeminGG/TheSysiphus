using UnityEngine;

[CreateAssetMenu(fileName = "DiveAttackDamageItem", menuName = "Scriptable Objects/PassiveItem/DiveAttack/Damage")]
public class DiveAttackDamagePassiveItem : PassiveItem
{
    [Range(0f, 5f)] public float damageIncreasePercent = 0.25f;

    public override void OnEquip(PlayerStats stats)
    {
        if (stats != null && stats.TryGetComponent(out PlayerDiveAttack diveAttack))
            diveAttack.AddDamageMultiplier(damageIncreasePercent);
    }

    public override void OnUnequip(PlayerStats stats)
    {
        if (stats != null && stats.TryGetComponent(out PlayerDiveAttack diveAttack))
            diveAttack.RemoveDamageMultiplier(damageIncreasePercent);
    }
}
