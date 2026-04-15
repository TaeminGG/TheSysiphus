using NUnit.Framework;
using UnityEngine;

public class ChunkAndRestrictionZonePlayModeTests
{
    [Test]
    public void Scaffold_IsConfigured_ForPlayModeChunkTests()
    {
        Assert.Pass("PlayMode chunk/restriction-zone test scaffold is ready.");
    }

    [Test]
    [Ignore("TODO: Build a test tilemap setup and verify ChunkLoader copies chunk tiles into the scene tilemaps.")]
    public void ChunkLoader_CopiesTiles_ToSceneTilemap()
    {
    }

    [Test]
    [Ignore("TODO: Build a path continuity fixture and verify chunk generation preserves a playable route.")]
    public void ChunkGeneration_PreservesPlayablePath()
    {
    }

    [Test]
    public void RestrictionZone_MovesDuringPlay()
    {
        GameObject rockObject = new GameObject("RestrictionRock");

        try
        {
            rockObject.transform.position = Vector3.zero;
            RockMovement rockMovement = rockObject.AddComponent<RockMovement>();
            rockMovement.SetBaseSpeed(4f);
            rockMovement.SetTargetY(-10f, true);

            float startY = rockObject.transform.position.y;

            rockMovement.Tick(0.25f);

            Assert.That(rockObject.transform.position.y, Is.LessThan(startY));
            Assert.That(rockMovement.Progress01, Is.GreaterThan(0f));
        }
        finally
        {
            Object.DestroyImmediate(rockObject);
        }
    }

    [Test]
    public void RestrictionZone_SpeedChanges_WhenConditionChanges()
    {
        GameObject rockObject = new GameObject("RestrictionRockSpeed");

        try
        {
            rockObject.transform.position = new Vector3(0f, 10f, 0f);
            RockMovement rockMovement = rockObject.AddComponent<RockMovement>();
            rockMovement.SetBaseSpeed(8f);

            Assert.That(rockMovement.CurrentMoveSpeed, Is.EqualTo(8f).Within(0.0001f));

            int slowToken = rockMovement.AddSpeedMultiplier(0.5f);
            Assert.That(rockMovement.CurrentMoveSpeed, Is.EqualTo(4f).Within(0.0001f));

            rockMovement.StopMovement();
            Assert.That(rockMovement.CurrentMoveSpeed, Is.EqualTo(0f).Within(0.0001f));

            rockMovement.ResumeMovement();
            Assert.That(rockMovement.CurrentMoveSpeed, Is.EqualTo(4f).Within(0.0001f));

            int stopToken = rockMovement.StopUntilReleased();
            Assert.That(rockMovement.CurrentMoveSpeed, Is.EqualTo(0f).Within(0.0001f));

            Assert.That(rockMovement.RemoveSpeedModifier(stopToken), Is.True);
            Assert.That(rockMovement.RemoveSpeedModifier(slowToken), Is.True);
            Assert.That(rockMovement.CurrentMoveSpeed, Is.EqualTo(8f).Within(0.0001f));
        }
        finally
        {
            Object.DestroyImmediate(rockObject);
        }
    }

    [Test]
    public void RestrictionZone_ReportsProgress_AgainstTargetY()
    {
        GameObject rockObject = new GameObject("RestrictionRockProgress");

        try
        {
            rockObject.transform.position = new Vector3(0f, 10f, 0f);
            RockMovement rockMovement = rockObject.AddComponent<RockMovement>();
            rockMovement.SetTargetY(0f, true);

            rockObject.transform.position = new Vector3(0f, 5f, 0f);

            Assert.That(rockMovement.Progress01, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(rockMovement.TravelledDistance, Is.EqualTo(5f).Within(0.0001f));
            Assert.That(rockMovement.RemainingDistanceToTarget, Is.EqualTo(5f).Within(0.0001f));
        }
        finally
        {
            Object.DestroyImmediate(rockObject);
        }
    }

    [Test]
    public void RestrictionZone_TimedSpeedModifier_Expires()
    {
        GameObject rockObject = new GameObject("RestrictionRockTimedSpeed");

        try
        {
            RockMovement rockMovement = rockObject.AddComponent<RockMovement>();
            rockMovement.SetBaseSpeed(6f);
            rockMovement.AddTimedSpeedMultiplier(0.5f, 0.05f);

            Assert.That(rockMovement.CurrentMoveSpeed, Is.EqualTo(3f).Within(0.0001f));

            rockMovement.Tick(0.1f);

            Assert.That(rockMovement.CurrentMoveSpeed, Is.EqualTo(6f).Within(0.0001f));
        }
        finally
        {
            Object.DestroyImmediate(rockObject);
        }
    }
}
