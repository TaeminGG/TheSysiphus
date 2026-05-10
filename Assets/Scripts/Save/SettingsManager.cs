using UnityEngine;

[DisallowMultipleComponent]
public sealed class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private SettingsSaveData settings;

    public SettingsSaveData Settings => settings ??= SaveManager.EnsureInstance().Profile.Settings;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
        Apply();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static SettingsManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        SettingsManager existing = FindFirstObjectByType<SettingsManager>();
        if (existing != null)
            return existing;

        GameObject managerObject = new GameObject("SettingsManager");
        return managerObject.AddComponent<SettingsManager>();
    }

    public void Load()
    {
        settings = SaveManager.EnsureInstance().Profile.Settings;
    }

    public void Save()
    {
        SaveManager saveManager = SaveManager.EnsureInstance();
        saveManager.Profile.Settings = Settings;
        saveManager.SaveProfile();
    }

    public void Apply()
    {
        AudioListener.volume = Settings.MasterVolume;
        Screen.fullScreen = Settings.Fullscreen;
    }

    public void SetMasterVolume(float value)
    {
        Settings.MasterVolume = value;
        Apply();
        Save();
    }

    public void SetMusicVolume(float value)
    {
        Settings.MusicVolume = value;
        Save();
    }

    public void SetSfxVolume(float value)
    {
        Settings.SfxVolume = value;
        Save();
    }

    public void SetUiVolume(float value)
    {
        Settings.UiVolume = value;
        Save();
    }

    public void SetFullscreen(bool value)
    {
        Settings.Fullscreen = value;
        Apply();
        Save();
    }

    public void SetMouseSensitivity(float value)
    {
        Settings.MouseSensitivity = value;
        Save();
    }
}
