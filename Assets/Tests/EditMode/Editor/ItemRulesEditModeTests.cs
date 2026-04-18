using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class ItemRulesEditModeTests
{
    [Test]
    public void Scaffold_IsConfigured_ForEditModeItemTests()
    {
        Assert.Pass("EditMode item test scaffold is ready.");
    }

    [Test]
    [Ignore("TODO: Extract active item registration logic and verify successful inventory insertion.")]
    public void ActiveInventory_AddsItem_WhenPickupSucceeds()
    {
    }

    [Test]
    [Ignore("TODO: Extract passive item registration logic and verify successful list insertion.")]
    public void PassiveItemList_AddsPassiveItem_WhenPickupSucceeds()
    {
    }

    [Test]
    [Ignore("TODO: Extract passive stat-application logic and verify stat bonuses are applied once equipped.")]
    public void PassiveItem_AppliesStatBonus_WhenEquipped()
    {
    }

    [Test]
    public void ItemUnlockRegistry_TracksInitialAndRuntimeUnlocks()
    {
        GameObject registryObject = new GameObject("UnlockRegistryTest");
        TestPassiveItem initialPassive = ScriptableObject.CreateInstance<TestPassiveItem>();
        TestPassiveItem runtimePassive = ScriptableObject.CreateInstance<TestPassiveItem>();

        try
        {
            initialPassive.name = "InitialPassive";
            runtimePassive.name = "RuntimePassive";

            ItemUnlockRegistry registry = registryObject.AddComponent<ItemUnlockRegistry>();
            SerializedObject serializedRegistry = new SerializedObject(registry);
            SerializedProperty initialPassives = serializedRegistry.FindProperty("initiallyUnlockedPassiveItems");
            initialPassives.arraySize = 1;
            initialPassives.GetArrayElementAtIndex(0).objectReferenceValue = initialPassive;
            serializedRegistry.ApplyModifiedPropertiesWithoutUndo();

            registry.ResetToInitialState();

            Assert.That(registry.IsUnlocked(initialPassive), Is.True);
            Assert.That(registry.IsUnlocked(runtimePassive), Is.False);
            Assert.That(registry.Unlock(runtimePassive), Is.True);
            Assert.That(registry.IsUnlocked(runtimePassive), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(registryObject);
            Object.DestroyImmediate(initialPassive);
            Object.DestroyImmediate(runtimePassive);
        }
    }

    [Test]
    public void ItemSpawnPool_FiltersLockedItems_WhenRegistryExists()
    {
        GameObject registryObject = new GameObject("UnlockRegistryForPoolTest");
        ItemSpawnPool pool = ScriptableObject.CreateInstance<ItemSpawnPool>();
        TestPassiveItem unlockedPassive = ScriptableObject.CreateInstance<TestPassiveItem>();
        TestPassiveItem lockedPassive = ScriptableObject.CreateInstance<TestPassiveItem>();
        TestActiveItem unlockedActive = ScriptableObject.CreateInstance<TestActiveItem>();
        TestActiveItem lockedActive = ScriptableObject.CreateInstance<TestActiveItem>();

        try
        {
            unlockedPassive.name = "UnlockedPassive";
            lockedPassive.name = "LockedPassive";
            unlockedActive.name = "UnlockedActive";
            lockedActive.name = "LockedActive";

            ItemUnlockRegistry registry = registryObject.AddComponent<ItemUnlockRegistry>();
            SerializedObject serializedRegistry = new SerializedObject(registry);
            SerializedProperty initialPassives = serializedRegistry.FindProperty("initiallyUnlockedPassiveItems");
            initialPassives.arraySize = 1;
            initialPassives.GetArrayElementAtIndex(0).objectReferenceValue = unlockedPassive;

            SerializedProperty initialActives = serializedRegistry.FindProperty("initiallyUnlockedActiveItems");
            initialActives.arraySize = 1;
            initialActives.GetArrayElementAtIndex(0).objectReferenceValue = unlockedActive;
            serializedRegistry.ApplyModifiedPropertiesWithoutUndo();
            registry.ResetToInitialState();

            SerializedObject serializedPool = new SerializedObject(pool);
            SerializedProperty passiveItems = serializedPool.FindProperty("passiveItems");
            passiveItems.arraySize = 2;
            passiveItems.GetArrayElementAtIndex(0).FindPropertyRelative("item").objectReferenceValue = unlockedPassive;
            passiveItems.GetArrayElementAtIndex(1).FindPropertyRelative("item").objectReferenceValue = lockedPassive;

            SerializedProperty activeItems = serializedPool.FindProperty("activeItems");
            activeItems.arraySize = 2;
            activeItems.GetArrayElementAtIndex(0).FindPropertyRelative("item").objectReferenceValue = unlockedActive;
            activeItems.GetArrayElementAtIndex(1).FindPropertyRelative("item").objectReferenceValue = lockedActive;
            serializedPool.ApplyModifiedPropertiesWithoutUndo();

            System.Collections.Generic.List<ItemSpawnPool.RollResult> unlockedEntries = pool.GetUnlockedEntries(registry);

            Assert.That(unlockedEntries.Count, Is.EqualTo(2));
            Assert.That(ContainsPassive(unlockedEntries, unlockedPassive), Is.True);
            Assert.That(ContainsPassive(unlockedEntries, lockedPassive), Is.False);
            Assert.That(ContainsActive(unlockedEntries, unlockedActive), Is.True);
            Assert.That(ContainsActive(unlockedEntries, lockedActive), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(registryObject);
            Object.DestroyImmediate(pool);
            Object.DestroyImmediate(unlockedPassive);
            Object.DestroyImmediate(lockedPassive);
            Object.DestroyImmediate(unlockedActive);
            Object.DestroyImmediate(lockedActive);
        }
    }

    [Test]
    public void ItemUnlockRegistry_RestoresCollectedPassiveItems_FromSnapshot()
    {
        GameObject registryObject = new GameObject("CollectedPassiveSnapshotTest");
        TestPassiveItem collectedPassive = ScriptableObject.CreateInstance<TestPassiveItem>();

        try
        {
            collectedPassive.name = "CollectedPassive";

            ItemUnlockRegistry registry = registryObject.AddComponent<ItemUnlockRegistry>();
            registry.ResetToInitialState();
            Assert.That(registry.Collect(collectedPassive), Is.True);

            ItemUnlockSnapshot snapshot = registry.CaptureSnapshot();

            registry.ResetToInitialState();
            Assert.That(registry.IsCollected(collectedPassive), Is.False);

            registry.RestoreSnapshot(snapshot);

            Assert.That(registry.IsUnlocked(collectedPassive), Is.True);
            Assert.That(registry.IsCollected(collectedPassive), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(registryObject);
            Object.DestroyImmediate(collectedPassive);
        }
    }

    [Test]
    public void ItemUnlockRegistry_ReleasesReservedPassiveItems_WhenReservationEnds()
    {
        GameObject registryObject = new GameObject("ReservedPassiveReleaseTest");
        TestPassiveItem reservedPassive = ScriptableObject.CreateInstance<TestPassiveItem>();

        try
        {
            reservedPassive.name = "ReservedPassive";

            ItemUnlockRegistry registry = registryObject.AddComponent<ItemUnlockRegistry>();
            registry.ResetToInitialState();
            Assert.That(registry.TryReservePassive(reservedPassive), Is.True);
            Assert.That(registry.IsReserved(reservedPassive), Is.True);
            Assert.That(registry.IsUnavailable(reservedPassive), Is.True);

            Assert.That(registry.ReleasePassiveReservation(reservedPassive), Is.True);
            Assert.That(registry.IsReserved(reservedPassive), Is.False);
            Assert.That(registry.IsUnavailable(reservedPassive), Is.False);
            Assert.That(registry.TryReservePassive(reservedPassive), Is.True);
            Assert.That(registry.Collect(reservedPassive), Is.True);
            Assert.That(registry.ReleasePassiveReservation(reservedPassive), Is.False);
            Assert.That(registry.TryReservePassive(reservedPassive), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(registryObject);
            Object.DestroyImmediate(reservedPassive);
        }
    }

    [Test]
    public void ItemSpawnPool_FiltersUnavailablePassiveItems_WhenRegistryExists()
    {
        GameObject registryObject = new GameObject("CollectedPassivePoolTest");
        ItemSpawnPool pool = ScriptableObject.CreateInstance<ItemSpawnPool>();
        TestPassiveItem reservedPassive = ScriptableObject.CreateInstance<TestPassiveItem>();
        TestPassiveItem availablePassive = ScriptableObject.CreateInstance<TestPassiveItem>();

        try
        {
            reservedPassive.name = "ReservedPassive";
            availablePassive.name = "AvailablePassive";

            ItemUnlockRegistry registry = registryObject.AddComponent<ItemUnlockRegistry>();
            registry.ResetToInitialState();
            Assert.That(registry.Unlock(reservedPassive), Is.True);
            Assert.That(registry.Unlock(availablePassive), Is.True);
            Assert.That(registry.TryReservePassive(reservedPassive), Is.True);

            SerializedObject serializedPool = new SerializedObject(pool);
            SerializedProperty passiveItems = serializedPool.FindProperty("passiveItems");
            passiveItems.arraySize = 2;
            passiveItems.GetArrayElementAtIndex(0).FindPropertyRelative("item").objectReferenceValue = reservedPassive;
            passiveItems.GetArrayElementAtIndex(1).FindPropertyRelative("item").objectReferenceValue = availablePassive;
            serializedPool.ApplyModifiedPropertiesWithoutUndo();

            System.Collections.Generic.List<ItemSpawnPool.RollResult> unlockedEntries = pool.GetUnlockedEntries(registry);

            Assert.That(unlockedEntries.Count, Is.EqualTo(1));
            Assert.That(ContainsPassive(unlockedEntries, reservedPassive), Is.False);
            Assert.That(ContainsPassive(unlockedEntries, availablePassive), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(registryObject);
            Object.DestroyImmediate(pool);
            Object.DestroyImmediate(reservedPassive);
            Object.DestroyImmediate(availablePassive);
        }
    }

    [Test]
    public void ItemSpawnPool_RollsAcrossCombinedChestPool_InsteadOfSplittingActiveAndPassive()
    {
        const int rollCount = 1000;

        GameObject registryObject = new GameObject("CombinedChestPoolTest");
        ItemSpawnPool pool = ScriptableObject.CreateInstance<ItemSpawnPool>();
        TestActiveItem activeItem = ScriptableObject.CreateInstance<TestActiveItem>();
        TestPassiveItem[] passiveItems = new TestPassiveItem[10];
        UnityEngine.Random.State previousState = UnityEngine.Random.state;

        try
        {
            activeItem.name = "CombinedActive";
            for (int i = 0; i < passiveItems.Length; i++)
            {
                passiveItems[i] = ScriptableObject.CreateInstance<TestPassiveItem>();
                passiveItems[i].name = $"CombinedPassive{i}";
            }

            ItemUnlockRegistry registry = registryObject.AddComponent<ItemUnlockRegistry>();
            SerializedObject serializedRegistry = new SerializedObject(registry);

            SerializedProperty initialPassives = serializedRegistry.FindProperty("initiallyUnlockedPassiveItems");
            initialPassives.arraySize = passiveItems.Length;
            for (int i = 0; i < passiveItems.Length; i++)
                initialPassives.GetArrayElementAtIndex(i).objectReferenceValue = passiveItems[i];

            SerializedProperty initialActives = serializedRegistry.FindProperty("initiallyUnlockedActiveItems");
            initialActives.arraySize = 1;
            initialActives.GetArrayElementAtIndex(0).objectReferenceValue = activeItem;
            serializedRegistry.ApplyModifiedPropertiesWithoutUndo();
            registry.ResetToInitialState();

            SerializedObject serializedPool = new SerializedObject(pool);
            SerializedProperty serializedPassiveItems = serializedPool.FindProperty("passiveItems");
            serializedPassiveItems.arraySize = passiveItems.Length;
            for (int i = 0; i < passiveItems.Length; i++)
                serializedPassiveItems.GetArrayElementAtIndex(i).FindPropertyRelative("item").objectReferenceValue = passiveItems[i];

            SerializedProperty serializedActiveItems = serializedPool.FindProperty("activeItems");
            serializedActiveItems.arraySize = 1;
            serializedActiveItems.GetArrayElementAtIndex(0).FindPropertyRelative("item").objectReferenceValue = activeItem;
            serializedPool.ApplyModifiedPropertiesWithoutUndo();

            int activeRolls = 0;
            UnityEngine.Random.InitState(12345);

            for (int i = 0; i < rollCount; i++)
            {
                Assert.That(pool.TryRollChestLoot(registry, out ItemSpawnPool.RollResult result), Is.True);
                if (!result.IsPassive)
                    activeRolls++;
            }

            Assert.That(activeRolls, Is.LessThan(180), "Active rolls are still too frequent for a combined 1:10 chest pool.");
        }
        finally
        {
            UnityEngine.Random.state = previousState;
            Object.DestroyImmediate(registryObject);
            Object.DestroyImmediate(pool);
            Object.DestroyImmediate(activeItem);

            for (int i = 0; i < passiveItems.Length; i++)
                DestroyImmediateSafe(passiveItems[i]);
        }
    }

    [Test]
    public void DashCooldownPassiveItem_ChangesResolvedDashCooldown()
    {
        GameObject player = null;
        DashCooldownPassiveItem item = ScriptableObject.CreateInstance<DashCooldownPassiveItem>();

        try
        {
            player = InstantiateConfiguredPlayer();
            PlayerStats stats = player.GetComponent<PlayerStats>();
            PlayerMove playerMove = player.GetComponent<PlayerMove>();
            item.dashCooldownReductionPercent = 0.25f;

            float baselineCooldown = playerMove.GetResolvedDashCooldown();

            item.OnEquip(stats);
            Assert.That(playerMove.GetResolvedDashCooldown(), Is.EqualTo(baselineCooldown * 0.75f).Within(0.001f));

            item.OnUnequip(stats);
            Assert.That(playerMove.GetResolvedDashCooldown(), Is.EqualTo(baselineCooldown).Within(0.001f));
        }
        finally
        {
            DestroyImmediateSafe(player);
            Object.DestroyImmediate(item);
        }
    }

    [Test]
    public void JumpBoostPassiveItem_ChangesResolvedJumpForce()
    {
        GameObject player = null;
        JumpBoostPassiveItem item = ScriptableObject.CreateInstance<JumpBoostPassiveItem>();

        try
        {
            player = InstantiateConfiguredPlayer();
            PlayerStats stats = player.GetComponent<PlayerStats>();
            PlayerMove playerMove = player.GetComponent<PlayerMove>();
            item.jumpForceIncreasePercent = 0.4f;

            float baselineJumpForce = playerMove.GetResolvedJumpForce();

            item.OnEquip(stats);
            Assert.That(playerMove.GetResolvedJumpForce(), Is.EqualTo(baselineJumpForce + (playerMove.jumpForce * 0.4f)).Within(0.001f));

            item.OnUnequip(stats);
            Assert.That(playerMove.GetResolvedJumpForce(), Is.EqualTo(baselineJumpForce).Within(0.001f));
        }
        finally
        {
            DestroyImmediateSafe(player);
            Object.DestroyImmediate(item);
        }
    }

    [Test]
    public void PotionHealBoostPassiveItem_ChangesResolvedPotionHealAmount()
    {
        GameObject player = null;
        PotionHealBoostPassiveItem item = ScriptableObject.CreateInstance<PotionHealBoostPassiveItem>();

        try
        {
            player = InstantiateConfiguredPlayer();
            PlayerStats stats = player.GetComponent<PlayerStats>();
            item.healMultiplierIncrease = 0.5f;

            float baselineHealAmount = stats.GetResolvedPotionHealAmount();

            item.OnEquip(stats);
            Assert.That(stats.GetResolvedPotionHealAmount(), Is.EqualTo(baselineHealAmount * 1.5f).Within(0.001f));

            item.OnUnequip(stats);
            Assert.That(stats.GetResolvedPotionHealAmount(), Is.EqualTo(baselineHealAmount).Within(0.001f));
        }
        finally
        {
            DestroyImmediateSafe(player);
            Object.DestroyImmediate(item);
        }
    }

    [Test]
    public void KillHealPassiveItem_HealsPlayerWhenKillIsNotified()
    {
        GameObject player = null;
        KillHealPassiveItem item = ScriptableObject.CreateInstance<KillHealPassiveItem>();

        try
        {
            player = InstantiateConfiguredPlayer();
            PlayerStats stats = player.GetComponent<PlayerStats>();
            item.healAmount = 7f;
            SetCurrentHp(stats, 20f);

            item.OnEquip(stats);
            stats.NotifyKill();

            Assert.That(stats.GetCurrentHp(), Is.EqualTo(27f).Within(0.001f));
        }
        finally
        {
            DestroyImmediateSafe(player);
            Object.DestroyImmediate(item);
        }
    }

    [Test]
    public void KillDamageBuffPassiveItem_AddsDamageWhenKillIsNotified()
    {
        GameObject player = null;
        KillDamageBuffPassiveItem item = ScriptableObject.CreateInstance<KillDamageBuffPassiveItem>();

        try
        {
            player = InstantiateConfiguredPlayer();
            PlayerStats stats = player.GetComponent<PlayerStats>();
            stats.damage = 3f;
            item.bonusDamage = 8f;
            item.duration = 4f;

            item.OnEquip(stats);
            stats.NotifyKill();

            Assert.That(stats.damage, Is.EqualTo(11f).Within(0.001f));
        }
        finally
        {
            DestroyImmediateSafe(player);
            Object.DestroyImmediate(item);
        }
    }

    [Test]
    public void KillDamageBuffPassiveItem_AddsDamageWhenEnemyDiesFromPlayerHitbox()
    {
        GameObject player = null;
        GameObject enemy = null;
        GameObject hitboxObject = null;
        KillDamageBuffPassiveItem item = ScriptableObject.CreateInstance<KillDamageBuffPassiveItem>();
        AttackData attackData = ScriptableObject.CreateInstance<AttackData>();

        try
        {
            player = InstantiateConfiguredPlayer();
            PlayerStats stats = player.GetComponent<PlayerStats>();
            stats.damage = 3f;
            item.bonusDamage = 8f;
            item.duration = 4f;
            item.OnEquip(stats);

            enemy = new GameObject("KillBuffEnemy");
            enemy.layer = ResolveLayer("Enemy");
            enemy.AddComponent<BoxCollider2D>();
            enemy.AddComponent<EnemyCrowdControl>();
            enemy.AddComponent<TestEnemyMovement>();
            EnemyStats enemyStats = enemy.AddComponent<EnemyStats>();
            enemyStats.maxHp = 5f;

            hitboxObject = new GameObject("KillBuffHitbox");
            BoxCollider2D hitboxCollider = hitboxObject.AddComponent<BoxCollider2D>();
            hitboxCollider.isTrigger = true;

            AttackHitBox hitbox = hitboxObject.AddComponent<AttackHitBox>();
            hitbox.hitLayer = 1 << enemy.layer;
            hitbox.SetPlayerOwner(stats);

            attackData.damage = 10f;
            attackData.damageScaling = 0f;
            attackData.hitboxSize = Vector2.one;
            attackData.attackDuration = 0.1f;
            attackData.knockbackForce = 0f;
            attackData.zUpperRange = 1f;
            attackData.zLowerRange = 1f;

            hitbox.EnemySetup(attackData, 0f, 0.1f, Vector3.zero, 0f, 0f);
            InvokePrivateMethod(hitbox, "OnTriggerEnter2D", enemy.GetComponent<Collider2D>());

            Assert.That(stats.damage, Is.EqualTo(11f).Within(0.001f));
        }
        finally
        {
            DestroyImmediateSafe(hitboxObject);
            DestroyImmediateSafe(enemy);
            DestroyImmediateSafe(player);
            Object.DestroyImmediate(attackData);
            Object.DestroyImmediate(item);
        }
    }

    [Test]
    public void AutoShieldPassiveItem_BlocksOneHit_AndResetsCooldownWhenDamagedDuringRecharge()
    {
        GameObject player = null;
        AutoShieldPassiveItem item = ScriptableObject.CreateInstance<AutoShieldPassiveItem>();

        try
        {
            player = InstantiateConfiguredPlayer();
            PlayerStats stats = player.GetComponent<PlayerStats>();
            item.cooldown = 6f;
            SetCurrentHp(stats, stats.GetMaxHp());

            item.OnEquip(stats);
            stats.TakeDamage(10f, Vector2.zero);

            Assert.That(stats.GetCurrentHp(), Is.EqualTo(stats.GetMaxHp()).Within(0.001f));

            Component effect = FindAttachedComponent(player, "AutoShieldPassiveEffect");
            Assert.That(effect, Is.Not.Null);

            SetPrivateField(effect, "cooldownRemaining", 1f);
            stats.TakeDamage(10f, Vector2.zero);

            Assert.That(stats.GetCurrentHp(), Is.EqualTo(stats.GetMaxHp() - 10f).Within(0.001f));
            Assert.That((float)GetPrivateField(effect, "cooldownRemaining"), Is.EqualTo(6f).Within(0.001f));
        }
        finally
        {
            DestroyImmediateSafe(player);
            Object.DestroyImmediate(item);
        }
    }

    [Test]
    public void DashInvulnerabilityPassiveItem_BlocksDamageOnlyDuringDash()
    {
        GameObject player = null;
        DashInvulnerabilityPassiveItem item = ScriptableObject.CreateInstance<DashInvulnerabilityPassiveItem>();

        try
        {
            player = InstantiateConfiguredPlayer();
            PlayerStats stats = player.GetComponent<PlayerStats>();
            PlayerMove playerMove = player.GetComponent<PlayerMove>();
            SetCurrentHp(stats, stats.GetMaxHp());

            item.OnEquip(stats);
            RaiseEvent(playerMove, "OnDashStarted");
            stats.TakeDamage(10f, Vector2.zero);

            Assert.That(stats.GetCurrentHp(), Is.EqualTo(stats.GetMaxHp()).Within(0.001f));

            RaiseEvent(playerMove, "OnDashEnded");
            stats.TakeDamage(10f, Vector2.zero);

            Assert.That(stats.GetCurrentHp(), Is.EqualTo(stats.GetMaxHp() - 10f).Within(0.001f));
        }
        finally
        {
            DestroyImmediateSafe(player);
            Object.DestroyImmediate(item);
        }
    }

    [Test]
    public void DashNovaPassiveItem_SpawnsDashNovaEffectOnDashEnd()
    {
        GameObject player = null;
        GameObject visualPrefab = null;
        GameObject effectPrefab = null;
        DashNovaPassiveItem item = ScriptableObject.CreateInstance<DashNovaPassiveItem>();
        AttackData attackData = ScriptableObject.CreateInstance<AttackData>();

        try
        {
            player = InstantiateConfiguredPlayer();
            PlayerStats stats = player.GetComponent<PlayerStats>();
            PlayerMove playerMove = player.GetComponent<PlayerMove>();
            visualPrefab = CreateDashNovaVisualPrefab();
            attackData.damage = 15f;
            attackData.damageScaling = 0.5f;
            attackData.hitboxSize = new Vector2(2.75f, 2.75f);
            attackData.attackDuration = 0.2f;
            attackData.knockbackForce = 1.5f;
            attackData.zUpperRange = 2f;
            attackData.zLowerRange = 0f;
            attackData.visual = visualPrefab;
            effectPrefab = CreateDashNovaEffectPrefab();
            item.effectPrefab = effectPrefab;
            item.attackData = attackData;
            item.rangeIndicatorLocalScale = new Vector3(2.75f, 2.75f, 1f);

            item.OnEquip(stats);
            RaiseEvent(playerMove, "OnDashEnded");

            GameObject dashNovaEffect = FindSceneObjectByName("DashNovaEffect");
            Assert.That(dashNovaEffect, Is.Not.Null);
            Assert.That(dashNovaEffect.GetComponent<AttackHitBox>(), Is.Not.Null);
            Assert.That(dashNovaEffect.GetComponent<CircleCollider2D>(), Is.Not.Null);
            Assert.That(dashNovaEffect.GetComponent<DashNovaEffectInstance>(), Is.Not.Null);
            Transform visual = dashNovaEffect.transform.Find("Visual");
            Assert.That(visual, Is.Not.Null);
            Assert.That(visual.GetComponent<SpriteRenderer>(), Is.Not.Null);
            Assert.That(visual.GetComponent<Animator>(), Is.Not.Null);
        }
        finally
        {
            DestroySceneObjectsByName("DashNovaEffect");
            DestroyImmediateSafe(player);
            DestroyImmediateSafe(visualPrefab);
            DestroyImmediateSafe(effectPrefab);
            Object.DestroyImmediate(attackData);
            Object.DestroyImmediate(item);
        }
    }

    [Test]
    public void PlayerMove_EndDashIfActive_RaisesDashEnded_WhenDashTimeAlreadyExceededDuration()
    {
        GameObject player = null;

        try
        {
            player = InstantiateConfiguredPlayer();
            PlayerMove playerMove = player.GetComponent<PlayerMove>();
            Assert.That(playerMove, Is.Not.Null);

            int dashEndedCalls = 0;
            playerMove.OnDashEnded += () => dashEndedCalls++;

            SetPrivateField(playerMove, "dashTime", playerMove.dashDuration + 0.05f);
            InvokePrivateMethod(playerMove, "EndDashIfActive");

            Assert.That(dashEndedCalls, Is.EqualTo(1));
            Assert.That((float)GetPrivateField(playerMove, "dashTime"), Is.EqualTo(playerMove.dashDuration).Within(0.001f));

            InvokePrivateMethod(playerMove, "EndDashIfActive");
            Assert.That(dashEndedCalls, Is.EqualTo(1));
        }
        finally
        {
            DestroyImmediateSafe(player);
        }
    }

    [Test]
    public void DashNovaPassiveItem_SpawnsDashNovaEffectWhenDashActuallyEnds()
    {
        GameObject player = null;
        GameObject visualPrefab = null;
        GameObject effectPrefab = null;
        DashNovaPassiveItem item = ScriptableObject.CreateInstance<DashNovaPassiveItem>();
        AttackData attackData = ScriptableObject.CreateInstance<AttackData>();

        try
        {
            player = InstantiateConfiguredPlayer();
            PlayerStats stats = player.GetComponent<PlayerStats>();
            PlayerMove playerMove = player.GetComponent<PlayerMove>();
            visualPrefab = CreateDashNovaVisualPrefab();
            attackData.damage = 15f;
            attackData.damageScaling = 0.5f;
            attackData.hitboxSize = new Vector2(2.75f, 2.75f);
            attackData.attackDuration = 0.2f;
            attackData.knockbackForce = 1.5f;
            attackData.zUpperRange = 2f;
            attackData.zLowerRange = 0f;
            attackData.visual = visualPrefab;
            effectPrefab = CreateDashNovaEffectPrefab();
            item.effectPrefab = effectPrefab;
            item.attackData = attackData;
            item.rangeIndicatorLocalScale = new Vector3(2.75f, 2.75f, 1f);

            item.OnEquip(stats);
            SetPrivateField(playerMove, "dashTime", playerMove.dashDuration + 0.05f);
            InvokePrivateMethod(playerMove, "EndDashIfActive");

            GameObject dashNovaEffect = FindSceneObjectByName("DashNovaEffect");
            Assert.That(dashNovaEffect, Is.Not.Null);
            Assert.That(dashNovaEffect.GetComponent<AttackHitBox>(), Is.Not.Null);
            Assert.That(dashNovaEffect.GetComponent<CircleCollider2D>(), Is.Not.Null);
            Assert.That(dashNovaEffect.GetComponent<DashNovaEffectInstance>(), Is.Not.Null);
            Assert.That(dashNovaEffect.transform.Find("Visual"), Is.Not.Null);
        }
        finally
        {
            DestroySceneObjectsByName("DashNovaEffect");
            DestroyImmediateSafe(player);
            DestroyImmediateSafe(visualPrefab);
            DestroyImmediateSafe(effectPrefab);
            Object.DestroyImmediate(attackData);
            Object.DestroyImmediate(item);
        }
    }

    [Test]
    public void DashNovaPassiveItem_DamagesEnemyAlreadyInsideNovaRange()
    {
        GameObject player = null;
        GameObject enemy = null;
        GameObject effectPrefab = null;
        DashNovaPassiveItem item = ScriptableObject.CreateInstance<DashNovaPassiveItem>();
        AttackData attackData = ScriptableObject.CreateInstance<AttackData>();

        try
        {
            player = InstantiateConfiguredPlayer();
            PlayerStats stats = player.GetComponent<PlayerStats>();
            PlayerMove playerMove = player.GetComponent<PlayerMove>();
            stats.damage = 3f;

            attackData.damage = 10f;
            attackData.damageScaling = 0f;
            attackData.hitboxSize = new Vector2(3f, 3f);
            attackData.attackDuration = 0.2f;
            attackData.knockbackForce = 0f;
            attackData.zUpperRange = 2f;
            attackData.zLowerRange = 0f;
            effectPrefab = CreateDashNovaEffectPrefab();
            item.effectPrefab = effectPrefab;
            item.attackData = attackData;

            enemy = new GameObject("DashNovaEnemy");
            enemy.layer = ResolveLayer("Enemy");
            enemy.transform.position = player.transform.position + new Vector3(0.5f, 0f, 0f);
            enemy.AddComponent<BoxCollider2D>();
            enemy.AddComponent<EnemyCrowdControl>();
            enemy.AddComponent<TestEnemyMovement>();
            EnemyStats enemyStats = enemy.AddComponent<EnemyStats>();
            SetPrivateField(enemyStats, "currentHp", 30f);

            item.OnEquip(stats);
            SetPrivateField(playerMove, "dashTime", playerMove.dashDuration + 0.05f);
            InvokePrivateMethod(playerMove, "EndDashIfActive");

            GameObject dashNovaEffect = FindSceneObjectByName("DashNovaEffect");
            Assert.That(dashNovaEffect, Is.Not.Null);
            AttackHitBox hitBox = dashNovaEffect.GetComponent<AttackHitBox>();
            Assert.That(hitBox, Is.Not.Null);

            InvokePrivateMethod(hitBox, "OnTriggerStay2D", enemy.GetComponent<Collider2D>());

            Assert.That((float)GetPrivateField(enemyStats, "currentHp"), Is.EqualTo(20f).Within(0.001f));
        }
        finally
        {
            DestroySceneObjectsByName("DashNovaEffect");
            DestroyImmediateSafe(enemy);
            DestroyImmediateSafe(player);
            DestroyImmediateSafe(effectPrefab);
            Object.DestroyImmediate(attackData);
            Object.DestroyImmediate(item);
        }
    }

    [Test]
    public void AttackHitProjectilePassiveItem_SpawnsProjectileOnAttackHit()
    {
        GameObject player = null;
        GameObject projectilePrefab = null;
        AttackHitProjectilePassiveItem item = ScriptableObject.CreateInstance<AttackHitProjectilePassiveItem>();

        try
        {
            player = InstantiateConfiguredPlayer();
            PlayerStats stats = player.GetComponent<PlayerStats>();
            AttackController attackController = player.GetComponent<AttackController>();
            Assert.That(attackController, Is.Not.Null);

            item.projectileLifetime = 1.25f;
            item.projectileSpeed = 9f;
            projectilePrefab = CreatePassiveAttackProjectilePrefab();
            item.projectilePrefab = projectilePrefab;
            item.OnEquip(stats);
            RaiseEvent(attackController, "OnAttackHit");

            GameObject projectileObject = FindSceneObjectByName("PassiveAttackProjectile");
            Assert.That(projectileObject, Is.Not.Null);
            Assert.That(projectileObject.GetComponent<Projectile>(), Is.Not.Null);
            Assert.That(projectileObject.GetComponent<AttackHitBox>(), Is.Not.Null);
        }
        finally
        {
            DestroySceneObjectsByName("PassiveAttackProjectile");
            DestroyImmediateSafe(player);
            DestroyImmediateSafe(projectilePrefab);
            Object.DestroyImmediate(item);
        }
    }

    [Test]
    public void AttackHitProjectilePassiveItem_SpawnsProjectileWhenPlayerHitboxActuallyHitsEnemy()
    {
        GameObject player = null;
        GameObject enemy = null;
        GameObject projectilePrefab = null;
        AttackHitProjectilePassiveItem item = ScriptableObject.CreateInstance<AttackHitProjectilePassiveItem>();
        AttackData attackData = ScriptableObject.CreateInstance<AttackData>();

        try
        {
            player = InstantiateConfiguredPlayer();
            PlayerStats stats = player.GetComponent<PlayerStats>();
            AttackController attackController = player.GetComponent<AttackController>();
            Assert.That(attackController, Is.Not.Null);

            GameObject hitboxObject = new GameObject("ProjectilePassiveHitbox");
            hitboxObject.transform.SetParent(player.transform, false);
            BoxCollider2D hitboxCollider = hitboxObject.AddComponent<BoxCollider2D>();
            hitboxCollider.isTrigger = true;

            AttackHitBox hitbox = hitboxObject.AddComponent<AttackHitBox>();
            attackController.hitbox = hitbox;
            InvokePrivateMethod(attackController, "Awake");

            enemy = new GameObject("ProjectilePassiveEnemy");
            enemy.layer = ResolveLayer("Enemy");
            enemy.transform.position = player.transform.position + Vector3.up;
            enemy.AddComponent<BoxCollider2D>();
            enemy.AddComponent<EnemyCrowdControl>();
            enemy.AddComponent<TestEnemyMovement>();
            EnemyStats enemyStats = enemy.AddComponent<EnemyStats>();
            enemyStats.maxHp = 50f;

            item.projectileLifetime = 1.25f;
            item.projectileSpeed = 9f;
            projectilePrefab = CreatePassiveAttackProjectilePrefab();
            item.projectilePrefab = projectilePrefab;
            item.OnEquip(stats);

            attackController.isAttacking = true;
            hitbox.hitLayer = 1 << enemy.layer;
            hitbox.SetPlayerOwner(stats);

            attackData.damage = 5f;
            attackData.damageScaling = 0f;
            attackData.hitboxSize = Vector2.one;
            attackData.attackDuration = 0.1f;
            attackData.knockbackForce = 0f;
            attackData.zUpperRange = 1f;
            attackData.zLowerRange = 1f;

            hitbox.EnemySetup(attackData, 0f, 0.1f, player.transform.position, 0f, 0f);
            InvokePrivateMethod(hitbox, "OnTriggerEnter2D", enemy.GetComponent<Collider2D>());

            GameObject projectileObject = FindSceneObjectByName("PassiveAttackProjectile");
            Assert.That(projectileObject, Is.Not.Null);
            Assert.That(projectileObject.GetComponent<Projectile>(), Is.Not.Null);
            Assert.That(projectileObject.transform.position.z, Is.EqualTo(0f).Within(0.001f));
        }
        finally
        {
            DestroySceneObjectsByName("PassiveAttackProjectile");
            DestroyImmediateSafe(enemy);
            DestroyImmediateSafe(player);
            DestroyImmediateSafe(projectilePrefab);
            Object.DestroyImmediate(attackData);
            Object.DestroyImmediate(item);
        }
    }

    [Test]
    public void HealingPulsePassiveItem_KnocksBackNearbyEnemiesWhenPlayerHeals()
    {
        GameObject player = null;
        GameObject enemy = null;
        HealingPulsePassiveItem item = ScriptableObject.CreateInstance<HealingPulsePassiveItem>();

        try
        {
            player = InstantiateConfiguredPlayer();
            PlayerStats stats = player.GetComponent<PlayerStats>();
            item.radius = 3f;
            item.knockbackForce = 2.5f;
            item.enemyLayer = ~0;
            item.OnEquip(stats);

            enemy = new GameObject("HealingPulseEnemy");
            enemy.transform.position = player.transform.position + Vector3.right;
            enemy.AddComponent<CircleCollider2D>();
            EnemyCrowdControl crowdControl = enemy.AddComponent<EnemyCrowdControl>();
            EnemyStats enemyStats = enemy.AddComponent<EnemyStats>();

            Assert.That(enemyStats, Is.Not.Null);
            Assert.That(crowdControl, Is.Not.Null);

            SetCurrentHp(stats, 40f);
            stats.Heal(10f);

            Assert.That(crowdControl.IsKnockbackActive, Is.True);
            Assert.That(Vector2.Distance(crowdControl.GetKnockbackDirection(), Vector2.right), Is.LessThanOrEqualTo(0.001f));
        }
        finally
        {
            DestroyImmediateSafe(enemy);
            DestroyImmediateSafe(player);
            Object.DestroyImmediate(item);
        }
    }

    private static bool ContainsPassive(System.Collections.Generic.List<ItemSpawnPool.RollResult> entries, PassiveItem target)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].IsPassive && entries[i].PassiveItem == target)
                return true;
        }

        return false;
    }

    private static bool ContainsActive(System.Collections.Generic.List<ItemSpawnPool.RollResult> entries, ActiveItem target)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (!entries[i].IsPassive && entries[i].ActiveItem == target)
                return true;
        }

        return false;
    }

    private static GameObject InstantiateConfiguredPlayer()
    {
        GameObject player = new GameObject("TestPlayer");
        player.transform.position = Vector3.zero;

        player.AddComponent<PlayerCrowdControl>();

        PlayerStats stats = player.AddComponent<PlayerStats>();
        Assert.That(stats, Is.Not.Null);
        SetCurrentHp(stats, stats.GetMaxHp());

        PlayerMove playerMove = player.AddComponent<PlayerMove>();
        playerMove.jumpForce = 5f;
        playerMove.dashCooltime = 2f;
        playerMove.floorZ = 0;

        AttackController attackController = player.AddComponent<AttackController>();
        Assert.That(attackController, Is.Not.Null);

        return player;
    }

    private static void SetCurrentHp(PlayerStats stats, float value)
    {
        SetPrivateField(stats, "currentHp", Mathf.Clamp(value, 0f, stats.GetMaxHp()));
    }

    private static void RaiseEvent(Component target, string eventName)
    {
        Assert.That(target, Is.Not.Null);

        FieldInfo[] fields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        FieldInfo eventField = Array.Find(fields, field => field.Name == eventName || field.Name.Contains(eventName, StringComparison.Ordinal));
        Assert.That(eventField, Is.Not.Null, $"Could not find backing field for event '{eventName}' on {target.GetType().Name}.");

        MulticastDelegate callbacks = eventField.GetValue(target) as MulticastDelegate;
        Assert.That(callbacks, Is.Not.Null, $"Event '{eventName}' has no subscribers on {target.GetType().Name}.");
        callbacks.DynamicInvoke();
    }

    private static void InvokePrivateMethod(object target, string methodName, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"Could not find method '{methodName}' on {target.GetType().Name}.");
        method.Invoke(target, arguments);
    }

    private static Component FindAttachedComponent(GameObject target, string typeName)
    {
        Component[] components = target.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null && components[i].GetType().Name == typeName)
                return components[i];
        }

        return null;
    }

    private static object GetPrivateField(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Could not find field '{fieldName}' on {target.GetType().Name}.");
        return field.GetValue(target);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Could not find field '{fieldName}' on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private static GameObject FindSceneObjectByName(string objectName)
    {
        GameObject[] gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < gameObjects.Length; i++)
        {
            GameObject gameObject = gameObjects[i];
            if (gameObject == null || !gameObject.scene.IsValid())
                continue;

            if (gameObject.name == objectName)
                return gameObject;
        }

        return null;
    }

    private static void DestroySceneObjectsByName(string objectName)
    {
        GameObject[] gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < gameObjects.Length; i++)
        {
            GameObject gameObject = gameObjects[i];
            if (gameObject == null || !gameObject.scene.IsValid() || gameObject.name != objectName)
                continue;

            Object.DestroyImmediate(gameObject);
        }
    }

    private static GameObject CreateDashNovaVisualPrefab()
    {
        GameObject visualPrefab = new GameObject("DashNovaVisualPrefab");
        visualPrefab.AddComponent<SpriteRenderer>();
        visualPrefab.AddComponent<Animator>();
        return visualPrefab;
    }

    private static GameObject CreateDashNovaEffectPrefab()
    {
        GameObject effectPrefab = new GameObject("DashNovaEffectPrefab");
        CircleCollider2D collider = effectPrefab.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        effectPrefab.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        effectPrefab.AddComponent<AttackHitBox>();
        effectPrefab.AddComponent<DashNovaEffectInstance>();
        return effectPrefab;
    }

    private static GameObject CreatePassiveAttackProjectilePrefab()
    {
        GameObject projectilePrefab = new GameObject("PassiveAttackProjectilePrefab");
        projectilePrefab.AddComponent<SpriteRenderer>();
        BoxCollider2D collider = projectilePrefab.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        projectilePrefab.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        projectilePrefab.AddComponent<AttackHitBox>();
        projectilePrefab.AddComponent<Projectile>();
        projectilePrefab.AddComponent<PassiveAttackProjectileInstance>();
        return projectilePrefab;
    }

    private static void DestroyImmediateSafe(UnityEngine.Object target)
    {
        if (target != null)
            Object.DestroyImmediate(target);
    }

    private static int ResolveLayer(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        Assert.That(layer, Is.GreaterThanOrEqualTo(0), $"Layer '{layerName}' does not exist in the project.");
        return layer;
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

    private sealed class TestEnemyMovement : MonoBehaviour, IEnemyMovement
    {
        public int FloorZ => 0;

        public void Move()
        {
        }

        public void SetPlayerDetected(bool val)
        {
        }

        public void ApplyCrowdControl(EnemyCrowdControlRequest request)
        {
        }
    }

}
