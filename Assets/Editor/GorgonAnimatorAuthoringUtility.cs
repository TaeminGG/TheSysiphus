using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class GorgonAnimatorAuthoringUtility
{
    private const string ControllerPath = "Assets/Sprites/Enemies/Gorgon/Gorgon.controller";
    private const string PlaceholderFolder = "Assets/Sprites/Enemies/Gorgon/AnimatorPlaceholders";
    private const string IdleClipPath = "Assets/Sprites/Enemies/Gorgon/Gorgon_Idle.anim";

    [MenuItem("Tools/Enemies/Rebuild Gorgon Animator Controller")]
    public static void RebuildGorgonAnimatorController()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            throw new FileNotFoundException($"AnimatorController was not found at {ControllerPath}.");

        EnsureFolder(PlaceholderFolder);
        EnsureParameters(controller);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        ClearStateMachine(stateMachine);

        AnimatorState idle = CreateState(stateMachine, "Idle", LoadIdleClip(), new Vector3(240, 0, 0));
        AnimatorState walk = CreateState(stateMachine, "Walk", GetOrCreatePlaceholderClip("Gorgon_Walk", true), new Vector3(520, 0, 0));
        AnimatorState melee = CreateState(stateMachine, "MeleeAttack", GetOrCreatePlaceholderClip("Gorgon_MeleeAttack", false), new Vector3(240, 180, 0));
        AnimatorState ranged = CreateState(stateMachine, "RangedAttack", GetOrCreatePlaceholderClip("Gorgon_RangedAttack", false), new Vector3(520, 180, 0));
        AnimatorState skill = CreateState(stateMachine, "Skill", GetOrCreatePlaceholderClip("Gorgon_Skill", false), new Vector3(800, 180, 0));
        AnimatorState death = CreateState(stateMachine, "Death", GetOrCreatePlaceholderClip("Gorgon_Death", false), new Vector3(520, 360, 0));

        stateMachine.defaultState = idle;

        AddTransition(idle, walk, AnimatorConditionMode.If, "IsWalking");
        AddTransition(walk, idle, AnimatorConditionMode.IfNot, "IsWalking");
        AddAnyStateTriggerTransition(stateMachine, melee, "MeleeAttack");
        AddAnyStateTriggerTransition(stateMachine, ranged, "RangedAttack");
        AddAnyStateTriggerTransition(stateMachine, skill, "Skill");
        AddAnyStateTriggerTransition(stateMachine, death, "Die");
        AddAttackReturnTransitions(melee, idle, walk);
        AddAttackReturnTransitions(ranged, idle, walk);
        AddAttackReturnTransitions(skill, idle, walk);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void RebuildGorgonAnimator()
    {
        RebuildGorgonAnimatorController();
    }

    private static void EnsureParameters(AnimatorController controller)
    {
        EnsureParameter(controller, "IsWalking", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "isWalking", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "MeleeAttack", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "RangedAttack", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Skill", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Die", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "AttackEnded", AnimatorControllerParameterType.Trigger);
    }

    private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        for (int i = 0; i < controller.parameters.Length; i++)
        {
            if (controller.parameters[i].name == name)
                return;
        }

        controller.AddParameter(name, type);
    }

    private static void ClearStateMachine(AnimatorStateMachine stateMachine)
    {
        ChildAnimatorState[] states = stateMachine.states;
        for (int i = 0; i < states.Length; i++)
            stateMachine.RemoveState(states[i].state);

        ChildAnimatorStateMachine[] machines = stateMachine.stateMachines;
        for (int i = 0; i < machines.Length; i++)
            stateMachine.RemoveStateMachine(machines[i].stateMachine);

        AnimatorStateTransition[] anyTransitions = stateMachine.anyStateTransitions;
        for (int i = 0; i < anyTransitions.Length; i++)
            stateMachine.RemoveAnyStateTransition(anyTransitions[i]);

        AnimatorTransition[] entryTransitions = stateMachine.entryTransitions;
        for (int i = 0; i < entryTransitions.Length; i++)
            stateMachine.RemoveEntryTransition(entryTransitions[i]);
    }

    private static AnimatorState CreateState(AnimatorStateMachine stateMachine, string stateName, Motion motion, Vector3 position)
    {
        AnimatorState state = stateMachine.AddState(stateName, position);
        state.motion = motion;
        state.speed = 1f;
        return state;
    }

    private static AnimationClip LoadIdleClip()
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
        if (clip != null)
            return clip;

        return GetOrCreatePlaceholderClip("Gorgon_Idle", true);
    }

    private static AnimationClip GetOrCreatePlaceholderClip(string clipName, bool loop)
    {
        string clipPath = $"{PlaceholderFolder}/{clipName}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip
            {
                name = Path.GetFileNameWithoutExtension(clipPath),
                frameRate = 12f
            };

            AssetDatabase.CreateAsset(clip, clipPath);
        }

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void AddAnyStateTriggerTransition(AnimatorStateMachine stateMachine, AnimatorState destination, string trigger)
    {
        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(destination);
        ConfigureImmediateTransition(transition);
        transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
    }

    private static void AddTransition(AnimatorState source, AnimatorState destination, AnimatorConditionMode conditionMode, string parameter)
    {
        AnimatorStateTransition transition = source.AddTransition(destination);
        ConfigureImmediateTransition(transition);
        transition.AddCondition(conditionMode, 0f, parameter);
    }

    private static void AddAttackReturnTransitions(AnimatorState source, AnimatorState idle, AnimatorState walk)
    {
        AnimatorStateTransition toWalk = source.AddTransition(walk);
        ConfigureImmediateTransition(toWalk);
        toWalk.AddCondition(AnimatorConditionMode.If, 0f, "AttackEnded");
        toWalk.AddCondition(AnimatorConditionMode.If, 0f, "IsWalking");

        AnimatorStateTransition toIdle = source.AddTransition(idle);
        ConfigureImmediateTransition(toIdle);
        toIdle.AddCondition(AnimatorConditionMode.If, 0f, "AttackEnded");
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsWalking");
    }

    private static void ConfigureImmediateTransition(AnimatorStateTransition transition)
    {
        transition.hasExitTime = false;
        transition.exitTime = 0f;
        transition.duration = 0f;
        transition.hasFixedDuration = true;
        transition.canTransitionToSelf = false;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);

            current = next;
        }
    }
}
