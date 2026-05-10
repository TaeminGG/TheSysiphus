using System;
using UnityEngine;

[Serializable]
public class SettingsSaveData
{
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float uiVolume = 1f;
    [SerializeField] private bool fullscreen = true;
    [SerializeField, Min(0.1f)] private float mouseSensitivity = 1f;

    public float MasterVolume
    {
        get => masterVolume;
        set => masterVolume = Mathf.Clamp01(value);
    }

    public float MusicVolume
    {
        get => musicVolume;
        set => musicVolume = Mathf.Clamp01(value);
    }

    public float SfxVolume
    {
        get => sfxVolume;
        set => sfxVolume = Mathf.Clamp01(value);
    }

    public float UiVolume
    {
        get => uiVolume;
        set => uiVolume = Mathf.Clamp01(value);
    }

    public bool Fullscreen
    {
        get => fullscreen;
        set => fullscreen = value;
    }

    public float MouseSensitivity
    {
        get => mouseSensitivity;
        set => mouseSensitivity = Mathf.Max(0.1f, value);
    }

    public SettingsSaveData Clone()
    {
        return new SettingsSaveData
        {
            MasterVolume = MasterVolume,
            MusicVolume = MusicVolume,
            SfxVolume = SfxVolume,
            UiVolume = UiVolume,
            Fullscreen = Fullscreen,
            MouseSensitivity = MouseSensitivity
        };
    }
}
