using UnityEngine;

[CreateAssetMenu(fileName = "PotionHealBoostItem", menuName = "Scriptable Objects/PassiveItem/PotionHealBoost")]
public class PotionHealBoostPassiveItem : PassiveItem
{
    [Range(0f, 5f)] public float healMultiplierIncrease = 0.35f;

    public override void OnEquip(PlayerStats stats)
    {
        stats.AddPotionHealMultiplier(healMultiplierIncrease);
    }

    public override void OnUnequip(PlayerStats stats)
    {
        stats.RemovePotionHealMultiplier(healMultiplierIncrease);
    }
}
