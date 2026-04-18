using UnityEngine;

[CreateAssetMenu(fileName = "DashNovaItem", menuName = "Scriptable Objects/PassiveItem/DashNova")]
public class DashNovaPassiveItem : PassiveItem
{
    public GameObject effectPrefab;
    public AttackData attackData;
    public Vector3 rangeIndicatorLocalOffset = Vector3.zero;
    public Vector3 rangeIndicatorLocalScale = Vector3.one;

    public override void OnEquip(PlayerStats stats)
    {
        DashNovaPassiveEffect effect = PassiveEffectComponentUtility.GetOrAddComponent<DashNovaPassiveEffect>(stats.gameObject);
        effect.Initialize(stats, this);
    }

    public override void OnUnequip(PlayerStats stats)
    {
        if (stats.TryGetComponent(out DashNovaPassiveEffect effect))
            effect.Uninitialize(this);
    }
}

public sealed class DashNovaPassiveEffect : MonoBehaviour
{
    private PlayerStats stats;
    private PlayerMove playerMove;
    private DashNovaPassiveItem source;

    public void Initialize(PlayerStats playerStats, DashNovaPassiveItem item)
    {
        if (source == item)
            return;

        Cleanup();

        stats = playerStats;
        playerMove = playerStats != null ? playerStats.GetComponent<PlayerMove>() : null;
        source = item;

        if (playerMove != null)
            playerMove.OnDashEnded += HandleDashEnded;
    }

    public void Uninitialize(DashNovaPassiveItem item)
    {
        if (source != item)
            return;

        Cleanup();
        Destroy(this);
    }

    private void HandleDashEnded()
    {
        if (stats == null || playerMove == null || source == null || source.attackData == null || source.effectPrefab == null)
            return;

        AttackData resolvedAttackData = source.attackData;
        Vector3 spawnPosition = new Vector3(transform.position.x, transform.position.y, 0f);
        GameObject effectObject = Instantiate(source.effectPrefab, spawnPosition, Quaternion.identity);
        effectObject.name = "DashNovaEffect";

        if (effectObject.TryGetComponent(out DashNovaEffectInstance effectInstance))
        {
            effectInstance.Initialize(
                resolvedAttackData,
                stats,
                stats.damage,
                transform.position,
                playerMove.floorZ,
                source.rangeIndicatorLocalOffset,
                source.rangeIndicatorLocalScale);
        }
    }

    private void Cleanup()
    {
        if (playerMove != null)
            playerMove.OnDashEnded -= HandleDashEnded;

        stats = null;
        playerMove = null;
        source = null;
    }

    private void OnDestroy()
    {
        Cleanup();
    }
}
