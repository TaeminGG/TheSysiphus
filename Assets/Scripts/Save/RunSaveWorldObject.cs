using UnityEngine;

[DisallowMultipleComponent]
public sealed class RunSaveWorldObject : MonoBehaviour
{
    [SerializeField] private RunSaveIdentity identity;
    [SerializeField] private string stateKey = string.Empty;
    [SerializeField] private bool consumed;
    [SerializeField] private bool deactivateWhenConsumed = true;

    public string SaveId => ResolveIdentity().SaveId;
    public bool Consumed => consumed;

    public void MarkConsumed()
    {
        consumed = true;
        ApplyConsumedVisualState();
    }

    public void MarkAvailable()
    {
        consumed = false;
        ApplyConsumedVisualState();
    }

    public WorldObjectRunSaveData Capture()
    {
        RunSaveIdentity resolvedIdentity = ResolveIdentity();
        return new WorldObjectRunSaveData
        {
            saveId = resolvedIdentity.SaveId,
            stateKey = string.IsNullOrWhiteSpace(stateKey) ? gameObject.name : stateKey,
            position = transform.position,
            consumed = consumed
        };
    }

    public void Restore(WorldObjectRunSaveData data)
    {
        if (data == null)
            return;

        transform.position = data.position;
        consumed = data.consumed;
        ApplyConsumedVisualState();
    }

    private RunSaveIdentity ResolveIdentity()
    {
        if (identity == null)
            identity = GetComponent<RunSaveIdentity>();

        if (identity == null)
            identity = gameObject.AddComponent<RunSaveIdentity>();

        return identity;
    }

    private void ApplyConsumedVisualState()
    {
        if (deactivateWhenConsumed)
            gameObject.SetActive(!consumed);
    }
}
