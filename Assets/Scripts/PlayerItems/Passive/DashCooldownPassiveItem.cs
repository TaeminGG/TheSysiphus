using UnityEngine;

[CreateAssetMenu(fileName = "DashCooldownItem", menuName = "Scriptable Objects/PassiveItem/DashCooldownReduction")]
public class DashCooldownPassiveItem : PassiveItem
{
    [Range(0f, 0.95f)] public float dashCooldownReductionPercent = 0.2f;

    public override void OnEquip(PlayerStats stats)
    {
        PlayerMove playerMove = stats.GetComponent<PlayerMove>();
        if (playerMove != null)
            playerMove.AddDashCooldownReduction(dashCooldownReductionPercent);
    }

    public override void OnUnequip(PlayerStats stats)
    {
        PlayerMove playerMove = stats.GetComponent<PlayerMove>();
        if (playerMove != null)
            playerMove.RemoveDashCooldownReduction(dashCooldownReductionPercent);
    }
}
