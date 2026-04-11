using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PickupItem : MonoBehaviour
{
    public LayerMask hitLayer;
    public Canvas canvas;
    public Image icon;
    public TextMeshProUGUI itemName;
    public TextMeshProUGUI itemDescription;

    public PassiveItem item;
    public ActiveItem activeItem;

    public int floorZ = 0;

    private PlayerMove playerMove;
    private bool isPickedUp = false;
    private bool isInteractable = false;

    public bool isPassive = true;               // 패시브 아이템인지 액티브 아이템인지

    void Start()
    {
        if (isPassive)
        {
            icon.sprite = item.icon;
            itemName.text = item.itemName;
            itemDescription.text = item.description;
        }
        else
        {
            icon.sprite = activeItem.icon;
            itemName.text = activeItem.itemName;
            itemDescription.text = activeItem.description;
        }
        canvas.enabled = false;
        playerMove = GameObject.Find("Player").GetComponent<PlayerMove>();
    }

    private void OnDisable()
    {
        UnregisterPickup();
    }

    private void Pickup()
    {
        if (isPickedUp) return;
        if (!IsPlayerOnSameFloor()) return;
        isPickedUp = true;

        playerMove.GetComponent<PassiveItemList>().AddItem(item);
        PlayerMove.OnPickup -= Pickup;
        Destroy(gameObject);
    }

    private void ActivePickup()
    {

        if (isPickedUp) return;
        if (!IsPlayerOnSameFloor()) return;
        isPickedUp = true;

        playerMove.GetComponent<ActiveInventory>().AddItem(activeItem);
        PlayerMove.OnPickup -= ActivePickup;
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayer) == 0) return;
        if (!other.TryGetComponent<PlayerMove>(out var enteringPlayer)) return;

        RefreshInteractionState(enteringPlayer);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayer) == 0) return;
        if (!other.TryGetComponent<PlayerMove>(out var stayingPlayer)) return;

        RefreshInteractionState(stayingPlayer);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayer) == 0) return;
        if (!other.TryGetComponent<PlayerMove>(out var exitingPlayer)) return;
        if (playerMove != null && playerMove != exitingPlayer) return;

        canvas.enabled = false;
        isInteractable = false;
        UnregisterPickup();
    }

    public void SetZ(int z)
    {
        floorZ = z;
        Vector3 pos = transform.position;
        pos.z = z;
        transform.position = pos;
    }

    private bool IsPlayerOnSameFloor()
    {
        return playerMove != null && playerMove.floorZ == floorZ;
    }

    private void RefreshInteractionState(PlayerMove targetPlayer)
    {
        playerMove = targetPlayer;

        bool canInteract = IsPlayerOnSameFloor();
        if (isInteractable == canInteract)
        {
            if (canInteract)
                RegisterPickup();
            return;
        }

        isInteractable = canInteract;
        canvas.enabled = canInteract;

        if (canInteract)
        {
            RegisterPickup();
            return;
        }

        UnregisterPickup();
    }

    private void RegisterPickup()
    {
        if (isPassive)
        {
            PlayerMove.OnPickup -= Pickup;
            PlayerMove.OnPickup += Pickup;
            return;
        }

        PlayerMove.OnPickup -= ActivePickup;
        PlayerMove.OnPickup += ActivePickup;
    }

    private void UnregisterPickup()
    {
        PlayerMove.OnPickup -= Pickup;
        PlayerMove.OnPickup -= ActivePickup;
    }


}
