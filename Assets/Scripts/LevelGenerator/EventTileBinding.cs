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
public enum EventTileEffectType
{
    None = 0,
    ApplyPoisonToPlayer = 1,
    ApplyBurnToPlayer = 2,
    TriggerCustomEvent = 3,
}

[Serializable]
public class EventTileBinding
{
    public TileBase markerTile;
    public EventTileTriggerCondition triggerCondition = EventTileTriggerCondition.PlayerFloorZEqualsTileZ;
    public int tileZ = 0;
    public bool repeatWhileConditionMatches = false;

    [Header("Effect")]
    public EventTileEffectType effectType = EventTileEffectType.TriggerCustomEvent;

    [Header("Poison")]
    public float poisonDuration = 1f;
    public float poisonDamagePerSecond = 1f;

    [Header("Burn")]
    public float burnDuration = 1f;
    public float burnTickInterval = 0.25f;
    public float burnDamagePerTick = 1f;

    [Header("Custom Event")]
    public UnityEvent onTriggered;
}
