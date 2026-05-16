using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PauseFullscreenToggleVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Sprite offSprite;
    [SerializeField] private Sprite onSprite;
    [SerializeField] private Color offColor = Color.white;
    [SerializeField] private Color onColor = Color.white;
    [SerializeField] private Color hoverColor = Color.white;

    private bool isFullscreen;
    private bool isPointerOver;

    public void SetVisuals(
        Image targetBackground,
        TextMeshProUGUI targetLabel,
        Sprite targetOffSprite,
        Sprite targetOnSprite,
        Color targetOffColor,
        Color targetOnColor,
        Color targetHoverColor)
    {
        backgroundImage = targetBackground != null ? targetBackground : GetComponent<Image>();
        label = targetLabel;
        offSprite = targetOffSprite;
        onSprite = targetOnSprite;
        offColor = targetOffColor;
        onColor = targetOnColor;
        hoverColor = targetHoverColor;
        Apply();
    }

    public void SetFullscreen(bool value)
    {
        isFullscreen = value;
        Apply();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
        Apply();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        Apply();
    }

    private void Apply()
    {
        if (backgroundImage != null)
        {
            Sprite sprite = isFullscreen ? onSprite : offSprite;
            backgroundImage.sprite = sprite;
            backgroundImage.color = isPointerOver ? hoverColor : (isFullscreen ? onColor : offColor);
            backgroundImage.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            backgroundImage.preserveAspect = false;
        }

        if (label != null)
            label.color = Color.white;
    }
}
