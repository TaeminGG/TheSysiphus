using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameServicesBootstrap : MonoBehaviour
{
    private void Awake()
    {
        SaveManager.EnsureInstance();
        SettingsManager.EnsureInstance();
    }
}
