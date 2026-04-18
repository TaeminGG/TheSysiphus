using UnityEngine;

[CreateAssetMenu(fileName = "KillDamageBuffItem", menuName = "Scriptable Objects/PassiveItem/KillDamageBuff")]
public class KillDamageBuffPassiveItem : PassiveItem
{
    public float bonusDamage = 8f;
    public float duration = 4f;

    public override void OnEquip(PlayerStats stats)
    {
        KillDamageBuffPassiveEffect effect = PassiveEffectComponentUtility.GetOrAddComponent<KillDamageBuffPassiveEffect>(stats.gameObject);
        effect.Initialize(stats, this);
    }

    public override void OnUnequip(PlayerStats stats)
    {
        if (stats.TryGetComponent(out KillDamageBuffPassiveEffect effect))
            effect.Uninitialize(this);
    }
}

public sealed class KillDamageBuffPassiveEffect : MonoBehaviour
{
    private PlayerStats stats;
    private KillDamageBuffPassiveItem source;

    public void Initialize(PlayerStats playerStats, KillDamageBuffPassiveItem item)
    {
        if (source == item)
            return;

        Cleanup();
        stats = playerStats;
        source = item;
        stats.OnKill += HandleKill;
    }

    public void Uninitialize(KillDamageBuffPassiveItem item)
    {
        if (source != item)
            return;

        Cleanup();
        Destroy(this);
    }

    private void HandleKill()
    {
        if (stats != null && source != null)
            stats.Buff(0, source.bonusDamage, source.duration);
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
