using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class PauseItemCollectionSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private PauseItemCollectionMenu.ItemEntryViewModel entry;
    private Action<PauseItemCollectionMenu.ItemEntryViewModel> hovered;
    private Action hoverEnded;

    public void Initialize(
        PauseItemCollectionMenu.ItemEntryViewModel entryData,
        Action<PauseItemCollectionMenu.ItemEntryViewModel> hoveredCallback,
        Action hoverEndedCallback)
    {
        entry = entryData;
        hovered = hoveredCallback;
        hoverEnded = hoverEndedCallback;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered?.Invoke(entry);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hoverEnded?.Invoke();
    }
}
