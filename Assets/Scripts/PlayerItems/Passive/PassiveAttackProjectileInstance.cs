using UnityEngine;

public sealed class PassiveAttackProjectileInstance : MonoBehaviour
{
    [SerializeField] private AttackHitBox hitBox;
    [SerializeField] private Projectile projectile;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private LayerMask hitLayer;

    public void Initialize(
        AttackData attackData,
        PlayerStats ownerStats,
        float playerDamage,
        Vector3 spawnPosition,
        float angle,
        int floorZ,
        float moveSpeed,
        float lifeTime,
        Sprite projectileSprite,
        Color projectileColor)
    {
        if (attackData == null)
            return;

        if (hitBox == null)
            hitBox = GetComponent<AttackHitBox>();

        if (projectile == null)
            projectile = GetComponent<Projectile>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        transform.position = spawnPosition;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (spriteRenderer != null)
        {
            if (projectileSprite != null)
                spriteRenderer.sprite = projectileSprite;

            spriteRenderer.color = projectileColor;
        }

        if (hitBox != null)
        {
            hitBox.hitLayer = hitLayer.value != 0 ? hitLayer : LayerMask.GetMask("Enemy");
            hitBox.SetPlayerOwner(ownerStats);
            hitBox.EnemySetup(attackData, playerDamage, lifeTime, spawnPosition, angle, floorZ);
        }

        if (projectile != null)
        {
            projectile.moveSpeed = moveSpeed;
            projectile.lifeTime = lifeTime;
            projectile.EnableGroundBlocking(false);
            projectile.SetFloorZ(floorZ);
            projectile.SetProjectileStartZ(floorZ);
        }
    }
}
