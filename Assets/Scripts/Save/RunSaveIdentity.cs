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

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(saveId))
            saveId = Guid.NewGuid().ToString("N");
    }
#endif
}
