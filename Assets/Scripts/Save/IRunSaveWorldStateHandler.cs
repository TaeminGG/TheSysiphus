public interface IRunSaveWorldStateHandler
{
    bool SuppressDefaultConsumedState { get; }
    string CaptureRunSavePayload();
    void RestoreRunSaveState(WorldObjectRunSaveData data);
}
