using System;
using UnityEngine;

[Serializable]
public class ProfileSaveData
{
    public const int CurrentVersion = 1;

    [SerializeField] private int version = CurrentVersion;
    [SerializeField] private ItemUnlockSnapshot itemUnlockSnapshot = new ItemUnlockSnapshot();
    [SerializeField] private SettingsSaveData settings = new SettingsSaveData();
    [SerializeField] private string lastSavedAtUtc = string.Empty;

    public int Version
    {
        get => version;
        set => version = Mathf.Max(1, value);
    }

    public ItemUnlockSnapshot ItemUnlockSnapshot
    {
        get
        {
            itemUnlockSnapshot ??= new ItemUnlockSnapshot();
            return itemUnlockSnapshot;
        }
        set => itemUnlockSnapshot = value ?? new ItemUnlockSnapshot();
    }

    public SettingsSaveData Settings
    {
        get
        {
            settings ??= new SettingsSaveData();
            return settings;
        }
        set => settings = value ?? new SettingsSaveData();
    }

    public string LastSavedAtUtc
    {
        get => lastSavedAtUtc;
        set => lastSavedAtUtc = value ?? string.Empty;
    }

    public void MarkSavedNow()
    {
        version = CurrentVersion;
        lastSavedAtUtc = DateTime.UtcNow.ToString("O");
    }
}
