using UnityEngine;

public sealed class DashNovaEffectInstance : MonoBehaviour
{
    [SerializeField] private AttackHitBox hitBox;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private LayerMask hitLayer;
    private float remainingLifetime = -1f;

    public void Initialize(
        AttackData attackData,
        PlayerStats ownerStats,
        float playerDamage,
        Vector3 worldPosition,
        int floorZ,
        Vector3 visualLocalOffset,
        Vector3 visualLocalScale)
    {
        if (attackData == null)
            return;

        if (hitBox == null)
            hitBox = GetComponent<AttackHitBox>();

        transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);

        if (hitBox != null)
        {
            hitBox.hitLayer = hitLayer.value != 0 ? hitLayer : LayerMask.GetMask("Enemy");
            hitBox.SetPlayerOwner(ownerStats);
            hitBox.EnemySetup(attackData, playerDamage, attackData.attackDuration, worldPosition, 0f, floorZ);
        }
        remainingLifetime = Mathf.Max(0.01f, attackData.attackDuration);

        if (attackData.visual != null)
        {
            Transform parent = visualRoot != null ? visualRoot : transform;
            GameObject visualObject = Instantiate(attackData.visual, parent);
            visualObject.name = "Visual";
            visualObject.transform.localPosition = visualLocalOffset;
            visualObject.transform.localRotation = Quaternion.identity;
            visualObject.transform.localScale = visualLocalScale;
        }
    }

    private void Update()
    {
        if (remainingLifetime < 0f)
            return;

        remainingLifetime -= Time.deltaTime;
        if (remainingLifetime <= 0f)
            Destroy(gameObject);
    }
}
