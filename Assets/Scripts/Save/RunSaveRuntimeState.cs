using System.Collections.Generic;

public static class RunSaveRuntimeState
{
    private static readonly HashSet<string> deadEnemyIds = new HashSet<string>();
    private static readonly HashSet<string> consumedPickupIds = new HashSet<string>();

    public static IReadOnlyCollection<string> DeadEnemyIds => deadEnemyIds;
    public static IReadOnlyCollection<string> ConsumedPickupIds => consumedPickupIds;

    public static void MarkEnemyDead(string saveId)
    {
        if (!string.IsNullOrWhiteSpace(saveId))
            deadEnemyIds.Add(saveId);
    }

    public static void MarkPickupConsumed(string saveId)
    {
        if (!string.IsNullOrWhiteSpace(saveId))
            consumedPickupIds.Add(saveId);
    }

    public static void RestoreFromRun(RunSaveData runData)
    {
        ClearRunState();

        if (runData == null)
            return;

        if (runData.Enemies != null)
        {
            for (int i = 0; i < runData.Enemies.Count; i++)
            {
                EnemyRunSaveData enemy = runData.Enemies[i];
                if (enemy != null && enemy.isDead)
                    MarkEnemyDead(enemy.saveId);
            }
        }

        if (runData.Pickups != null)
        {
            for (int i = 0; i < runData.Pickups.Count; i++)
            {
                PickupRunSaveData pickup = runData.Pickups[i];
                if (pickup != null && pickup.consumed)
                    MarkPickupConsumed(pickup.saveId);
            }
        }
    }

    public static void ClearRunState()
    {
        deadEnemyIds.Clear();
        consumedPickupIds.Clear();
    }
}
