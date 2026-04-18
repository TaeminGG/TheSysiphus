using UnityEngine;

[CreateAssetMenu(fileName = "KillHealItem", menuName = "Scriptable Objects/PassiveItem/KillHeal")]
public class KillHealPassiveItem : PassiveItem
{
    public float healAmount = 8f;

    public override void OnEquip(PlayerStats stats)
    {
        KillHealPassiveEffect effect = PassiveEffectComponentUtility.GetOrAddComponent<KillHealPassiveEffect>(stats.gameObject);
        effect.Initialize(stats, this);
    }

    public override void OnUnequip(PlayerStats stats)
    {
        if (stats.TryGetComponent(out KillHealPassiveEffect effect))
            effect.Uninitialize(this);
    }
}

public sealed class KillHealPassiveEffect : MonoBehaviour
{
    private PlayerStats stats;
    private KillHealPassiveItem source;

    public void Initialize(PlayerStats playerStats, KillHealPassiveItem item)
    {
        if (source == item)
            return;

        Cleanup();
        stats = playerStats;
        source = item;
        stats.OnKill += HandleKill;
    }

    public void Uninitialize(KillHealPassiveItem item)
    {
        if (source != item)
            return;

        Cleanup();
        Destroy(this);
    }

    private void HandleKill()
    {
        if (stats != null && source != null)
            stats.Heal(source.healAmount);
    }

    private void Cleanup()
    {
        if (stats != null)
            stats.OnKill -= HandleKill;

        stats = null;
        source = null;
    }

    private void OnDestroy()
    {
        Cleanup();
    }
}
