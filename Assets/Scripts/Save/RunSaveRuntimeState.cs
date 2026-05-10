using System.Collections.Generic;

public static class RunSaveRuntimeState
{
    private static readonly HashSet<string> deadEnemyIds = new HashSet<string>();

    public static IReadOnlyCollection<string> DeadEnemyIds => deadEnemyIds;

    public static void MarkEnemyDead(string saveId)
    {
        if (!string.IsNullOrWhiteSpace(saveId))
            deadEnemyIds.Add(saveId);
    }

    public static void ClearRunState()
    {
        deadEnemyIds.Clear();
    }
}
