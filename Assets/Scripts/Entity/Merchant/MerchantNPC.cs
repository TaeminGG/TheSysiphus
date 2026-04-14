using UnityEngine;

public class MerchantNPC : MonoBehaviour, IPlayerInteractable
{
    [System.Serializable]
    public class DialogueSet
    {
        [TextArea] public string[] lines;

        public string GetRandomLine()
        {
            if (lines == null || lines.Length == 0)
                return string.Empty;

            return lines[Random.Range(0, lines.Length)];
        }
    }

    [Header("Interaction")]
    public LayerMask hitLayer;
    public Canvas interactionCanvas;
    public int floorZ = 0;
    [SerializeField] private int interactionPriority = 15;

    [Header("Dialogue")]
    public MerchantDialogueUI dialogueUI;
    public float dialogueDuration = 2.5f;
    public DialogueSet approachLines;
    public DialogueSet purchaseLines;
    public DialogueSet insufficientGoldLines;
    public DialogueSet leaveLines;
    public DialogueSet talkLines;

    private PlayerMove playerMove;
    private bool isPlayerInRange = false;
    private bool hasWelcomedCurrentVisit = false;

    public int InteractionPriority => interactionPriority;
    public Vector3 InteractionPosition => transform.position;

    private void Start()
    {
        if (interactionCanvas != null)
            interactionCanvas.enabled = false;
    }

    private void OnDisable()
    {
        UnregisterInteractable();
    }

    public bool CanInteract(PlayerMove player)
    {
        return isPlayerInRange && playerMove == player && player != null && player.floorZ == floorZ;
    }

    public void Interact(PlayerMove player)
    {
        if (!CanInteract(player))
            return;

        PlayDialogue(talkLines);
    }

    public void SetInteractionFocus(PlayerMove player, bool isFocused)
    {
        if (interactionCanvas != null)
            interactionCanvas.enabled = isFocused && CanInteract(player);
    }

    public void HandleItemPurchased(PickupItem purchasedItem)
    {
        PlayDialogue(purchaseLines);
    }

    public void HandleInsufficientGold()
    {
        PlayDialogue(insufficientGoldLines);
    }

    public void SetZ(int z)
    {
        floorZ = z;
        Vector3 pos = transform.position;
        pos.z = z;
        transform.position = pos;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayer) == 0)
            return;

        if (!other.TryGetComponent(out PlayerMove enteringPlayer))
            return;

        playerMove = enteringPlayer;
        isPlayerInRange = true;
        RefreshInteractionState(enteringPlayer);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayer) == 0)
            return;

        if (!other.TryGetComponent(out PlayerMove stayingPlayer))
            return;

        playerMove = stayingPlayer;
        isPlayerInRange = true;
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

        bool shouldPlayLeaveDialogue = hasWelcomedCurrentVisit;
        isPlayerInRange = false;
        hasWelcomedCurrentVisit = false;

        if (interactionCanvas != null)
            interactionCanvas.enabled = false;

        UnregisterInteractable();

        if (shouldPlayLeaveDialogue)
            PlayDialogue(leaveLines);
    }

    private void RefreshInteractionState(PlayerMove targetPlayer)
    {
        bool canInteract = CanInteract(targetPlayer);
        if (canInteract)
        {
            targetPlayer.RegisterInteractable(this);
            if (!hasWelcomedCurrentVisit)
            {
                hasWelcomedCurrentVisit = true;
                PlayDialogue(approachLines);
            }
        }
        else
        {
            targetPlayer.UnregisterInteractable(this);
            if (hasWelcomedCurrentVisit)
            {
                hasWelcomedCurrentVisit = false;
                PlayDialogue(leaveLines);
            }
        }
    }

    private void PlayDialogue(DialogueSet dialogueSet)
    {
        if (dialogueUI == null)
            return;

        string line = dialogueSet.GetRandomLine();
        if (string.IsNullOrWhiteSpace(line))
            return;

        dialogueUI.Show(line, dialogueDuration);
    }

    private void UnregisterInteractable()
    {
        if (playerMove != null)
            playerMove.UnregisterInteractable(this);
    }
}
