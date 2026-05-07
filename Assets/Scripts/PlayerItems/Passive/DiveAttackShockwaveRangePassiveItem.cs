using UnityEngine;

[CreateAssetMenu(fileName = "DiveAttackShockwaveRangeItem", menuName = "Scriptable Objects/PassiveItem/DiveAttack/Shockwave Range")]
public class DiveAttackShockwaveRangePassiveItem : PassiveItem
{
    [Range(0f, 5f)] public float rangeIncreasePercent = 0.25f;

    public override void OnEquip(PlayerStats stats)
    {
        if (stats != null && stats.TryGetComponent(out PlayerDiveAttack diveAttack))
            diveAttack.AddLandingShockwaveRangeMultiplier(rangeIncreasePercent);
    }

    public override void OnUnequip(PlayerStats stats)
    {
        if (stats != null && stats.TryGetComponent(out PlayerDiveAttack diveAttack))
            diveAttack.RemoveLandingShockwaveRangeMultiplier(rangeIncreasePercent);
    }
}
