using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerTileEventDetector : MonoBehaviour
{
    [Header("References")]
    public Grid grid;
    public Tilemap eventMap;
    public PlayerMove playerMove;
    public Transform tileCheckOrigin;

    [Header("Event Tile Bindings")]
    public List<EventTileBinding> eventTileBindings = new List<EventTileBinding>();
    public bool logMissingBindings = true;
    public float playerZTolerance = 0.01f;

    private Vector3Int lastTriggeredCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
    private TileBase lastTriggeredTile;
    private EventTileBinding lastTriggeredBinding;

    private void Awake()
    {
        if (playerMove == null)
            playerMove = GetComponent<PlayerMove>();

        if (tileCheckOrigin == null && playerMove != null && playerMove.shadow != null)
            tileCheckOrigin = playerMove.shadow;
    }

    private void Start()
    {
        GridUpdate();
    }

    private void FixedUpdate()
    {
        if (playerMove == null || eventMap == null)
        {
            ClearLastTriggeredState();
            return;
        }

        Vector3 worldPosition = GetDetectionWorldPosition();
        Vector3Int currentCell = eventMap.WorldToCell(worldPosition);
        TileBase markerTile = eventMap.GetTile(currentCell);

        if (markerTile == null)
        {
            ClearLastTriggeredState();
            return;
        }

        EventTileBinding binding = FindBinding(markerTile);
        if (binding == null)
        {
            if (logMissingBindings && (lastTriggeredTile != markerTile || lastTriggeredCell != currentCell))
                Debug.LogWarning($"PlayerTileEventDetector: no event binding found for marker tile '{markerTile.name}'.", this);

            ClearLastTriggeredState();
            return;
        }

        if (!MatchesTriggerCondition(binding))
        {
            ClearLastTriggeredState();
            return;
        }

        bool isSameTriggerContext = lastTriggeredTile == markerTile &&
                                    lastTriggeredCell == currentCell &&
                                    lastTriggeredBinding == binding;

        if (!binding.repeatWhileConditionMatches && isSameTriggerContext)
            return;

        binding.onTriggered?.Invoke();
        lastTriggeredTile = markerTile;
        lastTriggeredCell = currentCell;
        lastTriggeredBinding = binding;
    }

    private Vector3 GetDetectionWorldPosition()
    {
        if (tileCheckOrigin != null)
            return tileCheckOrigin.position;

        return transform.position;
    }

    private EventTileBinding FindBinding(TileBase markerTile)
    {
        for (int i = 0; i < eventTileBindings.Count; i++)
        {
            EventTileBinding binding = eventTileBindings[i];
            if (binding != null && binding.markerTile == markerTile)
                return binding;
        }

        return null;
    }

    private bool MatchesTriggerCondition(EventTileBinding binding)
    {
        switch (binding.triggerCondition)
        {
            case EventTileTriggerCondition.PlayerFloorZEqualsTileZ:
                return playerMove.floorZ == binding.tileZ;

            case EventTileTriggerCondition.PlayerZEqualsTileZ:
                return Mathf.Abs(playerMove.playerZ - binding.tileZ) <= playerZTolerance;

            default:
                return false;
        }
    }

    private void ClearLastTriggeredState()
    {
        lastTriggeredTile = null;
        lastTriggeredBinding = null;
        lastTriggeredCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
    }

    private void GridUpdate()
    {
        if (grid != null && eventMap != null)
            return;

        if (grid == null)
            grid = FindFirstObjectByType<Grid>();

        if (grid == null)
            return;

        Tilemap[] maps = grid.GetComponentsInChildren<Tilemap>();
        for (int i = 0; i < maps.Length; i++)
        {
            Tilemap map = maps[i];
            if (map == null)
                continue;

            if (map.gameObject.name.ToLowerInvariant() == "eventmap")
            {
                eventMap = map;
                break;
            }
        }
    }
}
