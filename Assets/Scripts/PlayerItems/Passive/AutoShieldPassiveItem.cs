using UnityEngine;

[CreateAssetMenu(fileName = "AutoShieldItem", menuName = "Scriptable Objects/PassiveItem/AutoShield")]
public class AutoShieldPassiveItem : PassiveItem
{
    public float cooldown = 6f;
    public Sprite shieldSprite;
    public Color shieldColor = new Color(0.5f, 0.9f, 1f, 0.35f);
    public Vector3 visualScale = new Vector3(1.6f, 1.6f, 1f);

    public override void OnEquip(PlayerStats stats)
    {
        AutoShieldPassiveEffect effect = PassiveEffectComponentUtility.GetOrAddComponent<AutoShieldPassiveEffect>(stats.gameObject);
        effect.Initialize(stats, this);
    }

    public override void OnUnequip(PlayerStats stats)
    {
        if (stats.TryGetComponent(out AutoShieldPassiveEffect effect))
            effect.Uninitialize(this);
    }
}

public sealed class AutoShieldPassiveEffect : MonoBehaviour
{
    private PlayerStats stats;
    private AutoShieldPassiveItem source;
    private bool shieldReady;
    private float cooldownRemaining;
    private GameObject shieldVisualObject;

    public void Initialize(PlayerStats playerStats, AutoShieldPassiveItem item)
    {
        if (source == item)
            return;

        Cleanup();

        stats = playerStats;
        source = item;
        shieldReady = true;
        cooldownRemaining = 0f;

        stats.BeforeDamageTaken += HandleBeforeDamageTaken;
        stats.OnDamaged += HandleDamaged;
        EnsureVisual();
        RefreshVisualState();
    }

    public void Uninitialize(AutoShieldPassiveItem item)
    {
        if (source != item)
            return;

        Cleanup();
        Destroy(this);
    }

    private void Update()
    {
        if (shieldReady || source == null)
            return;

        cooldownRemaining -= Time.deltaTime;
        if (cooldownRemaining > 0f)
            return;

        shieldReady = true;
        cooldownRemaining = 0f;
        RefreshVisualState();
    }

    private bool HandleBeforeDamageTaken(float amount, Vector2 knockback)
    {
        if (!shieldReady || source == null)
            return false;

        shieldReady = false;
        cooldownRemaining = source.cooldown;
        RefreshVisualState();
        return true;
    }

    private void HandleDamaged(float amount)
    {
        if (source == null || shieldReady || amount <= 0f)
            return;

        cooldownRemaining = source.cooldown;
    }

    private void EnsureVisual()
    {
        if (shieldVisualObject != null || source == null || source.shieldSprite == null)
            return;

        shieldVisualObject = new GameObject("AutoShieldVisual");
        shieldVisualObject.transform.SetParent(transform, false);
        shieldVisualObject.transform.localPosition = Vector3.zero;
        shieldVisualObject.transform.localScale = source.visualScale;

        SpriteRenderer renderer = shieldVisualObject.AddComponent<SpriteRenderer>();
        renderer.sprite = source.shieldSprite;
        renderer.color = source.shieldColor;
        renderer.sortingOrder = 4;
    }

    private void RefreshVisualState()
    {
        if (shieldVisualObject != null)
            shieldVisualObject.SetActive(shieldReady);
    }

    private void Cleanup()
    {
        if (stats != null)
        {
            stats.BeforeDamageTaken -= HandleBeforeDamageTaken;
            stats.OnDamaged -= HandleDamaged;
        }

        if (shieldVisualObject != null)
            Destroy(shieldVisualObject);

        stats = null;
        source = null;
        shieldVisualObject = null;
    }

    private void OnDestroy()
    {
        Cleanup();
    }
}
