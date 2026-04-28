using UnityEngine;

public enum StatusAilmentType
{
    None = 0,
    Poison = 1,
    Burn = 2
}

public class StatusAilmentController : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer[] targetRenderers;
    [SerializeField] private bool autoCollectChildRenderers = true;
    [SerializeField] private Material poisonMaterial;
    [SerializeField] private Material burnMaterial;

    private IDamageable damageable;
    private IStatusTickDamageable statusTickDamageable;
    private Material[] defaultMaterials;

    private TimedDamageAilment poison;
    private TimedDamageAilment burn;

    public bool IsPoisoned => poison.IsActive;
    public bool IsBurning => burn.IsActive;

    private void Awake()
    {
        damageable = GetComponent<IDamageable>();
        statusTickDamageable = GetComponent<IStatusTickDamageable>();
        ResolveRenderers();
        CacheDefaultMaterials();
    }

    private void Update()
    {
        if (damageable == null || damageable.IsDead)
        {
            ClearExpiredVisualIfNeeded();
            return;
        }

        bool changed = false;
        changed |= UpdateAilment(ref poison);
        changed |= UpdateAilment(ref burn);

        if (changed)
            RefreshVisualMaterial();
    }

    public void ApplyPoison(float duration, float damagePerSecond)
    {
        if (duration <= 0f || damagePerSecond <= 0f)
            return;

        ApplyOrRefreshAilment(ref poison, StatusAilmentType.Poison, duration, 1f, damagePerSecond);
        RefreshVisualMaterial();
    }

    public void ApplyBurn(float duration, float tickInterval, float damagePerTick)
    {
        if (duration <= 0f || tickInterval <= 0f || damagePerTick <= 0f)
            return;

        ApplyOrRefreshAilment(ref burn, StatusAilmentType.Burn, duration, tickInterval, damagePerTick);
        RefreshVisualMaterial();
    }

    public void ClearPoison()
    {
        poison = default;
        RefreshVisualMaterial();
    }

    public void ClearBurn()
    {
        burn = default;
        RefreshVisualMaterial();
    }

    public void ClearAllAilments()
    {
        poison = default;
        burn = default;
        RefreshVisualMaterial();
    }

    private bool UpdateAilment(ref TimedDamageAilment ailment)
    {
        if (!ailment.IsActive)
            return false;

        ailment.RemainingDuration -= Time.deltaTime;
        ailment.TickTimer += Time.deltaTime;

        while (ailment.TickTimer >= ailment.TickInterval && ailment.RemainingDuration > -ailment.TickInterval)
        {
            ailment.TickTimer -= ailment.TickInterval;
            if (statusTickDamageable != null)
                statusTickDamageable.TakeStatusTickDamage(ailment.DamagePerTick);
            else
                damageable.TakeDamage(ailment.DamagePerTick, Vector2.zero);

            if (damageable == null || damageable.IsDead)
                break;
        }

        if (ailment.RemainingDuration > 0f)
            return false;

        ailment = default;
        return true;
    }

    private void ApplyOrRefreshAilment(ref TimedDamageAilment currentAilment, StatusAilmentType type, float duration, float tickInterval, float damagePerTick)
    {
        TimedDamageAilment incomingAilment = TimedDamageAilment.Create(type, duration, tickInterval, damagePerTick);

        if (!currentAilment.IsActive)
        {
            currentAilment = incomingAilment;
            return;
        }

        float currentStrength = currentAilment.GetStrengthScore();
        float incomingStrength = incomingAilment.GetStrengthScore();

        if (incomingStrength > currentStrength + 0.0001f)
        {
            currentAilment.TickInterval = incomingAilment.TickInterval;
            currentAilment.DamagePerTick = incomingAilment.DamagePerTick;
        }

        currentAilment.RemainingDuration = Mathf.Max(currentAilment.RemainingDuration, duration);
    }

    private void ResolveRenderers()
    {
        if (!autoCollectChildRenderers || targetRenderers != null && targetRenderers.Length > 0)
            return;

        targetRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void CacheDefaultMaterials()
    {
        if (targetRenderers == null)
            return;

        defaultMaterials = new Material[targetRenderers.Length];
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            SpriteRenderer renderer = targetRenderers[i];
            defaultMaterials[i] = renderer != null ? renderer.sharedMaterial : null;
        }
    }

    private void RefreshVisualMaterial()
    {
        if (targetRenderers == null || defaultMaterials == null)
            return;

        Material overrideMaterial = GetActiveStatusMaterial();

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            SpriteRenderer renderer = targetRenderers[i];
            if (renderer == null)
                continue;

            renderer.sharedMaterial = overrideMaterial != null ? overrideMaterial : defaultMaterials[i];
        }
    }

    private Material GetActiveStatusMaterial()
    {
        if (burn.IsActive && burnMaterial != null)
            return burnMaterial;

        if (poison.IsActive && poisonMaterial != null)
            return poisonMaterial;

        return null;
    }

    private void ClearExpiredVisualIfNeeded()
    {
        if (poison.IsActive || burn.IsActive)
            return;

        RefreshVisualMaterial();
    }

    private void OnDisable()
    {
        RefreshVisualMaterial();
    }

    private struct TimedDamageAilment
    {
        public StatusAilmentType Type;
        public float RemainingDuration;
        public float TickInterval;
        public float DamagePerTick;
        public float TickTimer;

        public bool IsActive => RemainingDuration > 0f;

        public float GetStrengthScore()
        {
            if (TickInterval <= 0f)
                return 0f;

            return DamagePerTick / TickInterval;
        }

        public static TimedDamageAilment Create(StatusAilmentType type, float duration, float tickInterval, float damagePerTick)
        {
            return new TimedDamageAilment
            {
                Type = type,
                RemainingDuration = duration,
                TickInterval = tickInterval,
                DamagePerTick = damagePerTick,
                TickTimer = 0f
            };
        }
    }
}
