using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class PauseMenuSettingsEditModeTests
{
    [Test]
    public void RebuildEditableLayout_CreatesSettingsControlsAndCreditsPanel()
    {
        GameObject root = new GameObject("PauseMenuSettingsTest", typeof(RectTransform));

        try
        {
            PauseItemCollectionMenu menu = root.AddComponent<PauseItemCollectionMenu>();

            menu.RebuildEditableLayout();

            Assert.That(FindDescendant(root.transform, "SettingsPanel"), Is.Not.Null);
            Assert.That(FindDescendant(root.transform, "SettingsControlsFrame"), Is.Not.Null);
            Assert.That(FindDescendant(root.transform, "FullscreenToggleButton"), Is.Not.Null);
            Assert.That(FindDescendant(root.transform, "SaveAndExitButton"), Is.Not.Null);
            Assert.That(FindDescendant(root.transform, "SettingsCreditsPanel"), Is.Not.Null);
            Assert.That(FindDescendant(root.transform, "CreditsTextBox"), Is.Not.Null);

            Transform fullscreenButton = FindDescendant(root.transform, "FullscreenToggleButton");
            Assert.That(fullscreenButton.GetComponent<Button>(), Is.Not.Null);
            Assert.That(fullscreenButton.GetComponent<PauseFullscreenToggleVisual>(), Is.Not.Null);

            Transform saveAndExitButton = FindDescendant(root.transform, "SaveAndExitButton");
            Assert.That(saveAndExitButton.GetComponent<Button>(), Is.Not.Null);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void FullscreenToggleVisual_UsesStateSpritesAndHoverColor()
    {
        GameObject buttonObject = new GameObject("FullscreenToggleVisualTest", typeof(RectTransform));
        Sprite offSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero);
        Sprite onSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero);

        try
        {
            Image image = buttonObject.AddComponent<Image>();
            TextMeshProUGUI label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(buttonObject.transform, false);

            PauseFullscreenToggleVisual visual = buttonObject.AddComponent<PauseFullscreenToggleVisual>();
            Color offColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            Color onColor = new Color(0.1f, 0.6f, 0.2f, 1f);
            Color hoverColor = new Color(0.8f, 0.7f, 0.3f, 1f);

            visual.SetVisuals(image, label, offSprite, onSprite, offColor, onColor, hoverColor);

            visual.SetFullscreen(false);
            Assert.That(image.sprite, Is.EqualTo(offSprite));
            Assert.That(image.color, Is.EqualTo(offColor));

            visual.SetFullscreen(true);
            Assert.That(image.sprite, Is.EqualTo(onSprite));
            Assert.That(image.color, Is.EqualTo(onColor));

            visual.OnPointerEnter(new PointerEventData(null));
            Assert.That(image.color, Is.EqualTo(hoverColor));

            visual.OnPointerExit(new PointerEventData(null));
            Assert.That(image.color, Is.EqualTo(onColor));
        }
        finally
        {
            Object.DestroyImmediate(buttonObject);
            Object.DestroyImmediate(offSprite);
            Object.DestroyImmediate(onSprite);
        }
    }

    [Test]
    public void RebuildEditableLayout_HidesSaveAndExitButton_InTitleMenuMode()
    {
        GameObject root = new GameObject("TitleMenuSettingsTest", typeof(RectTransform));

        try
        {
            PauseItemCollectionMenu menu = root.AddComponent<PauseItemCollectionMenu>();
            menu.ConfigureTitleMenuModeForTests(true, root);

            menu.RebuildEditableLayout();

            Assert.That(FindDescendant(root.transform, "FullscreenToggleButton"), Is.Not.Null);
            Assert.That(FindDescendant(root.transform, "SaveAndExitButton"), Is.Null);
            Assert.That(FindDescendant(root.transform, "SettingsCreditsPanel"), Is.Not.Null);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static Transform FindDescendant(Transform parent, string objectName)
    {
        if (parent == null)
            return null;

        if (parent.name == objectName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = FindDescendant(parent.GetChild(i), objectName);
            if (child != null)
                return child;
        }

        return null;
    }
}
