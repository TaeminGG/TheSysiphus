using UnityEngine;

public class FountainOfHealth : MonoBehaviour
{
    [Header("Layer")]
    public LayerMask hitLayer;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    public Sprite depletedSprite;
    public Canvas canvas;
    public int floorZ = 0;


    private PlayerMove playerMove;
    private bool isPlayerInRange = false;
    private bool isUsed = false;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        canvas.enabled = false;
        transform.position = new Vector3(transform.position.x, transform.position.y, floorZ + 1);
    }

    private void OnDisable()
    {
        PlayerMove.OnPickup -= TryUse;
    }

    private void TryUse()
    {
        if (isUsed) return;
        if (!isPlayerInRange || playerMove == null) return;
        if (playerMove.floorZ != floorZ) return;

        canvas.enabled = false;
        isUsed = true;
        PlayerMove.OnPickup -= TryUse;

        if (depletedSprite != null && spriteRenderer != null)
            spriteRenderer.sprite = depletedSprite;

        if (!playerMove.TryGetComponent<PlayerStats>(out var stat)) return;
        stat.ChargePotion(stat.GetMaxPotionCount());
        stat.Heal(stat.GetMaxHp());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isUsed) return;
        if (((1 << other.gameObject.layer) & hitLayer) == 0) return;
        if (!other.TryGetComponent<PlayerMove>(out var enteringPlayer)) return;

        canvas.enabled = true;

        playerMove = enteringPlayer;
        isPlayerInRange = true;

        PlayerMove.OnPickup -= TryUse;
        PlayerMove.OnPickup += TryUse;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayer) == 0) return;
        if (!other.TryGetComponent<PlayerMove>(out var exitingPlayer)) return;
        if (playerMove != exitingPlayer) return;

        canvas.enabled = false;

        isPlayerInRange = false;
        playerMove = null;
        PlayerMove.OnPickup -= TryUse;
    }
    public void SetZ(int z)
    {
        floorZ = z;
        Vector3 pos = transform.position;
        pos.z = z;
        transform.position = pos;
    }
}
