using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseItemCollectionSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ISelectHandler
{
    private PauseItemCollectionMenu.ItemEntryViewModel entry;
    private Action<PauseItemCollectionMenu.ItemEntryViewModel> hovered;
    private Action hoverEnded;
    private Outline selectionOutline;
    private Color selectedOutlineColor = Color.white;

    public void Initialize(
        PauseItemCollectionMenu.ItemEntryViewModel entryData,
        Action<PauseItemCollectionMenu.ItemEntryViewModel> hoveredCallback,
        Action hoverEndedCallback)
    {
        entry = entryData;
        hovered = hoveredCallback;
        hoverEnded = hoverEndedCallback;
    }

    public bool Matches(PauseItemCollectionMenu.ItemEntryViewModel other)
    {
        return entry.DisplayName == other.DisplayName
            && entry.CategoryLabel == other.CategoryLabel
            && entry.IsUnlocked == other.IsUnlocked;
    }

    public void SetSelectionOutline(Outline outline, Color color)
    {
        selectionOutline = outline;
        selectedOutlineColor = color;
        selectionOutline.effectColor = selectedOutlineColor;
        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        if (selectionOutline == null)
            return;

        selectionOutline.enabled = isSelected;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered?.Invoke(entry);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        hovered?.Invoke(entry);
    }

    public void OnSelect(BaseEventData eventData)
    {
        hovered?.Invoke(entry);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hoverEnded?.Invoke();
    }
}
