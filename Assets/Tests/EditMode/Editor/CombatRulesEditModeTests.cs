using NUnit.Framework;
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
}
