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

    public void MarkConsumedWithoutApplyingVisualState()
    {
        consumed = true;
    }

    public void MarkAvailable()
    {
        consumed = false;
        ApplyConsumedVisualState();
    }

    public WorldObjectRunSaveData Capture()
    {
        RunSaveIdentity resolvedIdentity = ResolveIdentity();
        IRunSaveWorldStateHandler stateHandler = ResolveStateHandler();
        return new WorldObjectRunSaveData
        {
            saveId = resolvedIdentity.SaveId,
            stateKey = string.IsNullOrWhiteSpace(stateKey) ? gameObject.name : stateKey,
            position = transform.position,
            consumed = consumed,
            payloadJson = stateHandler != null ? stateHandler.CaptureRunSavePayload() : string.Empty
        };
    }

    public void Restore(WorldObjectRunSaveData data)
    {
        if (data == null)
            return;

        transform.position = data.position;
        consumed = data.consumed;
        IRunSaveWorldStateHandler stateHandler = ResolveStateHandler();
        if (stateHandler != null)
            stateHandler.RestoreRunSaveState(data);

        if (stateHandler == null || !stateHandler.SuppressDefaultConsumedState)
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

    private IRunSaveWorldStateHandler ResolveStateHandler()
    {
        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IRunSaveWorldStateHandler handler)
                return handler;
        }

        return null;
    }

    private void ApplyConsumedVisualState()
    {
        if (deactivateWhenConsumed)
            gameObject.SetActive(!consumed);
    }
}
