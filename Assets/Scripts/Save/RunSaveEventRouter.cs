using UnityEngine;

[DisallowMultipleComponent]
public sealed class RunSaveEventRouter : MonoBehaviour
{
    [SerializeField] private bool saveOnApplicationPause = true;
    [SerializeField] private bool saveOnApplicationQuit = true;
    [SerializeField] private string defaultReason = "manual";

    public void SaveNow()
    {
        SaveNow(defaultReason);
    }

    public void SaveNow(string reason)
    {
        SaveManager saveManager = SaveManager.EnsureInstance();
        if (saveManager.HasActiveRun)
            saveManager.SaveCurrentRun(reason);
    }

    public void RestoreNow()
    {
        SaveManager.EnsureInstance().RestoreCurrentRun();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && saveOnApplicationPause)
            SaveNow("application-pause");
    }

    private void OnApplicationQuit()
    {
        if (saveOnApplicationQuit)
            SaveNow("application-quit");
    }
}
