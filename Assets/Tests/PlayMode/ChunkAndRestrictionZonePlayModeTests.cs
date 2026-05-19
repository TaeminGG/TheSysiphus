using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ChunkAndRestrictionZonePlayModeTests
{
    [Test]
    public void Scaffold_IsConfigured_ForPlayModeChunkTests()
    {
        Assert.Pass("PlayMode chunk/restriction-zone test scaffold is ready.");
    }

    [Test]
    public void LevelManager_StartRun_GeneratesChunksFromPlayerYProgress()
    {
        GameObject gridObject = new GameObject("Grid", typeof(Grid));
        GameObject tmapObject = new GameObject("tmap", typeof(Tilemap));
        GameObject bmapObject = new GameObject("bmap", typeof(Tilemap));
        GameObject eventMapObject = new GameObject("eventmap", typeof(Tilemap));
        GameObject levelObject = new GameObject("LevelManager");
        GameObject playerObject = new GameObject("Player");
        GameObject rocksObject = new GameObject("Rocks");
        GameObject tileChunk = new GameObject("TileChunk", typeof(Tilemap));
        GameObject boxChunk = new GameObject("BoxChunk", typeof(Tilemap));
        GameObject spawnChunk = new GameObject("SpawnChunk", typeof(Tilemap));

        try
        {
            tmapObject.transform.SetParent(gridObject.transform);
            bmapObject.transform.SetParent(gridObject.transform);
            eventMapObject.transform.SetParent(gridObject.transform);

            RockMovement rockMovement = rocksObject.AddComponent<RockMovement>();
            rocksObject.SetActive(false);

            ChunkLoader chunkLoader = levelObject.AddComponent<ChunkLoader>();
            chunkLoader.tmap = tmapObject.GetComponent<Tilemap>();
            chunkLoader.bmap = bmapObject.GetComponent<Tilemap>();
            chunkLoader.eventMap = eventMapObject.GetComponent<Tilemap>();
            chunkLoader.tileChunkPrefabs = new[] { tileChunk };
            chunkLoader.boxChunkPrefabs = new[] { boxChunk };
            chunkLoader.spawnChunkPrefabs = new[] { spawnChunk };

            LevelManager levelManager = levelObject.AddComponent<LevelManager>();
            levelManager.player = playerObject.transform;
            levelManager.rocksObject = rocksObject;
            levelManager.rockMovement = rockMovement;
            levelManager.startingChunkCount = 0;
            levelManager.currentLevel = 0;
            levelManager.chunkHeight = 6;
            levelManager.chunkWorldHeight = 3f;

            Assert.That(levelManager.currentLevel, Is.EqualTo(0));
            Assert.That(rocksObject.activeSelf, Is.False);

            levelManager.StartRun();

            Assert.That(levelManager.currentLevel, Is.EqualTo(1));
            Assert.That(rocksObject.activeSelf, Is.True);

            playerObject.transform.position = new Vector3(0f, -2.1f, 0f);
            levelManager.TickChunkGeneration();

            Assert.That(levelManager.currentLevel, Is.EqualTo(2));
        }
        finally
        {
            Object.DestroyImmediate(spawnChunk);
            Object.DestroyImmediate(boxChunk);
            Object.DestroyImmediate(tileChunk);
            Object.DestroyImmediate(rocksObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(levelObject);
            Object.DestroyImmediate(eventMapObject);
            Object.DestroyImmediate(bmapObject);
            Object.DestroyImmediate(tmapObject);
            Object.DestroyImmediate(gridObject);
        }
    }

    [Test]
    public void LevelManager_LoadsRegisteredBossRoomChunk_AndStopsFurtherChunkGeneration()
    {
        GameObject gridObject = new GameObject("Grid", typeof(Grid));
        GameObject tmapObject = new GameObject("tmap", typeof(Tilemap));
        GameObject bmapObject = new GameObject("bmap", typeof(Tilemap));
        GameObject eventMapObject = new GameObject("eventmap", typeof(Tilemap));
        GameObject levelObject = new GameObject("LevelManager");
        GameObject playerObject = new GameObject("Player");
        GameObject rocksObject = new GameObject("Rocks");
        GameObject tileChunkA = new GameObject("TileChunkA", typeof(Tilemap));
        GameObject tileChunkBoss = new GameObject("TileChunkBoss", typeof(Tilemap));
        GameObject boxChunkA = new GameObject("BoxChunkA", typeof(Tilemap));
        GameObject boxChunkBoss = new GameObject("BoxChunkBoss", typeof(Tilemap));
        GameObject spawnChunkA = new GameObject("SpawnChunkA", typeof(Tilemap));
        GameObject spawnChunkBoss = new GameObject("SpawnChunkBoss", typeof(Tilemap));

        try
        {
            tmapObject.transform.SetParent(gridObject.transform);
            bmapObject.transform.SetParent(gridObject.transform);
            eventMapObject.transform.SetParent(gridObject.transform);

            ChunkLoader chunkLoader = levelObject.AddComponent<ChunkLoader>();
            chunkLoader.tmap = tmapObject.GetComponent<Tilemap>();
            chunkLoader.bmap = bmapObject.GetComponent<Tilemap>();
            chunkLoader.eventMap = eventMapObject.GetComponent<Tilemap>();
            chunkLoader.tileChunkPrefabs = new[] { tileChunkA, tileChunkBoss };
            chunkLoader.boxChunkPrefabs = new[] { boxChunkA, boxChunkBoss };
            chunkLoader.spawnChunkPrefabs = new[] { spawnChunkA, spawnChunkBoss };

            LevelManager levelManager = levelObject.AddComponent<LevelManager>();
            levelManager.player = playerObject.transform;
            levelManager.rocksObject = rocksObject;
            levelManager.rockMovement = rocksObject.AddComponent<RockMovement>();
            levelManager.startingChunkCount = 0;
            levelManager.currentLevel = 0;
            levelManager.chunkHeight = 6;
            levelManager.chunkWorldHeight = 3f;
            levelManager.randomizeChunksAfterFirst = false;
            levelManager.bossRoomChunks.Add(new BossRoomChunkDefinition
            {
                chunkNumber = 2,
                chunkIndex = 1,
                stopChunkGenerationAfterLoaded = true,
                roomId = "test-boss"
            });

            levelManager.StartRun();

            Assert.That(levelManager.StageGoalY, Is.EqualTo(-3f).Within(0.0001f));
            Assert.That(levelManager.rockMovement.TargetY, Is.EqualTo(-3f).Within(0.0001f));

            playerObject.transform.position = new Vector3(0f, -2.1f, 0f);
            levelManager.TickChunkGeneration();

            Assert.That(levelManager.HasLoadedBossRoom, Is.True);
            Assert.That(levelManager.LoadedBossRoomNumber, Is.EqualTo(2));
            Assert.That(levelManager.LoadedChunks.Count, Is.EqualTo(2));
            Assert.That(levelManager.LoadedChunks[1].chunkIndex, Is.EqualTo(1));
            Assert.That(levelManager.LoadedChunks[1].isBossRoom, Is.True);

            playerObject.transform.position = new Vector3(0f, -20f, 0f);
            levelManager.TickChunkGeneration();

            Assert.That(levelManager.LoadedChunks.Count, Is.EqualTo(2));
        }
        finally
        {
            Object.DestroyImmediate(spawnChunkBoss);
            Object.DestroyImmediate(spawnChunkA);
            Object.DestroyImmediate(boxChunkBoss);
            Object.DestroyImmediate(boxChunkA);
            Object.DestroyImmediate(tileChunkBoss);
            Object.DestroyImmediate(tileChunkA);
            Object.DestroyImmediate(rocksObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(levelObject);
            Object.DestroyImmediate(eventMapObject);
            Object.DestroyImmediate(bmapObject);
            Object.DestroyImmediate(tmapObject);
            Object.DestroyImmediate(gridObject);
        }
    }

    [Test]
    public void LevelManager_RandomChunkGeneration_ExcludesRegisteredBossRoomChunkIndex()
    {
        GameObject gridObject = new GameObject("Grid", typeof(Grid));
        GameObject tmapObject = new GameObject("tmap", typeof(Tilemap));
        GameObject bmapObject = new GameObject("bmap", typeof(Tilemap));
        GameObject eventMapObject = new GameObject("eventmap", typeof(Tilemap));
        GameObject levelObject = new GameObject("LevelManager");
        GameObject playerObject = new GameObject("Player");
        GameObject rocksObject = new GameObject("Rocks");
        GameObject tileChunkA = new GameObject("TileChunkA", typeof(Tilemap));
        GameObject tileChunkBoss = new GameObject("TileChunkBoss", typeof(Tilemap));
        GameObject boxChunkA = new GameObject("BoxChunkA", typeof(Tilemap));
        GameObject boxChunkBoss = new GameObject("BoxChunkBoss", typeof(Tilemap));
        GameObject spawnChunkA = new GameObject("SpawnChunkA", typeof(Tilemap));
        GameObject spawnChunkBoss = new GameObject("SpawnChunkBoss", typeof(Tilemap));

        try
        {
            tmapObject.transform.SetParent(gridObject.transform);
            bmapObject.transform.SetParent(gridObject.transform);
            eventMapObject.transform.SetParent(gridObject.transform);

            ChunkLoader chunkLoader = levelObject.AddComponent<ChunkLoader>();
            chunkLoader.tmap = tmapObject.GetComponent<Tilemap>();
            chunkLoader.bmap = bmapObject.GetComponent<Tilemap>();
            chunkLoader.eventMap = eventMapObject.GetComponent<Tilemap>();
            chunkLoader.tileChunkPrefabs = new[] { tileChunkA, tileChunkBoss };
            chunkLoader.boxChunkPrefabs = new[] { boxChunkA, boxChunkBoss };
            chunkLoader.spawnChunkPrefabs = new[] { spawnChunkA, spawnChunkBoss };

            LevelManager levelManager = levelObject.AddComponent<LevelManager>();
            levelManager.player = playerObject.transform;
            levelManager.rocksObject = rocksObject;
            levelManager.rockMovement = rocksObject.AddComponent<RockMovement>();
            levelManager.startingChunkCount = 0;
            levelManager.currentLevel = 0;
            levelManager.chunkWorldHeight = 25f;
            levelManager.nextChunkTriggerRatio = 0.1f;
            levelManager.randomizeChunksAfterFirst = true;
            levelManager.bossRoomChunks.Add(new BossRoomChunkDefinition
            {
                chunkNumber = 3,
                chunkIndex = 1,
                stopChunkGenerationAfterLoaded = true,
                roomId = "test-boss"
            });

            levelManager.StartRun();

            playerObject.transform.position = new Vector3(0f, -3f, 0f);
            levelManager.TickChunkGeneration();

            Assert.That(levelManager.LoadedChunks.Count, Is.EqualTo(2));
            Assert.That(levelManager.LoadedChunks[1].chunkIndex, Is.EqualTo(0));
            Assert.That(levelManager.LoadedChunks[1].isBossRoom, Is.False);

            playerObject.transform.position = new Vector3(0f, -28f, 0f);
            levelManager.TickChunkGeneration();

            Assert.That(levelManager.LoadedChunks.Count, Is.EqualTo(3));
            Assert.That(levelManager.LoadedChunks[2].chunkIndex, Is.EqualTo(1));
            Assert.That(levelManager.LoadedChunks[2].isBossRoom, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(spawnChunkBoss);
            Object.DestroyImmediate(spawnChunkA);
            Object.DestroyImmediate(boxChunkBoss);
            Object.DestroyImmediate(boxChunkA);
            Object.DestroyImmediate(tileChunkBoss);
            Object.DestroyImmediate(tileChunkA);
            Object.DestroyImmediate(rocksObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(levelObject);
            Object.DestroyImmediate(eventMapObject);
            Object.DestroyImmediate(bmapObject);
            Object.DestroyImmediate(tmapObject);
            Object.DestroyImmediate(gridObject);
        }
    }

    [Test]
    public void BossRoomController_SpawnsBossControlsRocks_AndCompletesStageOnBossDeath()
    {
        GameObject bossRoomObject = new GameObject("BossRoom");
        GameObject bossPrefab = new GameObject("BossPrefab");
        GameObject rocksObject = new GameObject("Rocks");
        GameObject clearObject = new GameObject("StageClear");
        GameObject spawnedBoss = null;

        try
        {
            EnemyStats prefabStats = bossPrefab.AddComponent<EnemyStats>();
            prefabStats.enabled = false;

            RockMovement rockMovement = rocksObject.AddComponent<RockMovement>();
            rockMovement.SetBaseSpeed(6f);

            StageClearController stageClearController = clearObject.AddComponent<StageClearController>();
            BossRoomController bossRoom = bossRoomObject.AddComponent<BossRoomController>();
            bossRoom.bossPrefab = bossPrefab;
            bossRoom.rockMovement = rockMovement;
            bossRoom.stageClearController = stageClearController;
            bossRoom.rocksResumeDelay = 0f;
            bossRoom.resumedRockSpeed = 3f;

            bossRoom.EnterBossRoom();
            spawnedBoss = bossRoom.ActiveBoss;

            Assert.That(bossRoom.HasEntered, Is.True);
            Assert.That(bossRoom.HasSpawnedBoss, Is.True);
            Assert.That(rockMovement.CurrentMoveSpeed, Is.EqualTo(3f).Within(0.0001f));

            EnemyStats bossStats = bossRoom.ActiveBoss.GetComponent<EnemyStats>();
            bossStats.Kill();

            Assert.That(bossRoom.HasCompletedBoss, Is.True);
            Assert.That(stageClearController.IsCleared, Is.True);
        }
        finally
        {
            if (bossRoomObject != null)
                Object.DestroyImmediate(bossRoomObject);

            if (rocksObject != null)
                Object.DestroyImmediate(rocksObject);

            if (clearObject != null)
                Object.DestroyImmediate(clearObject);

            if (spawnedBoss != null)
                Object.DestroyImmediate(spawnedBoss);

            if (bossPrefab != null)
                Object.DestroyImmediate(bossPrefab);
        }
    }

    [Test]
    public void BossRoomController_BossClearScreenPausesWorld_AndSceneTransitionResumeRestoresTime()
    {
        GameObject bossRoomObject = new GameObject("BossRoom");
        GameObject bossPrefab = new GameObject("BossPrefab");
        GameObject canvasObject = new GameObject("Canvas", typeof(Canvas));
        GameObject timeObject = new GameObject("TimeControlService");
        GameObject spawnedBoss = null;

        try
        {
            Time.timeScale = 1f;

            EnemyStats prefabStats = bossPrefab.AddComponent<EnemyStats>();
            prefabStats.enabled = false;

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            TimeControlService timeControlService = timeObject.AddComponent<TimeControlService>();

            BossRoomController bossRoom = bossRoomObject.AddComponent<BossRoomController>();
            bossRoom.bossPrefab = bossPrefab;
            bossRoom.uiCanvas = canvas;
            bossRoom.timeControlService = timeControlService;
            bossRoom.rocksResumeDelay = 0f;
            bossRoom.bossClearUiDelay = 0f;
            bossRoom.bossClearUiFadeDuration = 0f;

            bossRoom.EnterBossRoom();
            spawnedBoss = bossRoom.ActiveBoss;

            EnemyStats bossStats = bossRoom.ActiveBoss.GetComponent<EnemyStats>();
            bossStats.Kill();

            Assert.That(Time.timeScale, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(timeControlService.IsPaused, Is.True);

            MethodInfo resumeMethod = typeof(BossRoomController).GetMethod("ResumeWorldForSceneTransition", BindingFlags.NonPublic | BindingFlags.Instance);
            resumeMethod.Invoke(bossRoom, null);

            Assert.That(Time.timeScale, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(timeControlService.IsPaused, Is.False);
        }
        finally
        {
            Time.timeScale = 1f;

            if (bossRoomObject != null)
                Object.DestroyImmediate(bossRoomObject);

            if (spawnedBoss != null)
                Object.DestroyImmediate(spawnedBoss);

            if (bossPrefab != null)
                Object.DestroyImmediate(bossPrefab);

            Object.DestroyImmediate(timeObject);
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void BossRoomController_BossDeathUnlocksLegendaryItems_AndPersistsProfile()
    {
        string testDirectory = Path.Combine(Application.temporaryCachePath, "BossLegendaryUnlockSaveTest");
        GameObject registryObject = new GameObject("LegendaryUnlockRegistry");
        GameObject saveManagerObject = new GameObject("SaveManagerTest");
        GameObject bossRoomObject = new GameObject("BossRoom");
        GameObject bossPrefab = new GameObject("BossPrefab");
        ItemUnlockCatalog catalog = ScriptableObject.CreateInstance<ItemUnlockCatalog>();
        TestPassiveItem legendaryPassive = ScriptableObject.CreateInstance<TestPassiveItem>();
        TestActiveItem legendaryActive = ScriptableObject.CreateInstance<TestActiveItem>();
        GameObject spawnedBoss = null;

        try
        {
            if (Directory.Exists(testDirectory))
                Directory.Delete(testDirectory, true);

            legendaryPassive.name = "BossClearLegendaryPassive";
            legendaryActive.name = "BossClearLegendaryActive";
            SetPrivateField(legendaryPassive, "rarity", ItemRarity.Legendary);
            SetPrivateField(legendaryActive, "rarity", ItemRarity.Legendary);
            SetPrivateField(catalog, "passiveItems", new List<PassiveItem> { legendaryPassive });
            SetPrivateField(catalog, "activeItems", new List<ActiveItem> { legendaryActive });

            SaveManager saveManager = saveManagerObject.AddComponent<SaveManager>();
            saveManager.ConfigureRepositoryForTests(new GameSaveRepository(testDirectory));

            ItemUnlockRegistry registry = registryObject.AddComponent<ItemUnlockRegistry>();
            SetPrivateField(registry, "catalog", catalog);
            registry.ResetToInitialState();

            EnemyStats prefabStats = bossPrefab.AddComponent<EnemyStats>();
            prefabStats.enabled = false;

            BossRoomController bossRoom = bossRoomObject.AddComponent<BossRoomController>();
            bossRoom.bossPrefab = bossPrefab;
            bossRoom.clearStageWhenBossDies = false;
            bossRoom.showClearScreenWhenBossDies = false;

            bossRoom.EnterBossRoom();
            spawnedBoss = bossRoom.ActiveBoss;
            bossRoom.ActiveBoss.GetComponent<EnemyStats>().Kill();

            ProfileSaveData loadedProfile = new GameSaveRepository(testDirectory).LoadProfile();

            Assert.That(registry.IsUnlocked(legendaryPassive), Is.True);
            Assert.That(registry.IsUnlocked(legendaryActive), Is.True);
            Assert.That(loadedProfile.ItemUnlockSnapshot.UnlockedPassiveItemIds, Does.Contain(legendaryPassive.ItemId));
            Assert.That(loadedProfile.ItemUnlockSnapshot.UnlockedActiveItemIds, Does.Contain(legendaryActive.ItemId));
        }
        finally
        {
            if (bossRoomObject != null)
                Object.DestroyImmediate(bossRoomObject);

            if (spawnedBoss != null)
                Object.DestroyImmediate(spawnedBoss);

            if (bossPrefab != null)
                Object.DestroyImmediate(bossPrefab);

            Object.DestroyImmediate(saveManagerObject);
            Object.DestroyImmediate(registryObject);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(legendaryPassive);
            Object.DestroyImmediate(legendaryActive);

            if (Directory.Exists(testDirectory))
                Directory.Delete(testDirectory, true);
        }
    }

    [Test]
    public void BossRoomController_KeepsRocksStoppedUntilResumeDelay_ThenAppliesConfiguredSpeed()
    {
        GameObject bossRoomObject = new GameObject("BossRoom");
        GameObject rocksObject = new GameObject("Rocks");

        try
        {
            RockMovement rockMovement = rocksObject.AddComponent<RockMovement>();
            rockMovement.SetBaseSpeed(6f);

            BossRoomController bossRoom = bossRoomObject.AddComponent<BossRoomController>();
            bossRoom.rockMovement = rockMovement;
            bossRoom.spawnBossOnEnter = false;
            bossRoom.rocksResumeDelay = 0.05f;
            bossRoom.resumedRockSpeed = 3f;

            bossRoom.EnterBossRoom();

            Assert.That(rockMovement.CurrentMoveSpeed, Is.EqualTo(0f).Within(0.0001f));

            MethodInfo resumeMethod = typeof(BossRoomController).GetMethod("ResumeRocksAfterDelay", BindingFlags.NonPublic | BindingFlags.Instance);
            IEnumerator resumeRoutine = (IEnumerator)resumeMethod.Invoke(bossRoom, null);

            Assert.That(resumeRoutine.MoveNext(), Is.True);
            Assert.That(rockMovement.CurrentMoveSpeed, Is.EqualTo(0f).Within(0.0001f));

            Assert.That(resumeRoutine.MoveNext(), Is.False);
            Assert.That(rockMovement.CurrentMoveSpeed, Is.EqualTo(3f).Within(0.0001f));
        }
        finally
        {
            Object.DestroyImmediate(bossRoomObject);
            Object.DestroyImmediate(rocksObject);
        }
    }

    [Test]
    public void ChunkLoader_SpawnsBossRoomController_FromRegisteredSpawnMarker()
    {
        GameObject gridObject = new GameObject("Grid", typeof(Grid));
        GameObject tmapObject = new GameObject("tmap", typeof(Tilemap));
        GameObject bmapObject = new GameObject("bmap", typeof(Tilemap));
        GameObject eventMapObject = new GameObject("eventmap", typeof(Tilemap));
        GameObject levelObject = new GameObject("Level");
        GameObject tileChunk = new GameObject("TileChunk", typeof(Tilemap));
        GameObject boxChunk = new GameObject("BoxChunk", typeof(Tilemap));
        GameObject spawnChunk = new GameObject("SpawnChunk", typeof(Tilemap));
        GameObject bossRoomPrefab = new GameObject("BossRoomPrefab");
        Tile markerTile = ScriptableObject.CreateInstance<Tile>();

        try
        {
            tmapObject.transform.SetParent(gridObject.transform);
            bmapObject.transform.SetParent(gridObject.transform);
            eventMapObject.transform.SetParent(gridObject.transform);

            bossRoomPrefab.AddComponent<BossRoomController>();
            markerTile.name = "BossRoomMarker";
            spawnChunk.GetComponent<Tilemap>().SetTile(Vector3Int.zero, markerTile);

            ChunkLoader chunkLoader = levelObject.AddComponent<ChunkLoader>();
            chunkLoader.tmap = tmapObject.GetComponent<Tilemap>();
            chunkLoader.bmap = bmapObject.GetComponent<Tilemap>();
            chunkLoader.eventMap = eventMapObject.GetComponent<Tilemap>();
            chunkLoader.tileChunkPrefabs = new[] { tileChunk };
            chunkLoader.boxChunkPrefabs = new[] { boxChunk };
            chunkLoader.spawnChunkPrefabs = new[] { spawnChunk };
            chunkLoader.spawnMarkerBindings.Add(new ChunkSpawnMarkerBinding
            {
                markerTile = markerTile,
                prefab = bossRoomPrefab
            });

            chunkLoader.AddChunk(0, 50, 0, false);

            BossRoomController spawnedBossRoom = levelObject.GetComponentInChildren<BossRoomController>();

            Assert.That(spawnedBossRoom, Is.Not.Null);
            Assert.That(spawnedBossRoom.gameObject.name, Does.StartWith("BossRoomPrefab"));
        }
        finally
        {
            Object.DestroyImmediate(markerTile);
            Object.DestroyImmediate(bossRoomPrefab);
            Object.DestroyImmediate(spawnChunk);
            Object.DestroyImmediate(boxChunk);
            Object.DestroyImmediate(tileChunk);
            Object.DestroyImmediate(levelObject);
            Object.DestroyImmediate(eventMapObject);
            Object.DestroyImmediate(bmapObject);
            Object.DestroyImmediate(tmapObject);
            Object.DestroyImmediate(gridObject);
        }
    }

    [Test]
    public void LevelManager_CapturesAndRestoresGeneratedChunkHistory()
    {
        GameObject gridObject = new GameObject("Grid", typeof(Grid));
        GameObject tmapObject = new GameObject("tmap", typeof(Tilemap));
        GameObject bmapObject = new GameObject("bmap", typeof(Tilemap));
        GameObject eventMapObject = new GameObject("eventmap", typeof(Tilemap));
        GameObject levelObject = new GameObject("LevelManager");
        GameObject restoredLevelObject = new GameObject("RestoredLevelManager");
        GameObject playerObject = new GameObject("Player");
        GameObject rocksObject = new GameObject("Rocks");
        GameObject restoredRocksObject = new GameObject("RestoredRocks");
        GameObject tileChunk = new GameObject("TileChunk", typeof(Tilemap));
        GameObject boxChunk = new GameObject("BoxChunk", typeof(Tilemap));
        GameObject spawnChunk = new GameObject("SpawnChunk", typeof(Tilemap));

        try
        {
            tmapObject.transform.SetParent(gridObject.transform);
            bmapObject.transform.SetParent(gridObject.transform);
            eventMapObject.transform.SetParent(gridObject.transform);

            ChunkLoader chunkLoader = levelObject.AddComponent<ChunkLoader>();
            chunkLoader.tmap = tmapObject.GetComponent<Tilemap>();
            chunkLoader.bmap = bmapObject.GetComponent<Tilemap>();
            chunkLoader.eventMap = eventMapObject.GetComponent<Tilemap>();
            chunkLoader.tileChunkPrefabs = new[] { tileChunk };
            chunkLoader.boxChunkPrefabs = new[] { boxChunk };
            chunkLoader.spawnChunkPrefabs = new[] { spawnChunk };

            LevelManager levelManager = levelObject.AddComponent<LevelManager>();
            levelManager.player = playerObject.transform;
            levelManager.rocksObject = rocksObject;
            levelManager.rockMovement = rocksObject.AddComponent<RockMovement>();
            levelManager.startingChunkCount = 0;
            levelManager.currentLevel = 0;
            levelManager.chunkHeight = 6;
            levelManager.chunkWorldHeight = 3f;

            levelManager.StartRun();
            playerObject.transform.position = new Vector3(0f, -2.1f, 0f);
            levelManager.TickChunkGeneration();

            LevelRunSaveData captured = levelManager.CaptureRunState();
            Assert.That(captured.loadedChunks.Count, Is.EqualTo(2));

            ChunkLoader restoredChunkLoader = restoredLevelObject.AddComponent<ChunkLoader>();
            restoredChunkLoader.tmap = tmapObject.GetComponent<Tilemap>();
            restoredChunkLoader.bmap = bmapObject.GetComponent<Tilemap>();
            restoredChunkLoader.eventMap = eventMapObject.GetComponent<Tilemap>();
            restoredChunkLoader.tileChunkPrefabs = new[] { tileChunk };
            restoredChunkLoader.boxChunkPrefabs = new[] { boxChunk };
            restoredChunkLoader.spawnChunkPrefabs = new[] { spawnChunk };

            LevelManager restoredLevelManager = restoredLevelObject.AddComponent<LevelManager>();
            restoredLevelManager.player = playerObject.transform;
            restoredLevelManager.rocksObject = restoredRocksObject;
            restoredLevelManager.rockMovement = restoredRocksObject.AddComponent<RockMovement>();
            restoredLevelManager.startingChunkCount = 0;
            restoredLevelManager.currentLevel = 0;

            restoredLevelManager.RestoreRunState(captured);

            Assert.That(restoredLevelManager.LoadedChunks.Count, Is.EqualTo(2));
            Assert.That(restoredLevelManager.currentLevel, Is.EqualTo(captured.currentLevel));
            Assert.That(restoredRocksObject.activeSelf, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(spawnChunk);
            Object.DestroyImmediate(boxChunk);
            Object.DestroyImmediate(tileChunk);
            Object.DestroyImmediate(restoredRocksObject);
            Object.DestroyImmediate(rocksObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(restoredLevelObject);
            Object.DestroyImmediate(levelObject);
            Object.DestroyImmediate(eventMapObject);
            Object.DestroyImmediate(bmapObject);
            Object.DestroyImmediate(tmapObject);
            Object.DestroyImmediate(gridObject);
        }
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

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = null;
        System.Type type = target.GetType();
        while (type != null && field == null)
        {
            field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            type = type.BaseType;
        }

        Assert.That(field, Is.Not.Null, $"Could not find field '{fieldName}' on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private class TestPassiveItem : PassiveItem
    {
        public override void OnEquip(PlayerStats stats)
        {
        }

        public override void OnUnequip(PlayerStats stats)
        {
        }
    }

    private class TestActiveItem : ActiveItem
    {
        public override void OnUsed()
        {
        }
    }
}
