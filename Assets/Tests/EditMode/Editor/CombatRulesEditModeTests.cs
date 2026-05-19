using NUnit.Framework;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
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
    public void PlayerDirectionalAnimator_MapsVectorsToEightDirections()
    {
        Assert.That(PlayerDirectionalAnimator.GetDirection(Vector2.down), Is.EqualTo(PlayerDirectionalAnimator.Direction8.South));
        Assert.That(PlayerDirectionalAnimator.GetDirection(new Vector2(1f, -1f)), Is.EqualTo(PlayerDirectionalAnimator.Direction8.SouthEast));
        Assert.That(PlayerDirectionalAnimator.GetDirection(Vector2.right), Is.EqualTo(PlayerDirectionalAnimator.Direction8.East));
        Assert.That(PlayerDirectionalAnimator.GetDirection(new Vector2(1f, 1f)), Is.EqualTo(PlayerDirectionalAnimator.Direction8.NorthEast));
        Assert.That(PlayerDirectionalAnimator.GetDirection(Vector2.up), Is.EqualTo(PlayerDirectionalAnimator.Direction8.North));
        Assert.That(PlayerDirectionalAnimator.GetDirection(new Vector2(-1f, 1f)), Is.EqualTo(PlayerDirectionalAnimator.Direction8.NorthWest));
        Assert.That(PlayerDirectionalAnimator.GetDirection(Vector2.left), Is.EqualTo(PlayerDirectionalAnimator.Direction8.West));
        Assert.That(PlayerDirectionalAnimator.GetDirection(new Vector2(-1f, -1f)), Is.EqualTo(PlayerDirectionalAnimator.Direction8.SouthWest));
    }

    [Test]
    public void PlayerAnimatorController_IsConfiguredForEightWayMotionStates()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Sprites/Player/PlayerAnim.controller");

        Assert.That(controller, Is.Not.Null);
        Assert.That(HasParameter(controller, "Direction", AnimatorControllerParameterType.Float), Is.True);
        Assert.That(HasParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool), Is.True);
        Assert.That(HasParameter(controller, "Jump", AnimatorControllerParameterType.Trigger), Is.True);
        Assert.That(HasParameter(controller, "AttackEnded", AnimatorControllerParameterType.Trigger), Is.True);
        Assert.That(HasState(controller, "Idle_8Way"), Is.True);
        Assert.That(HasState(controller, "Run_8Way"), Is.True);
        Assert.That(HasState(controller, "Jump_8Way"), Is.True);
        Assert.That(HasParameter(controller, "Dash", AnimatorControllerParameterType.Trigger), Is.False);
        Assert.That(HasState(controller, "Dash_8Way"), Is.False);
        Assert.That(HasState(controller, "Attack1_8Way"), Is.True);
        Assert.That(HasState(controller, "Attack2_8Way"), Is.True);
        Assert.That(HasState(controller, "Attack3_8Way"), Is.True);
        Assert.That(GetStateSpeed(controller, "Attack1_8Way"), Is.EqualTo(1f).Within(0.0001f));
        Assert.That(GetStateSpeed(controller, "Attack2_8Way"), Is.EqualTo(1f).Within(0.0001f));
        Assert.That(GetStateSpeed(controller, "Attack3_8Way"), Is.EqualTo(1f).Within(0.0001f));
        Assert.That(AttackStateReturnsImmediately(controller, "Attack1_8Way"), Is.True);
        Assert.That(AttackStateReturnsImmediately(controller, "Attack2_8Way"), Is.True);
        Assert.That(AttackStateReturnsImmediately(controller, "Attack3_8Way"), Is.True);
    }

    [Test]
    public void PlayerAnimatorController_UsesSpriteBackedClips_ForCorePlayerMotions()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Sprites/Player/PlayerAnim.controller");

        Assert.That(controller, Is.Not.Null);
        Assert.That(BlendTreeHasSpriteClips(controller, "Idle_8Way"), Is.True);
        Assert.That(BlendTreeHasSpriteClips(controller, "Run_8Way"), Is.True);
        Assert.That(BlendTreeHasSpriteClips(controller, "Jump_8Way"), Is.True);
        Assert.That(BlendTreeHasSpriteClips(controller, "Attack1_8Way"), Is.True);
        Assert.That(BlendTreeHasSpriteClips(controller, "Attack2_8Way"), Is.True);
        Assert.That(BlendTreeHasSpriteClips(controller, "Attack3_8Way"), Is.True);
        Assert.That(BlendTreeClipsLoop(controller, "Jump_8Way"), Is.False);
        Assert.That(JumpStateReturnsOnlyAfterLanding(controller), Is.True);
        Assert.That(JumpStateReturnsImmediatelyAfterLanding(controller), Is.True);
    }

    [Test]
    public void AttackTiming_ScalesAttackDurationWithAttackSpeed()
    {
        Assert.That(AttackAnimationTiming.GetScaledAttackDuration(0.5f, 1f), Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(AttackAnimationTiming.GetScaledAttackDuration(0.5f, 2f), Is.EqualTo(0.25f).Within(0.0001f));
        Assert.That(AttackAnimationTiming.GetScaledAttackDuration(0.5f, 0.5f), Is.EqualTo(1f).Within(0.0001f));
    }

    [Test]
    public void AttackTiming_ScalesComboWindowWithAttackSpeedSnapshot()
    {
        float speedAtAttackStart = 2f;
        float changedSpeedDuringAttack = 0.5f;

        float expectedWindow = AttackAnimationTiming.GetScaledComboWindow(0.3f, speedAtAttackStart);
        float unstableWindow = AttackAnimationTiming.GetScaledComboWindow(0.3f, changedSpeedDuringAttack);

        Assert.That(expectedWindow, Is.EqualTo(0.15f).Within(0.0001f));
        Assert.That(unstableWindow, Is.Not.EqualTo(expectedWindow).Within(0.0001f));
    }

    [Test]
    public void AttackTiming_MatchesPlaybackSpeedToTargetDuration()
    {
        float targetDuration = AttackAnimationTiming.GetScaledAttackDuration(0.5f, 1f);
        float playbackSpeed = AttackAnimationTiming.GetPlaybackSpeed(1.25f, targetDuration, 1f);

        Assert.That(playbackSpeed, Is.EqualTo(2.5f).Within(0.0001f));
        Assert.That(1.25f / playbackSpeed, Is.EqualTo(targetDuration).Within(0.0001f));
    }

    [Test]
    public void AttackTiming_UsesFallbackSpeedWhenClipLengthIsUnknown()
    {
        Assert.That(AttackAnimationTiming.GetPlaybackSpeed(0f, 0.5f, 1.75f), Is.EqualTo(1.75f).Within(0.0001f));
        Assert.That(AttackAnimationTiming.GetPlaybackSpeed(1.25f, 0f, 1.75f), Is.EqualTo(1.75f).Within(0.0001f));
    }

    [Test]
    public void AttackTiming_RecomputesPlaybackSpeedPerComboStep()
    {
        float firstDuration = AttackAnimationTiming.GetScaledAttackDuration(0.5f, 1f);
        float secondDuration = AttackAnimationTiming.GetScaledAttackDuration(0.5f, 2f);
        float thirdDuration = AttackAnimationTiming.GetScaledAttackDuration(0.4f, 0.5f);

        Assert.That(AttackAnimationTiming.GetPlaybackSpeed(1.25f, firstDuration, 1f), Is.EqualTo(2.5f).Within(0.0001f));
        Assert.That(AttackAnimationTiming.GetPlaybackSpeed(1.25f, secondDuration, 2f), Is.EqualTo(5f).Within(0.0001f));
        Assert.That(AttackAnimationTiming.GetPlaybackSpeed(1.25f, thirdDuration, 0.5f), Is.EqualTo(1.5625f).Within(0.0001f));
    }

    [Test]
    public void AttackTiming_CurrentSwingStaysStableWhenAttackSpeedChangesMidSwing()
    {
        float speedAtAttackStart = 1f;
        float changedSpeedDuringAttack = 3f;
        float targetDuration = AttackAnimationTiming.GetScaledAttackDuration(0.5f, speedAtAttackStart);
        float playbackSpeed = AttackAnimationTiming.GetPlaybackSpeed(1.25f, targetDuration, speedAtAttackStart);

        Assert.That(playbackSpeed, Is.EqualTo(2.5f).Within(0.0001f));
        Assert.That(AttackAnimationTiming.GetScaledAttackDuration(0.5f, changedSpeedDuringAttack), Is.Not.EqualTo(targetDuration).Within(0.0001f));
        Assert.That(1.25f / playbackSpeed, Is.EqualTo(targetDuration).Within(0.0001f));
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
    public void StatusAilmentController_PetrifyVisual_OverridesAndRestoresMaterial()
    {
        GameObject player = new GameObject("Player");
        Material defaultMaterial = new Material(Shader.Find("Sprites/Default"));
        Material petrifyMaterial = new Material(Shader.Find("Sprites/Default"));

        try
        {
            SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = defaultMaterial;

            StatusAilmentController status = player.AddComponent<StatusAilmentController>();
            SetPrivateField(status, "petrifyMaterial", petrifyMaterial);

            status.ApplyPetrifyVisual(1f);

            Assert.That(renderer.sharedMaterial, Is.EqualTo(petrifyMaterial));

            status.ClearPetrifyVisual();

            Assert.That(renderer.sharedMaterial, Is.EqualTo(defaultMaterial));
        }
        finally
        {
            Object.DestroyImmediate(defaultMaterial);
            Object.DestroyImmediate(petrifyMaterial);
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void GorgonAnimatorController_IsConfiguredForCoreMotions()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Sprites/Enemies/Gorgon/Gorgon.controller");

        Assert.That(controller, Is.Not.Null);
        Assert.That(HasParameter(controller, "IsWalking", AnimatorControllerParameterType.Bool), Is.True);
        Assert.That(HasParameter(controller, "MeleeAttack", AnimatorControllerParameterType.Trigger), Is.True);
        Assert.That(HasParameter(controller, "RangedAttack", AnimatorControllerParameterType.Trigger), Is.True);
        Assert.That(HasParameter(controller, "Skill", AnimatorControllerParameterType.Trigger), Is.True);
        Assert.That(HasParameter(controller, "Die", AnimatorControllerParameterType.Trigger), Is.True);
        Assert.That(HasParameter(controller, "AttackEnded", AnimatorControllerParameterType.Trigger), Is.True);

        Assert.That(HasStateWithMotion(controller, "Idle"), Is.True);
        Assert.That(HasStateWithMotion(controller, "Walk"), Is.True);
        Assert.That(HasStateWithMotion(controller, "MeleeAttack"), Is.True);
        Assert.That(HasStateWithMotion(controller, "RangedAttack"), Is.True);
        Assert.That(HasStateWithMotion(controller, "Skill"), Is.True);
        Assert.That(HasStateWithMotion(controller, "Death"), Is.True);
    }

    [Test]
    public void MinotaurAnimatorController_IsConfiguredForCoreMotions()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Sprites/Enemies/Minot/Minotaur.controller");

        Assert.That(controller, Is.Not.Null);
        Assert.That(HasParameter(controller, "IsWalking", AnimatorControllerParameterType.Bool), Is.True);
        Assert.That(HasParameter(controller, "Skill", AnimatorControllerParameterType.Trigger), Is.True);
        Assert.That(HasParameter(controller, "MeleeAttack", AnimatorControllerParameterType.Trigger), Is.True);
        Assert.That(HasParameter(controller, "Charge", AnimatorControllerParameterType.Trigger), Is.True);
        Assert.That(HasParameter(controller, "Die", AnimatorControllerParameterType.Trigger), Is.True);
        Assert.That(HasParameter(controller, "AttackEnded", AnimatorControllerParameterType.Trigger), Is.True);

        Assert.That(HasStateWithMotion(controller, "Idle"), Is.True);
        Assert.That(HasStateWithMotion(controller, "Walk"), Is.True);
        Assert.That(HasStateWithMotion(controller, "Skill"), Is.True);
        Assert.That(HasStateWithMotion(controller, "MeleeAttack"), Is.True);
        Assert.That(HasStateWithMotion(controller, "Charge"), Is.True);
        Assert.That(HasStateWithMotion(controller, "Death"), Is.True);
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

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    private static bool HasParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        for (int i = 0; i < controller.parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = controller.parameters[i];
            if (parameter.name == name && parameter.type == type)
                return true;
        }

        return false;
    }

    private static bool HasState(AnimatorController controller, string stateName)
    {
        ChildAnimatorState[] states = controller.layers[0].stateMachine.states;
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i].state.name == stateName && states[i].state.motion is BlendTree)
                return true;
        }

        return false;
    }

    private static bool HasStateWithMotion(AnimatorController controller, string stateName)
    {
        ChildAnimatorState[] states = controller.layers[0].stateMachine.states;
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i].state.name == stateName && states[i].state.motion != null)
                return true;
        }

        return false;
    }

    private static bool BlendTreeHasSpriteClips(AnimatorController controller, string stateName)
    {
        BlendTree blendTree = GetStateBlendTree(controller, stateName);
        if (blendTree == null || blendTree.children.Length != 8)
            return false;

        for (int i = 0; i < blendTree.children.Length; i++)
        {
            if (blendTree.children[i].motion is not AnimationClip clip)
                return false;

            if (AnimationUtility.GetObjectReferenceCurveBindings(clip).Length == 0)
                return false;
        }

        return true;
    }

    private static BlendTree GetStateBlendTree(AnimatorController controller, string stateName)
    {
        ChildAnimatorState[] states = controller.layers[0].stateMachine.states;
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i].state.name == stateName)
                return states[i].state.motion as BlendTree;
        }

        return null;
    }

    private static float GetStateSpeed(AnimatorController controller, string stateName)
    {
        ChildAnimatorState[] states = controller.layers[0].stateMachine.states;
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i].state.name == stateName)
                return states[i].state.speed;
        }

        return 0f;
    }

    private static bool AttackStateReturnsImmediately(AnimatorController controller, string stateName)
    {
        ChildAnimatorState[] states = controller.layers[0].stateMachine.states;
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i].state.name != stateName)
                continue;

            AnimatorStateTransition[] transitions = states[i].state.transitions;
            if (transitions.Length < 2)
                return false;

            for (int j = 0; j < transitions.Length; j++)
            {
                if (!HasCondition(transitions[j], "AttackEnded", AnimatorConditionMode.If))
                    return false;

                if (transitions[j].hasExitTime || transitions[j].duration > 0f)
                    return false;
            }

            return true;
        }

        return false;
    }

    private static bool BlendTreeClipsLoop(AnimatorController controller, string stateName)
    {
        BlendTree blendTree = GetStateBlendTree(controller, stateName);
        if (blendTree == null)
            return false;

        for (int i = 0; i < blendTree.children.Length; i++)
        {
            if (blendTree.children[i].motion is not AnimationClip clip)
                return false;

            if (AnimationUtility.GetAnimationClipSettings(clip).loopTime)
                return true;
        }

        return false;
    }

    private static bool JumpStateReturnsOnlyAfterLanding(AnimatorController controller)
    {
        ChildAnimatorState[] states = controller.layers[0].stateMachine.states;
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i].state.name != "Jump_8Way")
                continue;

            AnimatorStateTransition[] transitions = states[i].state.transitions;
            for (int j = 0; j < transitions.Length; j++)
            {
                if (!HasCondition(transitions[j], "IsAirborne", AnimatorConditionMode.IfNot))
                    return false;
            }

            return transitions.Length > 0;
        }

        return false;
    }

    private static bool HasCondition(AnimatorStateTransition transition, string parameterName, AnimatorConditionMode mode)
    {
        for (int i = 0; i < transition.conditions.Length; i++)
        {
            AnimatorCondition condition = transition.conditions[i];
            if (condition.parameter == parameterName && condition.mode == mode)
                return true;
        }

        return false;
    }

    private static bool JumpStateReturnsImmediatelyAfterLanding(AnimatorController controller)
    {
        ChildAnimatorState[] states = controller.layers[0].stateMachine.states;
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i].state.name != "Jump_8Way")
                continue;

            AnimatorStateTransition[] transitions = states[i].state.transitions;
            for (int j = 0; j < transitions.Length; j++)
            {
                if (transitions[j].hasExitTime || transitions[j].duration > 0f)
                    return false;
            }

            return transitions.Length > 0;
        }

        return false;
    }
}
