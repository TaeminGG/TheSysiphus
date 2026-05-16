using UnityEngine;

public class FountainOfHealth : MonoBehaviour, IPlayerInteractable
{
    [Header("Layer")]
    public LayerMask hitLayer;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    public Sprite depletedSprite;
    public Canvas canvas;
    public int floorZ = 0;
    [SerializeField] private int interactionPriority = 15;

    private PlayerMove playerMove;
    private bool isPlayerInRange = false;
    private bool isUsed = false;

    public int InteractionPriority => interactionPriority;
    public Vector3 InteractionPosition => transform.position;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (canvas != null)
            canvas.enabled = false;

        transform.position = new Vector3(transform.position.x, transform.position.y, floorZ + 1);
    }

    private void OnDisable()
    {
        UnregisterInteractable();
    }

    public bool CanInteract(PlayerMove player)
    {
        return !isUsed && isPlayerInRange && playerMove == player && player != null && player.floorZ == floorZ;
    }

    public void Interact(PlayerMove player)
    {
        if (!CanInteract(player))
            return;

        if (canvas != null)
            canvas.enabled = false;

        isUsed = true;
        UnregisterInteractable();
        GetComponent<RunSaveWorldObject>()?.MarkConsumedWithoutApplyingVisualState();

        if (depletedSprite != null && spriteRenderer != null)
            spriteRenderer.sprite = depletedSprite;

        PlayerStats stat = player.GetComponent<PlayerStats>();
        if (stat == null)
            return;

        stat.ChargePotion(stat.GetMaxPotionCount());
        stat.Heal(stat.GetMaxHp());
    }

    public void SetInteractionFocus(PlayerMove player, bool isFocused)
    {
        RefreshCanvas();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isUsed || ((1 << other.gameObject.layer) & hitLayer) == 0)
            return;

        if (!other.TryGetComponent(out PlayerMove enteringPlayer))
            return;

        RefreshInteractionState(enteringPlayer);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (isUsed || ((1 << other.gameObject.layer) & hitLayer) == 0)
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

        if (playerMove != exitingPlayer)
            return;

        isPlayerInRange = false;

        if (canvas != null)
            canvas.enabled = false;

        UnregisterInteractable();
    }

    public void SetZ(int z)
    {
        floorZ = z;
        Vector3 pos = transform.position;
        pos.z = z;
        transform.position = pos;
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
            canvas.enabled = !isUsed && isPlayerInRange && playerMove != null && playerMove.floorZ == floorZ;
    }

    private void UnregisterInteractable()
    {
        if (playerMove != null)
            playerMove.UnregisterInteractable(this);
    }
}
