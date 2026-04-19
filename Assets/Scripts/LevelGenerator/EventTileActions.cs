using UnityEngine;

public class EventTileActions : MonoBehaviour
{
    public void LogPoisonEvent()
    {
        Debug.Log("EventTileExampleActions: poison event triggered.", this);
    }

    public void LogArrowTriggerEvent()
    {
        Debug.Log("EventTileExampleActions: arrow trap event triggered.", this);
    }
}
