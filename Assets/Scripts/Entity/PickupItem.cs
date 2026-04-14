using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PickupItem : MonoBehaviour, IPlayerInteractable
{
    [Header("Interaction")]
    public LayerMask hitLayer;
    public Canvas canvas;
    [SerializeField] private int interactionPriority = 20;

    [Header("UI")]
    public Image icon;
    public TextMeshProUGUI itemName;
    public TextMeshProUGUI itemDescription;
    public TextMeshProUGUI priceText;

    [Header("Item Data")]
    public PassiveItem item;
    public ActiveItem activeItem;
    public bool isPassive = true;

    [Header("Placement")]
    public int floorZ = 0;

    [Header("Shop")]
    public bool requiresPurchase = false;
    public int price = 0;
    public MerchantNPC merchant;

    private PlayerMove playerMove;
    private bool isPickedUp = false;
    private bool isPlayerInRange = false;

    public int InteractionPriority => interactionPriority;
    public Vector3 InteractionPosition => transform.position;

    private void Start()
    {
        RefreshItemView();

        if (canvas != null)
            canvas.enabled = false;

        if (playerMove == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
                playerMove = player.GetComponent<PlayerMove>();
        }
    }

    private void OnDisable()
    {
        UnregisterInteractable();
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

        if (canvas != null)
            canvas.enabled = false;

        UnregisterInteractable();

        if (isPassive)
        {
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

    public void ConfigureAsMerchantItem(bool passive, PassiveItem passiveItem, ActiveItem activeItemData, int itemPrice, MerchantNPC merchantOwner, int z)
    {
        isPassive = passive;
        item = passiveItem;
        activeItem = activeItemData;
        requiresPurchase = true;
        price = Mathf.Max(0, itemPrice);
        merchant = merchantOwner;
        SetZ(z);
        RefreshItemView();
    }

    public void SetZ(int z)
    {
        floorZ = z;
        Vector3 pos = transform.position;
        pos.z = z;
        transform.position = pos;
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
