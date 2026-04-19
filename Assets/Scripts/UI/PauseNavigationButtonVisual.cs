using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PauseNavigationButtonVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Targets")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;

    [Header("Background Overrides")]
    [SerializeField] private bool overrideBackgroundSprites;
    [SerializeField] private Sprite normalBackgroundSprite;
    [SerializeField] private Sprite selectedBackgroundSprite;
    [SerializeField] private bool overrideBackgroundColors;
    [SerializeField] private Color normalBackgroundColor = Color.white;
    [SerializeField] private Color selectedBackgroundColor = Color.white;

    [Header("Icon Overrides")]
    [SerializeField] private bool overrideIconSprites;
    [SerializeField] private Sprite normalIconSprite;
    [SerializeField] private Sprite selectedIconSprite;
    [SerializeField] private bool overrideIconColors;
    [SerializeField] private Color normalIconColor = Color.white;
    [SerializeField] private Color selectedIconColor = Color.white;

    [Header("Unselected Hover Colors")]
    [SerializeField] private bool overrideUnselectedHoverBackgroundColor;
    [SerializeField] private Color unselectedHoverBackgroundColor = Color.white;
    [SerializeField] private bool overrideUnselectedHoverIconColor;
    [SerializeField] private Color unselectedHoverIconColor = Color.white;

    private Sprite defaultIconSprite;
    private Color defaultIconColor = Color.white;
    private Sprite defaultBackgroundSprite;
    private Color defaultBackgroundColor = Color.white;
    private Sprite resolvedNormalBackgroundSprite;
    private Sprite resolvedSelectedBackgroundSprite;
    private Color resolvedNormalBackgroundColor = Color.white;
    private Color resolvedSelectedBackgroundColor = Color.white;
    private Sprite resolvedNormalIconSprite;
    private Sprite resolvedSelectedIconSprite;
    private Color resolvedNormalIconColor = Color.white;
    private Color resolvedSelectedIconColor = Color.white;
    private bool hasCachedDefaults;
    private bool hasResolvedVisualState;
    private bool isSelected;
    private bool isPointerOver;

    public Image IconImage => iconImage;

    private void Awake()
    {
        RefreshReferences(true);
    }

    private void Reset()
    {
        RefreshReferences(true);
    }

    private void OnValidate()
    {
        RefreshReferences(true);
        ApplyCurrentVisualState();
    }

    public void RefreshReferences(bool forceRecache = false)
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (iconImage == null)
            iconImage = FindIconImage();

        CacheDefaults(forceRecache);
        EnsureResolvedVisualState();
    }

    public void ApplySelected(
        bool isSelected,
        Sprite fallbackNormalBackgroundSprite,
        Sprite fallbackSelectedBackgroundSprite,
        Color fallbackNormalIconColor,
        Color fallbackSelectedIconColor)
    {
        RefreshReferences();
        this.isSelected = isSelected;

        if (backgroundImage != null)
        {
            resolvedNormalBackgroundSprite = overrideBackgroundSprites
                ? (normalBackgroundSprite != null ? normalBackgroundSprite : defaultBackgroundSprite)
                : (fallbackNormalBackgroundSprite != null ? fallbackNormalBackgroundSprite : defaultBackgroundSprite);
            resolvedSelectedBackgroundSprite = overrideBackgroundSprites
                ? (selectedBackgroundSprite != null ? selectedBackgroundSprite : resolvedNormalBackgroundSprite)
                : (fallbackSelectedBackgroundSprite != null ? fallbackSelectedBackgroundSprite : resolvedNormalBackgroundSprite);
            resolvedNormalBackgroundColor = overrideBackgroundColors ? normalBackgroundColor : defaultBackgroundColor;
            resolvedSelectedBackgroundColor = overrideBackgroundColors ? selectedBackgroundColor : defaultBackgroundColor;
        }

        if (iconImage != null)
        {
            resolvedNormalIconSprite = overrideIconSprites
                ? (normalIconSprite != null ? normalIconSprite : defaultIconSprite)
                : defaultIconSprite;
            resolvedSelectedIconSprite = overrideIconSprites
                ? (selectedIconSprite != null ? selectedIconSprite : resolvedNormalIconSprite)
                : defaultIconSprite;
            resolvedNormalIconColor = overrideIconColors ? normalIconColor : defaultIconColor;
            resolvedSelectedIconColor = overrideIconColors ? selectedIconColor : defaultIconColor;
        }

        hasResolvedVisualState = true;
        ApplyCurrentVisualState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
        ApplyCurrentVisualState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        ApplyCurrentVisualState();
    }

    private Image FindIconImage()
    {
        Image[] images = GetComponentsInChildren<Image>(true);
        Image fallback = null;

        for (int i = 0; i < images.Length; i++)
        {
            Image candidate = images[i];
            if (candidate == null || candidate == backgroundImage)
                continue;

            if (candidate.name == "Icon")
                return candidate;

            if (fallback == null)
                fallback = candidate;
        }

        return fallback;
    }

    private void CacheDefaults(bool forceRecache)
    {
        if (hasCachedDefaults && !forceRecache)
            return;

        if (backgroundImage != null)
        {
            defaultBackgroundSprite = backgroundImage.sprite;
            defaultBackgroundColor = backgroundImage.color;
        }

        if (iconImage != null)
        {
            defaultIconSprite = iconImage.sprite;
            defaultIconColor = iconImage.color;
        }

        if (defaultBackgroundColor.a <= 0f)
            defaultBackgroundColor = Color.white;

        if (defaultIconColor.a <= 0f)
            defaultIconColor = Color.white;

        hasCachedDefaults = true;
        hasResolvedVisualState = false;
    }

    private void EnsureResolvedVisualState()
    {
        if (hasResolvedVisualState)
            return;

        resolvedNormalBackgroundSprite = defaultBackgroundSprite;
        resolvedSelectedBackgroundSprite = defaultBackgroundSprite;
        resolvedNormalBackgroundColor = defaultBackgroundColor;
        resolvedSelectedBackgroundColor = defaultBackgroundColor;
        resolvedNormalIconSprite = defaultIconSprite;
        resolvedSelectedIconSprite = defaultIconSprite;
        resolvedNormalIconColor = defaultIconColor;
        resolvedSelectedIconColor = defaultIconColor;
        hasResolvedVisualState = true;
    }

    private void ApplyCurrentVisualState()
    {
        EnsureResolvedVisualState();

        if (backgroundImage != null)
        {
            backgroundImage.sprite = isSelected ? resolvedSelectedBackgroundSprite : resolvedNormalBackgroundSprite;
            backgroundImage.color = ResolveBackgroundColor();
            backgroundImage.type = backgroundImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            backgroundImage.preserveAspect = false;
        }

        if (iconImage != null)
        {
            iconImage.sprite = isSelected ? resolvedSelectedIconSprite : resolvedNormalIconSprite;
            iconImage.color = ResolveIconColor();
        }
    }

    private Color ResolveBackgroundColor()
    {
        if (!isSelected && isPointerOver && overrideUnselectedHoverBackgroundColor)
            return unselectedHoverBackgroundColor;

        return isSelected ? resolvedSelectedBackgroundColor : resolvedNormalBackgroundColor;
    }

    private Color ResolveIconColor()
    {
        if (!isSelected && isPointerOver && overrideUnselectedHoverIconColor)
            return unselectedHoverIconColor;

        return isSelected ? resolvedSelectedIconColor : resolvedNormalIconColor;
    }
}
