using System;
using System.IO;
using UnityEngine;

public sealed class GameSaveRepository
{
    private const string DefaultProfileFileName = "profile-save.json";
    private const string DefaultRunFileName = "run-save.json";

    private readonly string saveDirectory;
    private readonly string profileFileName;
    private readonly string runFileName;

    public GameSaveRepository(string saveDirectory, string profileFileName = DefaultProfileFileName, string runFileName = DefaultRunFileName)
    {
        if (string.IsNullOrWhiteSpace(saveDirectory))
            throw new ArgumentException("Save directory must be provided.", nameof(saveDirectory));

        this.saveDirectory = saveDirectory;
        this.profileFileName = string.IsNullOrWhiteSpace(profileFileName) ? DefaultProfileFileName : profileFileName;
        this.runFileName = string.IsNullOrWhiteSpace(runFileName) ? DefaultRunFileName : runFileName;
    }

    public string SaveDirectory => saveDirectory;
    public string ProfilePath => Path.Combine(saveDirectory, profileFileName);
    public string RunPath => Path.Combine(saveDirectory, runFileName);
    public bool HasActiveRun => TryLoadRun(out RunSaveData runData) && runData.ActiveRun;

    public ProfileSaveData LoadProfile()
    {
        if (!File.Exists(ProfilePath))
            return new ProfileSaveData();

        try
        {
            ProfileSaveData data = JsonUtility.FromJson<ProfileSaveData>(File.ReadAllText(ProfilePath));
            return data ?? new ProfileSaveData();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to load profile save. A fresh profile will be used. {exception.Message}");
            return new ProfileSaveData();
        }
    }

    public void SaveProfile(ProfileSaveData data)
    {
        data ??= new ProfileSaveData();
        data.MarkSavedNow();
        WriteJson(ProfilePath, data);
    }

    public RunSaveData StartNewRun(string sceneName)
    {
        RunSaveData data = RunSaveData.CreateNew(sceneName);
        SaveRun(data);
        return data;
    }

    public bool TryLoadRun(out RunSaveData data)
    {
        data = null;
        if (!File.Exists(RunPath))
            return false;

        try
        {
            data = JsonUtility.FromJson<RunSaveData>(File.ReadAllText(RunPath));
            return data != null && data.ActiveRun;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to load run save. It will be ignored. {exception.Message}");
            return false;
        }
    }

    public void SaveRun(RunSaveData data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        data.MarkSavedNow();
        WriteJson(RunPath, data);
    }

    public void DeleteRun()
    {
        if (File.Exists(RunPath))
            File.Delete(RunPath);
    }

    private void WriteJson(string path, object data)
    {
        Directory.CreateDirectory(saveDirectory);
        File.WriteAllText(path, JsonUtility.ToJson(data, true));
    }
}
