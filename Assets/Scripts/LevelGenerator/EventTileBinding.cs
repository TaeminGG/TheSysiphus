using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

[Serializable]
public enum EventTileTriggerCondition
{
    PlayerFloorZEqualsTileZ = 0,
    PlayerZEqualsTileZ = 1,
}

[Serializable]
public class EventTileBinding
{
    public TileBase markerTile;
    public EventTileTriggerCondition triggerCondition = EventTileTriggerCondition.PlayerFloorZEqualsTileZ;
    public int tileZ = 0;
    public bool repeatWhileConditionMatches = false;
    public UnityEvent onTriggered;
}
