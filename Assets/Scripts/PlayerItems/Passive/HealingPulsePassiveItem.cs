using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HealingPulseItem", menuName = "Scriptable Objects/PassiveItem/HealingPulse")]
public class HealingPulsePassiveItem : PassiveItem
{
    public float radius = 2.5f;
    public float knockbackForce = 2.5f;
    public LayerMask enemyLayer;

    public override void OnEquip(PlayerStats stats)
    {
        HealingPulsePassiveEffect effect = PassiveEffectComponentUtility.GetOrAddComponent<HealingPulsePassiveEffect>(stats.gameObject);
        effect.Initialize(stats, this);
    }

    public override void OnUnequip(PlayerStats stats)
    {
        if (stats.TryGetComponent(out HealingPulsePassiveEffect effect))
            effect.Uninitialize(this);
    }
}

public sealed class HealingPulsePassiveEffect : MonoBehaviour
{
    private PlayerStats stats;
    private PlayerMove playerMove;
    private HealingPulsePassiveItem source;
    private readonly HashSet<EnemyStats> processedEnemies = new HashSet<EnemyStats>();

    public void Initialize(PlayerStats playerStats, HealingPulsePassiveItem item)
    {
        if (source == item)
            return;

        Cleanup();
        stats = playerStats;
        playerMove = playerStats != null ? playerStats.GetComponent<PlayerMove>() : null;
        source = item;
        stats.OnHealEvent += HandleHeal;
    }

    public void Uninitialize(HealingPulsePassiveItem item)
    {
        if (source != item)
            return;

        Cleanup();
        Destroy(this);
    }

    private void HandleHeal()
    {
        if (source == null)
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, source.radius, source.enemyLayer);
        processedEnemies.Clear();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            EnemyStats enemyStats = hit.GetComponentInParent<EnemyStats>();
            if (enemyStats == null || !processedEnemies.Add(enemyStats))
                continue;

            IEnemyMovement enemyMovement = enemyStats.GetComponent<IEnemyMovement>();
            if (enemyMovement != null && playerMove != null && enemyMovement.FloorZ != playerMove.floorZ)
                continue;

            Vector2 knockbackDirection = hit.bounds.center - transform.position;
            if (knockbackDirection.sqrMagnitude <= 0.001f && playerMove != null)
                knockbackDirection = playerMove.GetFacingDirection();

            enemyStats.ApplyKnockback(knockbackDirection.normalized * source.knockbackForce);
        }
    }

    private void Cleanup()
    {
        if (stats != null)
            stats.OnHealEvent -= HandleHeal;

        stats = null;
        playerMove = null;
        source = null;
        processedEnemies.Clear();
    }

    private void OnDestroy()
    {
        Cleanup();
    }
}
