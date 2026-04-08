using UnityEngine;

[CreateAssetMenu(fileName = "SpeedItem", menuName = "Scriptable Objects/PassiveItem/SpeedUp")]
public class SpeedUp : PassiveItem
{
    public float bonus = 5f;

    public override void OnEquip(PlayerStats stats)
    {
        stats.AddMoveSpeed(bonus);
    }
    public override void OnUnequip(PlayerStats stats)
    {
        stats.ReduceMoveSpeed(bonus);
    }
}
