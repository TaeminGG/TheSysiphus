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

    private PlayerMove playerMove;
    private bool isPickedUp = false;

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

    private void Pickup()
    {
        if (isPickedUp) return;
        isPickedUp = true;

        playerMove.GetComponent<PassiveItemList>().AddItem(item);
        PlayerMove.OnPickup -= Pickup;
        Destroy(gameObject);
    }

    private void ActivePickup()
    {

        if (isPickedUp) return;
        isPickedUp = true;

        playerMove.GetComponent<ActiveInventory>().AddItem(activeItem);
        PlayerMove.OnPickup -= ActivePickup;
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayer) == 0) return;

        canvas.enabled = true;

        if (isPassive)
        {
            PlayerMove.OnPickup -= Pickup;
            PlayerMove.OnPickup += Pickup;
        }
        else
        {
            PlayerMove.OnPickup -= ActivePickup;
            PlayerMove.OnPickup += ActivePickup;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayer) == 0) return;

        canvas.enabled = false;
        if (isPassive)
        {
            PlayerMove.OnPickup -= Pickup;
        }
        else
        {
            PlayerMove.OnPickup -= ActivePickup;
        }
    }


}
