using UnityEngine;

public interface IPlayerInteractable
{
    int InteractionPriority { get; }
    Vector3 InteractionPosition { get; }

    bool CanInteract(PlayerMove player);
    void Interact(PlayerMove player);
    void SetInteractionFocus(PlayerMove player, bool isFocused);
}
