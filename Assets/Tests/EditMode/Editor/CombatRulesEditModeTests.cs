using NUnit.Framework;
using System.Reflection;
using UnityEngine;

public class CombatRulesEditModeTests
{
    [Test]
    public void Scaffold_IsConfigured_ForEditModeCombatTests()
    {
        Assert.Pass("EditMode combat test scaffold is ready.");
    }

    [Test]
    public void AttackDamage_ReturnsBaseDamagePlusRoundedScaledPlayerDamage()
    {
        var data = ScriptableObject.CreateInstance<AttackData>();
        try
        {
            data.damage = 10f;
            data.damageScaling = 0.5f;

            var finalDamage = data.GetFinalDamage(7f);

            Assert.That(finalDamage, Is.EqualTo(14f));
        }
        finally
        {
            Object.DestroyImmediate(data);
        }
    }

    [Test]
    public void AttackDamage_RoundsScaledDamageBeforeAddingBaseDamage()
    {
        var data = ScriptableObject.CreateInstance<AttackData>();
        try
        {
            data.damage = 12f;
            data.damageScaling = 0.5f;

            var finalDamage = data.GetFinalDamage(5f);

            Assert.That(finalDamage, Is.EqualTo(14f));
        }
        finally
        {
            Object.DestroyImmediate(data);
        }
    }

    [Test]
    [Ignore("TODO: Extract attack timing calculation and verify attack speed scaling.")]
    public void AttackTiming_ScalesWithAttackSpeed()
    {
    }

    [Test]
    [Ignore("TODO: Extract grounded/jumping attack gate and verify players cannot attack while jumping.")]
    public void PlayerAttack_Disabled_WhileJumping()
    {
    }

    [Test]
    [Ignore("TODO: Extract damage eligibility rule and verify players can still be hit while falling.")]
    public void PlayerDamage_Allowed_WhileFalling()
    {
    }

    [Test]
    public void GorgonAttack_PrioritizesPetrifyingGaze_WhenSkillIsReadyInSkillRange()
    {
        GorgonAttackMode mode = GorgonAttack.SelectAttackMode(
            distance: 1.5f,
            sameFloor: true,
            skillReady: true,
            skillRangeValue: 2.5f,
            meleeRangeValue: 0.8f,
            rangedRangeValue: 4.5f);

        Assert.That(mode, Is.EqualTo(GorgonAttackMode.PetrifyingGaze));
    }

    [Test]
    public void GorgonAttack_UsesMelee_WhenSkillIsCoolingDownAndPlayerIsOnSameFloor()
    {
        GorgonAttackMode mode = GorgonAttack.SelectAttackMode(
            distance: 0.6f,
            sameFloor: true,
            skillReady: false,
            skillRangeValue: 2.5f,
            meleeRangeValue: 0.8f,
            rangedRangeValue: 4.5f);

        Assert.That(mode, Is.EqualTo(GorgonAttackMode.Melee));
    }

    [Test]
    public void GorgonAttack_UsesRanged_WhenPlayerIsOnDifferentFloor()
    {
        GorgonAttackMode mode = GorgonAttack.SelectAttackMode(
            distance: 0.6f,
            sameFloor: false,
            skillReady: false,
            skillRangeValue: 2.5f,
            meleeRangeValue: 0.8f,
            rangedRangeValue: 4.5f);

        Assert.That(mode, Is.EqualTo(GorgonAttackMode.Ranged));
    }

    [Test]
    public void GorgonAttack_FacingCheck_RequiresPlayerFacingTowardEnemy()
    {
        bool isFacing = GorgonAttack.IsFacingTarget(Vector2.right, Vector2.right, 0.55f);
        bool isNotFacing = GorgonAttack.IsFacingTarget(Vector2.right, Vector2.left, 0.55f);

        Assert.That(isFacing, Is.True);
        Assert.That(isNotFacing, Is.False);
    }

    [Test]
    public void GorgonAttack_PetrifyApplyRange_IsCheckedSeparatelyFromTriggerRange()
    {
        bool inApplyRange = GorgonAttack.CanApplyPetrify(1.5f, 1.75f);
        bool outOfApplyRange = GorgonAttack.CanApplyPetrify(2.1f, 1.75f);

        Assert.That(inApplyRange, Is.True);
        Assert.That(outOfApplyRange, Is.False);
    }

    [Test]
    public void PlayerStatusTickDamage_IgnoresHitInvulnerability()
    {
        GameObject player = new GameObject("Player");
        try
        {
            PlayerStats stats = player.AddComponent<PlayerStats>();
            SetCurrentHp(stats, stats.GetMaxHp());

            stats.TakeDamage(10f, Vector2.zero);
            stats.TakeStatusTickDamage(5f);

            Assert.That(stats.GetCurrentHp(), Is.EqualTo(85f));
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void PlayerStatusTickDamage_DoesNotStartHitInvulnerability()
    {
        GameObject player = new GameObject("Player");
        try
        {
            PlayerStats stats = player.AddComponent<PlayerStats>();
            SetCurrentHp(stats, stats.GetMaxHp());

            stats.TakeStatusTickDamage(5f);
            stats.TakeDamage(10f, Vector2.zero);

            Assert.That(stats.GetCurrentHp(), Is.EqualTo(85f));
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void PlayerStatusTickDamage_IsBlockedByExternalInvulnerability()
    {
        GameObject player = new GameObject("Player");
        try
        {
            PlayerStats stats = player.AddComponent<PlayerStats>();
            SetCurrentHp(stats, stats.GetMaxHp());
            stats.AddExternalInvulnerabilitySource();

            stats.TakeStatusTickDamage(5f);

            Assert.That(stats.GetCurrentHp(), Is.EqualTo(100f));
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void PlayerStatusTickDamage_CanBePreventedByBeforeDamageTaken()
    {
        GameObject player = new GameObject("Player");
        try
        {
            PlayerStats stats = player.AddComponent<PlayerStats>();
            SetCurrentHp(stats, stats.GetMaxHp());
            stats.BeforeDamageTaken += (_, _) => true;

            stats.TakeStatusTickDamage(5f);

            Assert.That(stats.GetCurrentHp(), Is.EqualTo(100f));
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void PlayerControlLock_BlocksAndReleasesSelectedActions()
    {
        GameObject player = new GameObject("PlayerControlLockTest");
        try
        {
            PlayerCrowdControl crowdControl = player.AddComponent<PlayerCrowdControl>();

            int token = crowdControl.AddControlLock(PlayerControlLock.Move | PlayerControlLock.Dash);

            Assert.That(crowdControl.CanMove, Is.False);
            Assert.That(crowdControl.CanDash, Is.False);
            Assert.That(crowdControl.CanAttack, Is.True);

            Assert.That(crowdControl.RemoveControlLock(token), Is.True);
            Assert.That(crowdControl.CanMove, Is.True);
            Assert.That(crowdControl.CanDash, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    private static void SetCurrentHp(PlayerStats stats, float value)
    {
        FieldInfo field = typeof(PlayerStats).GetField("currentHp", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(stats, Mathf.Clamp(value, 0f, stats.GetMaxHp()));
    }
}
