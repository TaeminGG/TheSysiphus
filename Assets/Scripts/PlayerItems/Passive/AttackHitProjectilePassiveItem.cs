using UnityEngine;

[CreateAssetMenu(fileName = "AttackHitProjectileItem", menuName = "Scriptable Objects/PassiveItem/AttackHitProjectile")]
public class AttackHitProjectilePassiveItem : PassiveItem
{
    public GameObject projectilePrefab;
    public float baseDamage = 10f;
    public float damageScaling = 0.45f;
    public float knockbackForce = 0.75f;
    public Vector2 hitboxSize = new Vector2(0.45f, 0.75f);
    public float projectileSpeed = 7f;
    public float projectileLifetime = 1.5f;
    public float zUpperRange = 2f;
    public float zLowerRange = 0f;
    public Sprite projectileSprite;
    public Color projectileColor = new Color(0.6f, 1f, 0.95f, 0.9f);

    public override void OnEquip(PlayerStats stats)
    {
        AttackHitProjectilePassiveEffect effect = PassiveEffectComponentUtility.GetOrAddComponent<AttackHitProjectilePassiveEffect>(stats.gameObject);
        effect.Initialize(stats, this);
    }

    public override void OnUnequip(PlayerStats stats)
    {
        if (stats.TryGetComponent(out AttackHitProjectilePassiveEffect effect))
            effect.Uninitialize(this);
    }
}

public sealed class AttackHitProjectilePassiveEffect : MonoBehaviour
{
    private PlayerStats stats;
    private PlayerMove playerMove;
    private PlayerAttackDirection attackDirection;
    private AttackController attackController;
    private AttackHitProjectilePassiveItem source;
    private AttackData attackData;

    public void Initialize(PlayerStats playerStats, AttackHitProjectilePassiveItem item)
    {
        if (source == item)
            return;

        Cleanup();

        stats = playerStats;
        playerMove = playerStats != null ? playerStats.GetComponent<PlayerMove>() : null;
        attackDirection = playerStats != null ? playerStats.GetComponent<PlayerAttackDirection>() : null;
        attackController = playerStats != null ? playerStats.GetComponent<AttackController>() : null;
        source = item;
        attackData = PassiveEffectComponentUtility.CreateRuntimeAttackData(
            $"{item.ItemId}_projectile",
            item.baseDamage,
            item.damageScaling,
            item.hitboxSize,
            item.projectileLifetime,
            item.knockbackForce,
            item.zUpperRange,
            item.zLowerRange);

        if (attackController != null)
            attackController.OnAttackHit += HandleAttackHit;
    }

    public void Uninitialize(AttackHitProjectilePassiveItem item)
    {
        if (source != item)
            return;

        Cleanup();
        Destroy(this);
    }

    private void HandleAttackHit()
    {
        if (stats == null || playerMove == null || source == null || attackData == null || source.projectilePrefab == null)
            return;

        Vector2 direction = attackDirection != null ? attackDirection.GetAttackDirection() : playerMove.GetFacingDirection();
        if (direction.sqrMagnitude <= 0.001f)
            direction = playerMove.GetFacingDirection();

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        Vector3 spawnPosition = new Vector3(transform.position.x, transform.position.y, playerMove.floorZ);
        GameObject projectileObject = Instantiate(source.projectilePrefab, spawnPosition, Quaternion.Euler(0f, 0f, angle));
        projectileObject.name = "PassiveAttackProjectile";

        if (projectileObject.TryGetComponent(out PassiveAttackProjectileInstance projectileInstance))
        {
            projectileInstance.Initialize(
                attackData,
                stats,
                stats.damage,
                spawnPosition,
                angle,
                playerMove.floorZ,
                source.projectileSpeed,
                source.projectileLifetime,
                source.projectileSprite,
                source.projectileColor);
        }
    }

    private void Cleanup()
    {
        if (attackController != null)
            attackController.OnAttackHit -= HandleAttackHit;

        if (attackData != null)
            Destroy(attackData);

        stats = null;
        playerMove = null;
        attackDirection = null;
        attackController = null;
        source = null;
        attackData = null;
    }

    private void OnDestroy()
    {
        Cleanup();
    }
}
