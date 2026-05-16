using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RunSaveIdentity : MonoBehaviour
{
    [SerializeField] private string saveId = string.Empty;
    [SerializeField] private string prefabId = string.Empty;

    public string SaveId
    {
        get
        {
            if (string.IsNullOrWhiteSpace(saveId))
                saveId = Guid.NewGuid().ToString("N");

            return saveId;
        }
        set => saveId = value ?? string.Empty;
    }

    public string PrefabId
    {
        get => string.IsNullOrWhiteSpace(prefabId) ? gameObject.name : prefabId;
        set => prefabId = value ?? string.Empty;
    }

    public void Configure(string newSaveId, string newPrefabId = null)
    {
        saveId = newSaveId ?? string.Empty;
        if (newPrefabId != null)
            prefabId = newPrefabId;
    }

    public static RunSaveIdentity Ensure(GameObject target, string newSaveId = null, string newPrefabId = null)
    {
        if (target == null)
            return null;

        RunSaveIdentity identity = target.GetComponent<RunSaveIdentity>();
        if (identity == null)
            identity = target.AddComponent<RunSaveIdentity>();

        if (!string.IsNullOrWhiteSpace(newSaveId) || newPrefabId != null)
            identity.Configure(newSaveId ?? identity.SaveId, newPrefabId);

        return identity;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(saveId))
            saveId = Guid.NewGuid().ToString("N");
    }
#endif
}
