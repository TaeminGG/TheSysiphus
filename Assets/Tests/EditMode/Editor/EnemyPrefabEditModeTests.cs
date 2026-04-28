using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class EnemyPrefabEditModeTests
{
    private const string SkeletonPrefabPath = "Assets/Prefabs/Enemies/Skeleton.prefab";
    private const string PoisonUndeadPrefabPath = "Assets/Prefabs/Enemies/PoisonUndead.prefab";
    private const string FireElementalPrefabPath = "Assets/Prefabs/Enemies/FireElemental.prefab";
    private const string SpiritPrefabPath = "Assets/Prefabs/Enemies/Spirit.prefab";
    private const string BoomerPrefabPath = "Assets/Prefabs/Enemies/Boomer.prefab";
    private const string KampeTracePrefabPath = "Assets/Prefabs/Enemies/KampeTrace.prefab";
    private const string MinotaurPrefabPath = "Assets/Prefabs/Enemies/Minotaur.prefab";
    private const string MinotaurChargeHitBoxPrefabPath = "Assets/Prefabs/Enemies/AttackDummy/MinotaurChargeHitBox.prefab";
    private const string MinotaurEarthquakeBlastPrefabPath = "Assets/Prefabs/Enemies/AttackDummy/MinotaurEarthquakeBlast.prefab";
    private const string SkeletonProjectilePrefabPath = "Assets/Prefabs/Enemies/AttackDummy/SkeletonProjectile.prefab";
    private const string EnemyCircleBlastPrefabPath = "Assets/Prefabs/Enemies/AttackDummy/EnemyCircleBlast.prefab";

    [Test]
    public void SkeletonPrefab_IsConfiguredForGroundedProjectileCombat()
    {
        ValidateEnemyPrefab(
            SkeletonPrefabPath,
            instance =>
            {
                Assert.That(instance.GetComponent<GroundedEnemy>(), Is.Not.Null);
                Assert.That(instance.GetComponent<SkeletonMovement>(), Is.Not.Null);

                EnemyProjectileAttack attack = instance.GetComponent<EnemyProjectileAttack>();
                Assert.That(attack, Is.Not.Null);
                Assert.That(attack.projectilePrefab, Is.Not.Null);
                Assert.That(attack.attackData, Is.Not.Null);
                Assert.That(attack.requireCasterFloorAtLeastTargetFloor, Is.True);
            });
    }

    [Test]
    public void PoisonUndeadPrefab_IsConfiguredToApplyPoison()
    {
        ValidateEnemyPrefab(
            PoisonUndeadPrefabPath,
            instance =>
            {
                Assert.That(instance.GetComponent<GroundedEnemy>(), Is.Not.Null);
                Assert.That(instance.GetComponent<UndeadMovement>(), Is.Not.Null);

                EnemyMeleeAttack attack = instance.GetComponent<EnemyMeleeAttack>();
                Assert.That(attack, Is.Not.Null);
                Assert.That(instance.GetComponent<IEnemyAttack>(), Is.SameAs(attack));
                Assert.That(attack.hitBox, Is.Not.Null);
                Assert.That(attack.attackData, Is.Not.Null);
                Assert.That(attack.poisonDuration, Is.GreaterThan(0f));
                Assert.That(attack.poisonDamagePerSecond, Is.GreaterThan(0f));
            });
    }

    [Test]
    public void FireElementalPrefab_IsConfiguredToApplyBurn()
    {
        ValidateEnemyPrefab(
            FireElementalPrefabPath,
            instance =>
            {
                Assert.That(instance.GetComponent<GroundedEnemy>(), Is.Not.Null);
                Assert.That(instance.GetComponent<UndeadMovement>(), Is.Not.Null);

                EnemyMeleeAttack attack = instance.GetComponent<EnemyMeleeAttack>();
                Assert.That(attack, Is.Not.Null);
                Assert.That(instance.GetComponent<IEnemyAttack>(), Is.SameAs(attack));
                Assert.That(attack.hitBox, Is.Not.Null);
                Assert.That(attack.attackData, Is.Not.Null);
                Assert.That(attack.burnDuration, Is.GreaterThan(0f));
                Assert.That(attack.burnTickInterval, Is.GreaterThan(0f));
                Assert.That(attack.burnDamagePerTick, Is.GreaterThan(0f));
            });
    }

    [Test]
    public void SpiritPrefab_UsesFloatingBaseAndZAgnosticMelee()
    {
        ValidateEnemyPrefab(
            SpiritPrefabPath,
            instance =>
            {
                Assert.That(instance.GetComponent<FloatingEnemy>(), Is.Not.Null);
                Assert.That(instance.GetComponent<GroundedEnemy>(), Is.Null);
                Assert.That(instance.GetComponent<SpiritMovement>(), Is.Not.Null);

                EnemyMeleeAttack attack = instance.GetComponent<EnemyMeleeAttack>();
                Assert.That(attack, Is.Not.Null);
                Assert.That(attack.hitBox, Is.Not.Null);
                Assert.That(attack.attackData, Is.Not.Null);
                Assert.That(attack.requireSameFloor, Is.False);
                Assert.That(attack.ignorePlayerZCheck, Is.True);
            });
    }

    [Test]
    public void BoomerPrefab_IsConfiguredForFloatingSelfDestruct()
    {
        ValidateEnemyPrefab(
            BoomerPrefabPath,
            instance =>
            {
                Assert.That(instance.GetComponent<FloatingEnemy>(), Is.Not.Null);
                Assert.That(instance.GetComponent<BoomerMovement>(), Is.Not.Null);

                BoomerAttack attack = instance.GetComponent<BoomerAttack>();
                Assert.That(attack, Is.Not.Null);
                Assert.That(attack.explosionHitBoxPrefab, Is.Not.Null);
                Assert.That(attack.explosionAttackData, Is.Not.Null);
                Assert.That(attack.fuseTime, Is.GreaterThan(0f));
            });
    }

    [Test]
    public void KampeTracePrefab_IsConfiguredForDelayedGroundBlast()
    {
        ValidateEnemyPrefab(
            KampeTracePrefabPath,
            instance =>
            {
                Assert.That(instance.GetComponent<GroundedEnemy>(), Is.Not.Null);
                Assert.That(instance.GetComponent<StaticEnemyMovement>(), Is.Not.Null);

                DelayedGroundBlastAttack attack = instance.GetComponent<DelayedGroundBlastAttack>();
                Assert.That(attack, Is.Not.Null);
                Assert.That(attack.hitBoxPrefab, Is.Not.Null);
                Assert.That(attack.attackData, Is.Not.Null);
                Assert.That(attack.telegraphRadius, Is.GreaterThan(0f));
            });
    }

    [Test]
    public void MinotaurPrefab_IsConfiguredForAllElitePatterns()
    {
        ValidateEnemyPrefab(
            MinotaurPrefabPath,
            instance =>
            {
                Assert.That(instance.GetComponent<GroundedEnemy>(), Is.Not.Null);
                Assert.That(instance.GetComponent<MinotaurMovement>(), Is.Not.Null);

                MinotaurAttack attack = instance.GetComponent<MinotaurAttack>();
                Assert.That(attack, Is.Not.Null);
                Assert.That(attack.earthquakeEffectRadius, Is.GreaterThan(0f));
                Assert.That(attack.earthquakeHitBoxPrefab, Is.Not.Null);
                Assert.That(attack.earthquakeAttackData, Is.Not.Null);
                Assert.That(attack.comboHitBox, Is.Not.Null);
                Assert.That(attack.comboFirstAttackData, Is.Not.Null);
                Assert.That(attack.comboSecondAttackData, Is.Not.Null);
                Assert.That(attack.chargeHitBoxPrefab, Is.Not.Null);
                Assert.That(attack.chargeAttackData, Is.Not.Null);
                Assert.That(attack.chargeCoolTime, Is.GreaterThan(0f));
            });
    }

    [Test]
    public void ChunkLoaderApplySpawnFloorZ_UpdatesGroundedEnemyPrefabsAndTheirMovementState()
    {
        AssertSpawnFloorZSync(
            SkeletonPrefabPath,
            4,
            instance =>
            {
                Assert.That(instance.GetComponent<SkeletonMovement>().floorZ, Is.EqualTo(4));
                Assert.That(instance.GetComponent<GroundedEnemy>().floorZ, Is.EqualTo(4));
            });

        AssertSpawnFloorZSync(
            PoisonUndeadPrefabPath,
            6,
            instance =>
            {
                Assert.That(instance.GetComponent<UndeadMovement>().floorZ, Is.EqualTo(6));
                Assert.That(instance.GetComponent<GroundedEnemy>().floorZ, Is.EqualTo(6));
            });

        AssertSpawnFloorZSync(
            FireElementalPrefabPath,
            2,
            instance =>
            {
                Assert.That(instance.GetComponent<UndeadMovement>().floorZ, Is.EqualTo(2));
                Assert.That(instance.GetComponent<GroundedEnemy>().floorZ, Is.EqualTo(2));
            });

        AssertSpawnFloorZSync(
            KampeTracePrefabPath,
            8,
            instance =>
            {
                Assert.That(instance.GetComponent<StaticEnemyMovement>().floorZ, Is.EqualTo(8));
                Assert.That(instance.GetComponent<GroundedEnemy>().floorZ, Is.EqualTo(8));
            });

        AssertSpawnFloorZSync(
            MinotaurPrefabPath,
            4,
            instance =>
            {
                Assert.That(instance.GetComponent<MinotaurMovement>().floorZ, Is.EqualTo(4));
                Assert.That(instance.GetComponent<GroundedEnemy>().floorZ, Is.EqualTo(4));
            });
    }

    [Test]
    public void ChunkLoaderApplySpawnFloorZ_UpdatesFloatingEnemyPrefabs()
    {
        AssertSpawnFloorZSync(
            SpiritPrefabPath,
            4,
            instance => Assert.That(instance.GetComponent<SpiritMovement>().floorZ, Is.EqualTo(4)));

        AssertSpawnFloorZSync(
            BoomerPrefabPath,
            6,
            instance => Assert.That(instance.GetComponent<BoomerMovement>().floorZ, Is.EqualTo(6)));
    }

    [Test]
    public void SkeletonProjectilePrefab_HasProjectileAndAttackHitBox()
    {
        GameObject prefab = LoadPrefab(SkeletonProjectilePrefabPath);
        Assert.That(prefab.GetComponent<Projectile>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<AttackHitBox>(), Is.Not.Null);
    }

    [Test]
    public void EnemyCircleBlastPrefab_HasProjectileAndAttackHitBox()
    {
        GameObject prefab = LoadPrefab(EnemyCircleBlastPrefabPath);
        Assert.That(prefab.GetComponent<Projectile>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<AttackHitBox>(), Is.Not.Null);
    }

    [Test]
    public void MinotaurEarthquakeBlastPrefab_IsConfiguredAsTemporaryAoE()
    {
        GameObject prefab = LoadPrefab(MinotaurEarthquakeBlastPrefabPath);
        Assert.That(prefab.GetComponent<Projectile>(), Is.Null);
        Assert.That(prefab.GetComponent<CircleCollider2D>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<AttackHitBox>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<TemporaryAttackLifetime>(), Is.Not.Null);
    }

    [Test]
    public void MinotaurChargeHitBoxPrefab_IsConfiguredAsTemporaryFollowHitBox()
    {
        GameObject prefab = LoadPrefab(MinotaurChargeHitBoxPrefabPath);
        Assert.That(prefab.GetComponent<Projectile>(), Is.Null);
        Assert.That(prefab.GetComponent<BoxCollider2D>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<AttackHitBox>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<TemporaryAttackLifetime>(), Is.Not.Null);
    }

    private static void ValidateEnemyPrefab(string prefabPath, System.Action<GameObject> extraAssertions)
    {
        GameObject prefab = LoadPrefab(prefabPath);
        GameObject instance = Object.Instantiate(prefab);

        try
        {
            AssertNoMissingScripts(instance);

            Assert.That(instance.GetComponent<EnemyStats>(), Is.Not.Null);
            Assert.That(instance.GetComponentInChildren<SpriteRenderer>(true), Is.Not.Null);

            Animator animator = instance.GetComponentInChildren<Animator>(true);
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null);

            extraAssertions(instance);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static void AssertSpawnFloorZSync(string prefabPath, int floorZ, System.Action<GameObject> extraAssertions)
    {
        GameObject prefab = LoadPrefab(prefabPath);
        GameObject instance = Object.Instantiate(prefab);

        try
        {
            ChunkLoader.ApplySpawnFloorZToObject(instance, floorZ);
            extraAssertions(instance);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static GameObject LoadPrefab(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Assert.That(prefab, Is.Not.Null, $"Failed to load prefab at '{prefabPath}'.");
        return prefab;
    }

    private static void AssertNoMissingScripts(GameObject root)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            int missingScriptCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
            Assert.That(missingScriptCount, Is.Zero, $"'{child.name}' has {missingScriptCount} missing script reference(s).");
        }
    }
}
