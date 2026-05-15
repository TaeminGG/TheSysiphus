using UnityEngine;

public class EventTileActions : MonoBehaviour
{
    [SerializeField] private LevelManager levelManager;

    public void StartRun()
    {
        if (levelManager == null)
            levelManager = FindFirstObjectByType<LevelManager>();

        if (levelManager == null)
        {
            Debug.LogWarning("EventTileActions: LevelManager was not found, so the run could not start.", this);
            return;
        }

        levelManager.StartRun();
    }

    public void LogPoisonEvent()
    {
        Debug.Log("EventTileExampleActions: poison event triggered.", this);
    }

    public void LogArrowTriggerEvent()
    {
        Debug.Log("EventTileExampleActions: arrow trap event triggered.", this);
    }
}
