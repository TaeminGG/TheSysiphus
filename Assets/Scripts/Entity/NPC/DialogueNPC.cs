using UnityEngine;

public class DialogueNPC : MonoBehaviour, IPlayerInteractable
{
    [Header("Interaction")]
    public LayerMask hitLayer;
    public Canvas interactionCanvas;
    public int floorZ = 0;
    [SerializeField] private int interactionPriority = 15;

    [Header("Dialogue")]
    public MerchantDialogueUI dialogueUI;
    public float dialogueDuration = 2.5f;
    [TextArea] public string[] firstEncounterLines;
    [TextArea] public string[] repeatLines;

    private PlayerMove playerMove;
    private bool isPlayerInRange;
    private int firstEncounterIndex;
    private int repeatIndex;

    public int InteractionPriority => interactionPriority;
    public Vector3 InteractionPosition => transform.position;

    private void Start()
    {
        if (interactionCanvas != null)
            interactionCanvas.enabled = false;
    }

    private void OnDisable()
    {
        if (interactionCanvas != null)
            interactionCanvas.enabled = false;

        if (dialogueUI != null)
            dialogueUI.HideImmediately();

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

        string line = GetNextLine();
        if (string.IsNullOrWhiteSpace(line) || dialogueUI == null)
            return;

        dialogueUI.Show(line, dialogueDuration);
    }

    public void SetInteractionFocus(PlayerMove player, bool isFocused)
    {
        if (interactionCanvas != null)
            interactionCanvas.enabled = isFocused && CanInteract(player);
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

        isPlayerInRange = false;

        if (interactionCanvas != null)
            interactionCanvas.enabled = false;

        UnregisterInteractable();
    }

    private void RefreshInteractionState(PlayerMove targetPlayer)
    {
        if (CanInteract(targetPlayer))
        {
            targetPlayer.RegisterInteractable(this);
            return;
        }

        targetPlayer.UnregisterInteractable(this);
    }

    private string GetNextLine()
    {
        if (firstEncounterLines != null && firstEncounterIndex < firstEncounterLines.Length)
            return firstEncounterLines[firstEncounterIndex++];

        string[] loopLines = HasLines(repeatLines) ? repeatLines : firstEncounterLines;
        if (!HasLines(loopLines))
            return string.Empty;

        string line = loopLines[repeatIndex % loopLines.Length];
        repeatIndex++;
        return line;
    }

    private static bool HasLines(string[] lines)
    {
        return lines != null && lines.Length > 0;
    }

    private void UnregisterInteractable()
    {
        if (playerMove != null)
            playerMove.UnregisterInteractable(this);
    }
}
