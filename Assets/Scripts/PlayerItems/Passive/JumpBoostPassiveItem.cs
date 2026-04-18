using UnityEngine;

[CreateAssetMenu(fileName = "JumpBoostItem", menuName = "Scriptable Objects/PassiveItem/JumpBoost")]
public class JumpBoostPassiveItem : PassiveItem
{
    [Range(0f, 5f)] public float jumpForceIncreasePercent = 0.25f;

    public override void OnEquip(PlayerStats stats)
    {
        PlayerMove playerMove = stats.GetComponent<PlayerMove>();
        if (playerMove != null)
            playerMove.AddJumpForceMultiplier(jumpForceIncreasePercent);
    }

    public override void OnUnequip(PlayerStats stats)
    {
        PlayerMove playerMove = stats.GetComponent<PlayerMove>();
        if (playerMove != null)
            playerMove.RemoveJumpForceMultiplier(jumpForceIncreasePercent);
    }
}
