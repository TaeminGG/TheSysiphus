using NUnit.Framework;

public class CombatFlowPlayModeTests
{
    [Test]
    public void Scaffold_IsConfigured_ForPlayModeCombatTests()
    {
        Assert.Pass("PlayMode combat test scaffold is ready.");
    }

    [Test]
    [Ignore("TODO: Build a grounded player setup and verify attack starts when grounded.")]
    public void PlayerAttack_Starts_WhenGrounded()
    {
    }

    [Test]
    [Ignore("TODO: Build a jumping player setup and verify attack does not start while airborne.")]
    public void PlayerAttack_DoesNotStart_WhileJumping()
    {
    }

    [Test]
    [Ignore("TODO: Build a falling player setup and verify damage still applies while falling.")]
    public void PlayerDamage_Applies_WhileFalling()
    {
    }

    [Test]
    [Ignore("TODO: Build attacker/target Z-offset fixtures and verify AttackHitBox only hits within Z range.")]
    public void AttackHitBox_HitsOnlyTargets_WithinZRange()
    {
    }
}
