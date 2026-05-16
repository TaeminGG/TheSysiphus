using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PickupItem : MonoBehaviour, IPlayerInteractable
{
    [Serializable]
    private class ShaderKeywordOverride
    {
        [Tooltip("AllIn1 shader keyword name. Example: GLOW_ON, OUTBASE_ON")]
        public string keyword = string.Empty;
        [Tooltip("Enable or disable the keyword for this rarity profile")]
        public bool enabled = true;
    }

    [Serializable]
    private class ShaderFloatOverride
    {
        [Tooltip("Shader float property name. Example: _Glow, _OutlineWidth")]
        public string propertyName = string.Empty;
        [Tooltip("Float value applied to the shader property")]
        public float value = 0f;
    }

    [Serializable]
    private class ShaderColorOverride
    {
        [Tooltip("Shader color property name. Example: _GlowColor, _OutlineColor, _Color")]
        public string propertyName = string.Empty;
        [Tooltip("If enabled, this property uses the rarity tint color below")]
        public bool useRarityColor = true;
        [Tooltip("Manual color used when Use Rarity Color is disabled")]
        public Color value = Color.white;
    }

    [Serializable]
    private class ShaderVectorOverride
    {
        [Tooltip("Shader vector property name")]
        public string propertyName = string.Empty;
        [Tooltip("Vector value applied to the shader property")]
        public Vector4 value = Vector4.zero;
    }

    [Serializable]
    private class RarityVisualProfile
    {
        [Tooltip("Apply the tint color below to the SpriteRenderer color")]
        public bool applySpriteTint = true;
        [Tooltip("Base color used for sprite tint and optional shader color overrides")]
        public Color tintColor = Color.white;
        [Tooltip("Keywords to toggle on the runtime material for this rarity")]
        public List<ShaderKeywordOverride> shaderKeywords = new List<ShaderKeywordOverride>();
        [Tooltip("Float shader properties to override for this rarity")]
        public List<ShaderFloatOverride> floatProperties = new List<ShaderFloatOverride>();
        [Tooltip("Color shader properties to override for this rarity")]
        public List<ShaderColorOverride> colorProperties = new List<ShaderColorOverride>();
        [Tooltip("Vector shader properties to override for this rarity")]
        public List<ShaderVectorOverride> vectorProperties = new List<ShaderVectorOverride>();
    }

    [Header("Interaction")]
    [Tooltip("Layers that are allowed to trigger pickup interaction")]
    public LayerMask hitLayer;
    [Tooltip("Interaction UI canvas shown when the player is in range")]
    public Canvas canvas;
    [Tooltip("Higher priority interactables are preferred when multiple are in range")]
    [SerializeField] private int interactionPriority = 20;

    [Header("UI")]
    [Tooltip("UI image that shows the picked item's icon")]
    public Image icon;
    [Tooltip("UI text for the item name")]
    public TextMeshProUGUI itemName;
    [Tooltip("UI text for the item description")]
    public TextMeshProUGUI itemDescription;
    [Tooltip("Optional UI text for the item price. If missing, price is appended to the description")]
    public TextMeshProUGUI priceText;

    [Header("Item Data")]
    [Tooltip("Passive item assigned to this pickup when Is Passive is enabled")]
    public PassiveItem item;
    [Tooltip("Active item assigned to this pickup when Is Passive is disabled")]
    public ActiveItem activeItem;
    [Tooltip("Choose whether this pickup gives a passive item or an active item")]
    public bool isPassive = true;

    [Header("Placement")]
    [Tooltip("Logical floor index used to match player and pickup interaction layers")]
    public int floorZ = 0;

    [Header("Shop")]
    [Tooltip("When enabled, the player must spend gold before picking up this item")]
    public bool requiresPurchase = false;
    [Tooltip("Gold cost used when Requires Purchase is enabled")]
    public int price = 0;
    [Tooltip("Merchant owner notified when purchase succeeds or fails")]
    public MerchantNPC merchant;

    [Header("Visual")]
    [Tooltip("SpriteRenderer that receives rarity tint and shader overrides. Defaults to the first SpriteRenderer found on this object or its children")]
    [SerializeField] private SpriteRenderer targetRenderer;
    [Tooltip("TrailRenderers that should use the resolved rarity tint. Defaults to child TrailRenderers under this pickup")]
    [SerializeField] private TrailRenderer[] rarityTrailRenderers = Array.Empty<TrailRenderer>();
    [Tooltip("Use PickupItem's own rarity value instead of reading the assigned item asset rarity")]
    [SerializeField] private bool useExplicitRarity = false;
    [Tooltip("Only used when Use Explicit Rarity is enabled")]
    [SerializeField] private ItemRarity explicitRarity = ItemRarity.Common;

    [Header("Rarity Visuals")]
    [Tooltip("Visual settings applied when the resolved rarity is Common")]
    [SerializeField] private RarityVisualProfile commonVisual = CreateDefaultProfile(ItemRarity.Common);
    [Tooltip("Visual settings applied when the resolved rarity is Uncommon")]
    [SerializeField] private RarityVisualProfile uncommonVisual = CreateDefaultProfile(ItemRarity.Uncommon);
    [Tooltip("Visual settings applied when the resolved rarity is Rare")]
    [SerializeField] private RarityVisualProfile rareVisual = CreateDefaultProfile(ItemRarity.Rare);
    [Tooltip("Visual settings applied when the resolved rarity is Epic")]
    [SerializeField] private RarityVisualProfile epicVisual = CreateDefaultProfile(ItemRarity.Epic);
    [Tooltip("Visual settings applied when the resolved rarity is Legendary")]
    [SerializeField] private RarityVisualProfile legendaryVisual = CreateDefaultProfile(ItemRarity.Legendary);

    private PlayerMove playerMove;
    private bool isPickedUp = false;
    private bool isPlayerInRange = false;
    private MaterialPropertyBlock shaderPropertyBlock;
    private Material runtimeMaterial;
    private bool hasCachedRendererDefaults = false;
    private bool hasCachedTrailDefaults = false;
    private Color defaultRendererColor = Color.white;
    private Sprite defaultRendererSprite;
    private string[] defaultShaderKeywords = Array.Empty<string>();
    private Gradient[] defaultTrailGradients = Array.Empty<Gradient>();
    private SpriteRenderer cachedDefaultRendererSource;
    private TrailRenderer[] cachedTrailRendererSources = Array.Empty<TrailRenderer>();
    private SpriteRenderer runtimeMaterialOwner;
    private ItemUnlockRegistry itemUnlockRegistry;
    private bool passiveReservationClaimed = false;
    private RunSaveIdentity runSaveIdentity;

    public int InteractionPriority => interactionPriority;
    public Vector3 InteractionPosition => transform.position;
    public ItemRarity CurrentRarity => ResolveRarity();

    private void Awake()
    {
        runSaveIdentity = RunSaveIdentity.Ensure(gameObject);
        CacheVisualReferences();
        EnsureVisualProfiles();
    }

    private void OnEnable()
    {
        RegisterRegistryListener();
    }

    private void Start()
    {
        RegisterRegistryListener();
        RefreshItemView();
        if (!EnsurePassiveAvailability())
            return;

        if (canvas != null)
            canvas.enabled = false;

        if (playerMove == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
                playerMove = player.GetComponent<PlayerMove>();
        }
    }

    private void OnValidate()
    {
        CacheVisualReferences();
        EnsureVisualProfiles();
    }

    private void OnDisable()
    {
        UnregisterInteractable();
        UnregisterRegistryListener();
    }

    private void OnDestroy()
    {
        ReleasePassiveReservation();
    }

    public bool CanInteract(PlayerMove player)
    {
        return !isPickedUp && isPlayerInRange && playerMove == player && IsPlayerOnSameFloor();
    }

    public void Interact(PlayerMove player)
    {
        if (!CanInteract(player))
            return;

        if ((isPassive && item == null) || (!isPassive && activeItem == null))
            return;

        if (!TryPurchase(player))
            return;

        isPickedUp = true;
        RunSaveRuntimeState.MarkPickupConsumed(GetSaveId());

        if (canvas != null)
            canvas.enabled = false;

        UnregisterInteractable();

        if (isPassive)
        {
            itemUnlockRegistry ??= ItemUnlockRegistry.FindInstance();
            itemUnlockRegistry?.Collect(item);
            PassiveItemList passiveItemList = player.GetComponent<PassiveItemList>();
            passiveItemList?.AddItem(item);
        }
        else
        {
            ActiveInventory inventory = player.GetComponent<ActiveInventory>();
            inventory?.AddItem(activeItem);
        }

        Destroy(gameObject);
    }

    public void SetInteractionFocus(PlayerMove player, bool isFocused)
    {
        RefreshCanvas();
    }

    public void ConfigureDropItem(bool passive, PassiveItem passiveItem, ActiveItem activeItemData, bool reservationAlreadyClaimed = false)
    {
        ReleasePassiveReservation();
        isPassive = passive;
        item = passiveItem;
        activeItem = activeItemData;
        passiveReservationClaimed = reservationAlreadyClaimed && passive && passiveItem != null;
        requiresPurchase = false;
        price = 0;
        merchant = null;
        useExplicitRarity = false;
        RefreshItemView();
        EnsurePassiveAvailability();
    }

    public void RestoreRunState(bool passive, PassiveItem passiveItem, ActiveItem activeItemData, int z, bool purchaseRequired, int itemPrice)
    {
        RestoreRunState(passive, passiveItem, activeItemData, z, purchaseRequired, itemPrice, false);
    }

    public void RestoreRunState(bool passive, PassiveItem passiveItem, ActiveItem activeItemData, int z, bool purchaseRequired, int itemPrice, bool preserveSavedAvailability)
    {
        ReleasePassiveReservation();
        isPickedUp = false;
        isPlayerInRange = false;
        isPassive = passive;
        item = passiveItem;
        activeItem = activeItemData;
        passiveReservationClaimed = preserveSavedAvailability && passive && passiveItem != null;
        requiresPurchase = purchaseRequired;
        price = Mathf.Max(0, itemPrice);
        merchant = null;
        SetZ(z);
        RefreshItemView();

        if (canvas != null)
            canvas.enabled = false;

        if (!preserveSavedAvailability)
            EnsurePassiveAvailability();
    }

    public string GetSaveId()
    {
        runSaveIdentity ??= RunSaveIdentity.Ensure(gameObject);
        return runSaveIdentity != null ? runSaveIdentity.SaveId : gameObject.name;
    }

    public void MarkPassiveReservationClaimed()
    {
        if (!isPassive || item == null)
            return;

        passiveReservationClaimed = true;
    }

    public void ConfigureAsMerchantItem(bool passive, PassiveItem passiveItem, ActiveItem activeItemData, int itemPrice, MerchantNPC merchantOwner, int z, bool reservationAlreadyClaimed = false)
    {
        ReleasePassiveReservation();
        isPassive = passive;
        item = passiveItem;
        activeItem = activeItemData;
        passiveReservationClaimed = reservationAlreadyClaimed && passive && passiveItem != null;
        requiresPurchase = true;
        price = Mathf.Max(0, itemPrice);
        merchant = merchantOwner;
        SetZ(z);
        useExplicitRarity = false;
        RefreshItemView();
        EnsurePassiveAvailability();
    }

    public void SetRarity(ItemRarity rarity)
    {
        useExplicitRarity = true;
        explicitRarity = rarity;
        ApplyVisuals();
    }

    public void ClearExplicitRarity()
    {
        useExplicitRarity = false;
        ApplyVisuals();
    }

    public void SetZ(int z)
    {
        floorZ = z;
        Vector3 pos = transform.position;
        pos.z = z;
        transform.position = pos;
    }

    private static RarityVisualProfile CreateDefaultProfile(ItemRarity rarity)
    {
        return new RarityVisualProfile
        {
            applySpriteTint = true,
            tintColor = ItemRarityUtility.GetDisplayColor(rarity)
        };
    }

    private void CacheVisualReferences()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (rarityTrailRenderers == null || rarityTrailRenderers.Length == 0)
            rarityTrailRenderers = GetComponentsInChildren<TrailRenderer>(true);

        if (targetRenderer != null && shaderPropertyBlock == null)
            shaderPropertyBlock = new MaterialPropertyBlock();

        CacheDefaultVisualState();
    }

    private void CacheDefaultVisualState()
    {
        CacheDefaultRendererState();
        CacheDefaultTrailStates();
    }

    private void CacheDefaultRendererState()
    {
        if (targetRenderer == null)
            return;

        if (hasCachedRendererDefaults && cachedDefaultRendererSource == targetRenderer)
            return;

        defaultRendererColor = targetRenderer.color;
        defaultRendererSprite = targetRenderer.sprite;

        Material sharedMaterial = targetRenderer.sharedMaterial;
        defaultShaderKeywords = sharedMaterial != null
            ? sharedMaterial.shaderKeywords ?? Array.Empty<string>()
            : Array.Empty<string>();

        cachedDefaultRendererSource = targetRenderer;
        hasCachedRendererDefaults = true;
    }

    private void CacheDefaultTrailStates()
    {
        TrailRenderer[] trailRenderers = rarityTrailRenderers ?? Array.Empty<TrailRenderer>();
        if (hasCachedTrailDefaults && TrailRendererArraysMatch(trailRenderers, cachedTrailRendererSources))
            return;

        defaultTrailGradients = new Gradient[trailRenderers.Length];
        cachedTrailRendererSources = new TrailRenderer[trailRenderers.Length];

        for (int i = 0; i < trailRenderers.Length; i++)
        {
            cachedTrailRendererSources[i] = trailRenderers[i];
            defaultTrailGradients[i] = trailRenderers[i] != null
                ? CloneGradient(trailRenderers[i].colorGradient)
                : null;
        }

        hasCachedTrailDefaults = true;
    }

    private void EnsureVisualProfiles()
    {
        commonVisual = EnsureVisualProfile(commonVisual, ItemRarity.Common);
        uncommonVisual = EnsureVisualProfile(uncommonVisual, ItemRarity.Uncommon);
        rareVisual = EnsureVisualProfile(rareVisual, ItemRarity.Rare);
        epicVisual = EnsureVisualProfile(epicVisual, ItemRarity.Epic);
        legendaryVisual = EnsureVisualProfile(legendaryVisual, ItemRarity.Legendary);
    }

    private static RarityVisualProfile EnsureVisualProfile(RarityVisualProfile profile, ItemRarity rarity)
    {
        if (profile == null)
            profile = CreateDefaultProfile(rarity);

        if (profile.tintColor.Equals(default(Color)))
            profile.tintColor = ItemRarityUtility.GetDisplayColor(rarity);

        profile.shaderKeywords ??= new List<ShaderKeywordOverride>();
        profile.floatProperties ??= new List<ShaderFloatOverride>();
        profile.colorProperties ??= new List<ShaderColorOverride>();
        profile.vectorProperties ??= new List<ShaderVectorOverride>();

        return profile;
    }

    private ItemRarity ResolveRarity()
    {
        if (useExplicitRarity)
            return explicitRarity;

        if (isPassive)
            return item != null ? item.Rarity : ItemRarity.Common;

        return activeItem != null ? activeItem.Rarity : ItemRarity.Common;
    }

    private RarityVisualProfile GetVisualProfile(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Uncommon:
                return uncommonVisual;
            case ItemRarity.Rare:
                return rareVisual;
            case ItemRarity.Epic:
                return epicVisual;
            case ItemRarity.Legendary:
                return legendaryVisual;
            default:
                return commonVisual;
        }
    }

    private void ApplyVisuals()
    {
        EnsureVisualProfiles();
        ResetVisualState();

        RarityVisualProfile profile = GetVisualProfile(ResolveRarity());
        if (profile == null)
            return;

        if (targetRenderer != null)
        {
            targetRenderer.color = profile.applySpriteTint ? profile.tintColor : Color.white;
            ApplyShaderKeywords(profile);
            ApplyShaderProperties(profile);
        }

        ApplyTrailColors(profile.tintColor);
    }

    private void ApplyShaderKeywords(RarityVisualProfile profile)
    {
        if (targetRenderer == null || targetRenderer.sharedMaterial == null)
            return;

        Material material = GetRuntimeMaterial();
        if (material == null)
            return;

        material.shaderKeywords = defaultShaderKeywords;

        if (profile.shaderKeywords == null || profile.shaderKeywords.Count == 0)
            return;

        for (int i = 0; i < profile.shaderKeywords.Count; i++)
        {
            ShaderKeywordOverride keywordOverride = profile.shaderKeywords[i];
            if (keywordOverride == null || string.IsNullOrWhiteSpace(keywordOverride.keyword))
                continue;

            if (keywordOverride.enabled)
                material.EnableKeyword(keywordOverride.keyword);
            else
                material.DisableKeyword(keywordOverride.keyword);
        }
    }

    private void ResetVisualState()
    {
        CacheDefaultVisualState();

        if (targetRenderer != null)
        {
            targetRenderer.color = defaultRendererColor;

            if (defaultRendererSprite != null)
                targetRenderer.sprite = defaultRendererSprite;

            if (shaderPropertyBlock == null)
                shaderPropertyBlock = new MaterialPropertyBlock();

            shaderPropertyBlock.Clear();
            targetRenderer.SetPropertyBlock(shaderPropertyBlock);

            Material material = GetRuntimeMaterial();
            if (material != null)
                material.shaderKeywords = defaultShaderKeywords;
        }

        ResetTrailVisuals();
    }

    private Material GetRuntimeMaterial()
    {
        if (targetRenderer == null)
            return null;

        if (runtimeMaterial == null || runtimeMaterialOwner != targetRenderer)
        {
            runtimeMaterial = targetRenderer.material;
            runtimeMaterialOwner = targetRenderer;
        }

        return runtimeMaterial;
    }

    private void ApplyShaderProperties(RarityVisualProfile profile)
    {
        if (shaderPropertyBlock == null || targetRenderer == null)
            return;

        targetRenderer.GetPropertyBlock(shaderPropertyBlock);

        ApplyFloatProperties(profile);
        ApplyColorProperties(profile);
        ApplyVectorProperties(profile);

        targetRenderer.SetPropertyBlock(shaderPropertyBlock);
    }

    private void ApplyFloatProperties(RarityVisualProfile profile)
    {
        if (profile.floatProperties == null)
            return;

        for (int i = 0; i < profile.floatProperties.Count; i++)
        {
            ShaderFloatOverride property = profile.floatProperties[i];
            if (property == null || string.IsNullOrWhiteSpace(property.propertyName))
                continue;

            shaderPropertyBlock.SetFloat(property.propertyName, property.value);
        }
    }

    private void ApplyColorProperties(RarityVisualProfile profile)
    {
        if (profile.colorProperties == null)
            return;

        for (int i = 0; i < profile.colorProperties.Count; i++)
        {
            ShaderColorOverride property = profile.colorProperties[i];
            if (property == null || string.IsNullOrWhiteSpace(property.propertyName))
                continue;

            Color color = property.useRarityColor ? profile.tintColor : property.value;
            shaderPropertyBlock.SetColor(property.propertyName, color);
        }
    }

    private void ApplyVectorProperties(RarityVisualProfile profile)
    {
        if (profile.vectorProperties == null)
            return;

        for (int i = 0; i < profile.vectorProperties.Count; i++)
        {
            ShaderVectorOverride property = profile.vectorProperties[i];
            if (property == null || string.IsNullOrWhiteSpace(property.propertyName))
                continue;

            shaderPropertyBlock.SetVector(property.propertyName, property.value);
        }
    }

    private void ResetTrailVisuals()
    {
        TrailRenderer[] trailRenderers = rarityTrailRenderers ?? Array.Empty<TrailRenderer>();
        if (trailRenderers.Length == 0)
            return;

        CacheDefaultTrailStates();

        for (int i = 0; i < trailRenderers.Length; i++)
        {
            TrailRenderer trailRenderer = trailRenderers[i];
            if (trailRenderer == null)
                continue;

            Gradient defaultGradient = i < defaultTrailGradients.Length
                ? defaultTrailGradients[i]
                : null;

            if (defaultGradient != null)
                trailRenderer.colorGradient = CloneGradient(defaultGradient);
        }
    }

    private void ApplyTrailColors(Color tintColor)
    {
        TrailRenderer[] trailRenderers = rarityTrailRenderers ?? Array.Empty<TrailRenderer>();
        if (trailRenderers.Length == 0)
            return;

        CacheDefaultTrailStates();

        for (int i = 0; i < trailRenderers.Length; i++)
        {
            TrailRenderer trailRenderer = trailRenderers[i];
            if (trailRenderer == null)
                continue;

            Gradient sourceGradient = i < defaultTrailGradients.Length
                ? defaultTrailGradients[i]
                : null;

            trailRenderer.colorGradient = CreateTintedGradient(sourceGradient, tintColor);
        }
    }

    private static bool TrailRendererArraysMatch(TrailRenderer[] left, TrailRenderer[] right)
    {
        left ??= Array.Empty<TrailRenderer>();
        right ??= Array.Empty<TrailRenderer>();

        if (left.Length != right.Length)
            return false;

        for (int i = 0; i < left.Length; i++)
        {
            if (left[i] != right[i])
                return false;
        }

        return true;
    }

    private static Gradient CloneGradient(Gradient source)
    {
        if (source == null)
            return null;

        Gradient clone = new Gradient
        {
            mode = source.mode
        };
        clone.SetKeys(source.colorKeys, source.alphaKeys);
        return clone;
    }

    private static Gradient CreateTintedGradient(Gradient source, Color tintColor)
    {
        Gradient baseGradient = CloneGradient(source) ?? new Gradient();
        GradientColorKey[] colorKeys = baseGradient.colorKeys;
        GradientAlphaKey[] alphaKeys = baseGradient.alphaKeys;

        if (colorKeys == null || colorKeys.Length == 0)
        {
            colorKeys = new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            };
        }

        if (alphaKeys == null || alphaKeys.Length == 0)
        {
            alphaKeys = new[]
            {
                new GradientAlphaKey(tintColor.a, 0f),
                new GradientAlphaKey(tintColor.a, 1f)
            };
        }

        for (int i = 0; i < colorKeys.Length; i++)
            colorKeys[i] = new GradientColorKey(tintColor, colorKeys[i].time);

        baseGradient.SetKeys(colorKeys, alphaKeys);
        return baseGradient;
    }

    private bool TryPurchase(PlayerMove player)
    {
        if (!requiresPurchase)
            return true;

        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (stats == null)
            return false;

        if (!stats.TrySpendGold(price))
        {
            merchant?.HandleInsufficientGold();
            return false;
        }

        merchant?.HandleItemPurchased(this);
        return true;
    }

    private void RegisterRegistryListener()
    {
        if (itemUnlockRegistry != null)
            return;

        itemUnlockRegistry = ItemUnlockRegistry.FindInstance();
        if (itemUnlockRegistry != null)
            itemUnlockRegistry.UnlocksChanged += HandleRegistryChanged;
    }

    private void UnregisterRegistryListener()
    {
        if (itemUnlockRegistry == null)
            return;

        itemUnlockRegistry.UnlocksChanged -= HandleRegistryChanged;
        itemUnlockRegistry = null;
    }

    private void HandleRegistryChanged()
    {
        if (isPickedUp)
            return;

        if (!EnsurePassiveAvailability())
            return;

        HideIfCollectedPassive();
    }

    private void HideIfCollectedPassive()
    {
        if (!Application.isPlaying || !isPassive || item == null)
            return;

        RegisterRegistryListener();
        if (itemUnlockRegistry == null || !itemUnlockRegistry.IsCollected(item))
            return;

        isPickedUp = true;
        if (canvas != null)
            canvas.enabled = false;

        UnregisterInteractable();
        Destroy(gameObject);
    }

    private bool EnsurePassiveAvailability()
    {
        if (!Application.isPlaying || !isPassive || item == null)
            return true;

        RegisterRegistryListener();
        if (itemUnlockRegistry == null)
            return true;

        if (passiveReservationClaimed)
        {
            HideIfCollectedPassive();
            return !isPickedUp;
        }

        if (itemUnlockRegistry.TryReservePassive(item))
        {
            passiveReservationClaimed = true;
            return true;
        }

        isPickedUp = true;
        if (canvas != null)
            canvas.enabled = false;

        UnregisterInteractable();
        Destroy(gameObject);
        return false;
    }

    private void ReleasePassiveReservation()
    {
        if (!Application.isPlaying || !passiveReservationClaimed || isPickedUp || !isPassive || item == null)
            return;

        if (itemUnlockRegistry == null)
            itemUnlockRegistry = ItemUnlockRegistry.FindInstance();

        itemUnlockRegistry?.ReleasePassiveReservation(item);
        passiveReservationClaimed = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayer) == 0)
            return;

        if (!other.TryGetComponent(out PlayerMove enteringPlayer))
            return;

        RefreshInteractionState(enteringPlayer);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayer) == 0)
            return;

        if (!other.TryGetComponent(out PlayerMove stayingPlayer))
            return;

        RefreshInteractionState(stayingPlayer);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayer) == 0)
            return;

        if (!other.TryGetComponent(out PlayerMove exitingPlayer))
            return;

        if (playerMove != null && playerMove != exitingPlayer)
            return;

        isPlayerInRange = false;

        if (canvas != null)
            canvas.enabled = false;

        UnregisterInteractable();
    }

    private void RefreshInteractionState(PlayerMove targetPlayer)
    {
        playerMove = targetPlayer;
        isPlayerInRange = true;

        RefreshCanvas();

        if (CanInteract(targetPlayer))
        {
            targetPlayer.RegisterInteractable(this);
            return;
        }

        targetPlayer.UnregisterInteractable(this);
    }

    private void RefreshCanvas()
    {
        if (canvas != null)
            canvas.enabled = !isPickedUp && isPlayerInRange && IsPlayerOnSameFloor();
    }

    private void UnregisterInteractable()
    {
        if (playerMove != null)
            playerMove.UnregisterInteractable(this);
    }

    private bool IsPlayerOnSameFloor()
    {
        return playerMove != null && playerMove.floorZ == floorZ;
    }

    private void RefreshItemView()
    {
        Sprite displayIcon = isPassive ? item?.icon : activeItem?.icon;
        string displayName = isPassive ? item?.itemName : activeItem?.itemName;
        string description = isPassive ? item?.description : activeItem?.description;

        if (icon != null)
            icon.sprite = displayIcon;

        if (itemName != null)
            itemName.text = displayName ?? string.Empty;

        if (itemDescription != null)
            itemDescription.text = GetDescriptionWithPrice(description ?? string.Empty);

        if (priceText != null)
        {
            priceText.gameObject.SetActive(requiresPurchase);
            priceText.text = requiresPurchase ? $"{price}G" : string.Empty;
        }

        ApplyVisuals();
    }

    private string GetDescriptionWithPrice(string baseDescription)
    {
        if (!requiresPurchase || priceText != null)
            return baseDescription;

        if (string.IsNullOrWhiteSpace(baseDescription))
            return $"Price: {price}G";

        return $"{baseDescription}\nPrice: {price}G";
    }
}
