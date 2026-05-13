using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class PlayerAnimatorAuthoringUtility
{
    private const string ControllerPath = "Assets/Sprites/Player/PlayerAnim.controller";
    private const string PlaceholderFolder = "Assets/Sprites/Player/AnimatorPlaceholders";
    private const string PlayerSpriteFolder = "Assets/Sprites/Player/sisi";
    private const string BlendParameter = "Direction";

    private static readonly DirectionSlot[] Directions =
    {
        new DirectionSlot("S", 0, 5),
        new DirectionSlot("SE", 1, 6),
        new DirectionSlot("E", 2, 7),
        new DirectionSlot("NE", 3, 0),
        new DirectionSlot("N", 4, 1),
        new DirectionSlot("NW", 5, 2),
        new DirectionSlot("W", 6, 3),
        new DirectionSlot("SW", 7, 4)
    };

    private static readonly Dictionary<string, ClipSource> ClipSources = new Dictionary<string, ClipSource>
    {
        { "Idle", new ClipSource($"{PlayerSpriteFolder}/Idle.png", "Idle", 12f, true) },
        { "Run", new ClipSource($"{PlayerSpriteFolder}/Run.png", "Run", 12f, true) },
        { "Jump", new ClipSource($"{PlayerSpriteFolder}/Special1.png", "Special1", 12f, false) },
        { "Attack1", new ClipSource($"{PlayerSpriteFolder}/Attack1.png", "Attack1", 12f, false) },
        { "Attack2", new ClipSource($"{PlayerSpriteFolder}/Attack2.png", "Attack2", 12f, false) },
        { "Attack3", new ClipSource($"{PlayerSpriteFolder}/Attack3.png", "Attack3", 12f, false) }
    };

    [MenuItem("Tools/Player/Rebuild 8-Way Animator Controller")]
    public static void RebuildPlayerAnimatorController()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            throw new FileNotFoundException($"AnimatorController was not found at {ControllerPath}.");

        EnsureFolder(PlaceholderFolder);
        EnsureParameters(controller);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        ClearStateMachine(stateMachine);

        AnimatorState idle = CreateBlendTreeState(controller, stateMachine, "Idle_8Way", "Idle", new Vector3(240, 0, 0), false);
        AnimatorState run = CreateBlendTreeState(controller, stateMachine, "Run_8Way", "Run", new Vector3(520, 0, 0), false);
        AnimatorState jump = CreateBlendTreeState(controller, stateMachine, "Jump_8Way", "Jump", new Vector3(240, 180, 0), false);
        AnimatorState attack1 = CreateBlendTreeState(controller, stateMachine, "Attack1_8Way", "Attack1", new Vector3(240, 380, 0), true);
        AnimatorState attack2 = CreateBlendTreeState(controller, stateMachine, "Attack2_8Way", "Attack2", new Vector3(520, 380, 0), true);
        AnimatorState attack3 = CreateBlendTreeState(controller, stateMachine, "Attack3_8Way", "Attack3", new Vector3(800, 380, 0), true);

        stateMachine.defaultState = idle;

        AddTransition(idle, run, false, 0f, AnimatorConditionMode.If, 0f, "IsMoving");
        AddTransition(run, idle, false, 0f, AnimatorConditionMode.IfNot, 0f, "IsMoving");

        AddAnyStateTransition(stateMachine, jump, "Jump");
        AddAnyStateTransition(stateMachine, attack1, "shackles_attack_1");
        AddAnyStateTransition(stateMachine, attack2, "shackles_attack_2");
        AddAnyStateTransition(stateMachine, attack3, "shackles_attack_3");

        AddReturnTransitions(jump, idle, run, "IsMoving");
        AddAttackReturnTransitions(attack1, idle, run);
        AddAttackReturnTransitions(attack2, idle, run);
        AddAttackReturnTransitions(attack3, idle, run);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void RebuildPlayer8WayAnimator()
    {
        RebuildPlayerAnimatorController();
    }

    private static void EnsureParameters(AnimatorController controller)
    {
        EnsureParameter(controller, "Look", AnimatorControllerParameterType.Int);
        EnsureParameter(controller, "Direction", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "DirectionX", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "DirectionY", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "MoveX", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "MoveY", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "IsGrounded", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "IsAirborne", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "IsDashing", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "AttackSpeed", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "Jump", AnimatorControllerParameterType.Trigger);
        RemoveParameter(controller, "Dash");
        EnsureParameter(controller, "AttackEnded", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "shackles_attack_1", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "shackles_attack_2", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "shackles_attack_3", AnimatorControllerParameterType.Trigger);
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

    private static void RemoveParameter(AnimatorController controller, string name)
    {
        AnimatorControllerParameter[] parameters = controller.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name != name)
                continue;

            controller.RemoveParameter(parameters[i]);
            return;
        }
    }

    private static void ClearStateMachine(AnimatorStateMachine stateMachine)
    {
        ChildAnimatorState[] states = stateMachine.states;
        for (int i = 0; i < states.Length; i++)
        {
            stateMachine.RemoveState(states[i].state);
        }

        ChildAnimatorStateMachine[] machines = stateMachine.stateMachines;
        for (int i = 0; i < machines.Length; i++)
        {
            stateMachine.RemoveStateMachine(machines[i].stateMachine);
        }

        AnimatorStateTransition[] anyTransitions = stateMachine.anyStateTransitions;
        for (int i = 0; i < anyTransitions.Length; i++)
        {
            stateMachine.RemoveAnyStateTransition(anyTransitions[i]);
        }

        AnimatorTransition[] entryTransitions = stateMachine.entryTransitions;
        for (int i = 0; i < entryTransitions.Length; i++)
        {
            stateMachine.RemoveEntryTransition(entryTransitions[i]);
        }
    }

    private static AnimatorState CreateBlendTreeState(
        AnimatorController controller,
        AnimatorStateMachine stateMachine,
        string stateName,
        string motionName,
        Vector3 position,
        bool useAttackSpeed)
    {
        BlendTree blendTree;
        AnimatorState state = controller.CreateBlendTreeInController(stateName, out blendTree, 0);
        MoveState(stateMachine, state, position);
        state.speed = 1f;

        blendTree.name = $"{stateName}_BlendTree";
        blendTree.blendType = BlendTreeType.Simple1D;
        blendTree.blendParameter = BlendParameter;
        blendTree.useAutomaticThresholds = false;

        for (int i = 0; i < Directions.Length; i++)
        {
            DirectionSlot direction = Directions[i];
            blendTree.AddChild(GetOrCreateSpriteClip(motionName, direction.Name, direction.SpriteSheetRow), direction.Threshold);
        }

        if (useAttackSpeed)
        {
            state.speedParameter = "AttackSpeed";
            state.speedParameterActive = true;
        }

        return state;
    }

    private static void MoveState(AnimatorStateMachine stateMachine, AnimatorState state, Vector3 position)
    {
        ChildAnimatorState[] states = stateMachine.states;
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i].state != state)
                continue;

            states[i].position = position;
            stateMachine.states = states;
            return;
        }
    }

    private static AnimationClip GetOrCreateSpriteClip(string motionName, string directionName, int directionIndex)
    {
        string clipPath = $"{PlaceholderFolder}/Player_{motionName}_{directionName}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip
            {
                name = Path.GetFileNameWithoutExtension(clipPath)
            };

            AssetDatabase.CreateAsset(clip, clipPath);
        }

        if (!ClipSources.TryGetValue(motionName, out ClipSource source))
            throw new KeyNotFoundException($"No sprite source is configured for player motion '{motionName}'.");

        Sprite[] sprites = LoadDirectionSprites(source, directionIndex);
        if (sprites.Length == 0)
            throw new FileNotFoundException($"No sprites were found for {motionName} {directionName} in {source.TexturePath}.");

        ApplySpriteFrames(clip, sprites, source.FrameRate, source.Loop);
        return clip;
    }

    private static Sprite[] LoadDirectionSprites(ClipSource source, int directionIndex)
    {
        Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(source.TexturePath);
        List<Sprite> sprites = new List<Sprite>();
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
                sprites.Add(sprite);
        }

        if (sprites.Count == 0)
            return System.Array.Empty<Sprite>();

        List<Sprite> orderedSprites = sprites
            .Where(sprite => TryGetSpriteIndex(sprite.name, source.SpriteNamePrefix, out _))
            .OrderBy(sprite =>
            {
                TryGetSpriteIndex(sprite.name, source.SpriteNamePrefix, out int index);
                return index;
            })
            .ToList();

        int framesPerDirection = Mathf.FloorToInt(orderedSprites.Count / 8f);
        if (framesPerDirection <= 0)
            return System.Array.Empty<Sprite>();

        int topOrderRow = 7 - directionIndex;
        int start = topOrderRow * framesPerDirection;
        if (start < 0 || start + framesPerDirection > orderedSprites.Count)
            return System.Array.Empty<Sprite>();

        return orderedSprites
            .Skip(start)
            .Take(framesPerDirection)
            .ToArray();
    }

    private static bool TryGetSpriteIndex(string spriteName, string prefix, out int index)
    {
        index = 0;
        string expectedPrefix = $"{prefix}_";
        if (spriteName.StartsWith(expectedPrefix, System.StringComparison.Ordinal))
            return int.TryParse(spriteName.Substring(expectedPrefix.Length), out index);

        string alternatePrefix = $"{prefix} ";
        if (!spriteName.StartsWith(alternatePrefix, System.StringComparison.Ordinal))
            return false;

        int separatorIndex = spriteName.LastIndexOf('_');
        if (separatorIndex < 0 || separatorIndex >= spriteName.Length - 1)
            return false;

        return int.TryParse(spriteName.Substring(separatorIndex + 1), out index);
    }

    private static void ApplySpriteFrames(AnimationClip clip, Sprite[] sprites, float frameRate, bool loop)
    {
        clip.ClearCurves();
        clip.frameRate = frameRate;

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = sprites[i]
            };
        }

        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
    }

    private static void AddAnyStateTransition(AnimatorStateMachine stateMachine, AnimatorState destination, string trigger)
    {
        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(destination);
        ConfigureImmediateTransition(transition);
        transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
    }

    private static void AddReturnTransitions(AnimatorState source, AnimatorState idle, AnimatorState run, string movingParameter)
    {
        AnimatorStateTransition toRun = source.AddTransition(run);
        ConfigureImmediateTransition(toRun);
        toRun.AddCondition(AnimatorConditionMode.If, 0f, movingParameter);
        toRun.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsAirborne");

        AnimatorStateTransition toIdle = source.AddTransition(idle);
        ConfigureImmediateTransition(toIdle);
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, movingParameter);
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsAirborne");
    }

    private static void AddAttackReturnTransitions(AnimatorState source, AnimatorState idle, AnimatorState run)
    {
        AnimatorStateTransition toRun = source.AddTransition(run);
        ConfigureImmediateTransition(toRun);
        toRun.AddCondition(AnimatorConditionMode.If, 0f, "AttackEnded");
        toRun.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");

        AnimatorStateTransition toIdle = source.AddTransition(idle);
        ConfigureImmediateTransition(toIdle);
        toIdle.AddCondition(AnimatorConditionMode.If, 0f, "AttackEnded");
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");
    }

    private static void AddTransition(
        AnimatorState source,
        AnimatorState destination,
        bool hasExitTime,
        float exitTime,
        AnimatorConditionMode conditionMode,
        float threshold,
        string parameter)
    {
        AnimatorStateTransition transition = source.AddTransition(destination);
        transition.hasExitTime = hasExitTime;
        transition.exitTime = exitTime;
        transition.duration = 0f;
        transition.hasFixedDuration = true;
        transition.canTransitionToSelf = false;
        transition.AddCondition(conditionMode, threshold, parameter);
    }

    private static void ConfigureImmediateTransition(AnimatorStateTransition transition)
    {
        transition.hasExitTime = false;
        transition.exitTime = 0f;
        transition.duration = 0f;
        transition.hasFixedDuration = true;
        transition.canTransitionToSelf = false;
    }

    private static void ConfigureExitTransition(AnimatorStateTransition transition, float exitTime)
    {
        transition.hasExitTime = true;
        transition.exitTime = exitTime;
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

    private readonly struct DirectionSlot
    {
        public DirectionSlot(string name, float threshold, int spriteSheetRow)
        {
            Name = name;
            Threshold = threshold;
            SpriteSheetRow = spriteSheetRow;
        }

        public string Name { get; }
        public float Threshold { get; }
        public int SpriteSheetRow { get; }
    }

    private readonly struct ClipSource
    {
        public ClipSource(string texturePath, string spriteNamePrefix, float frameRate, bool loop)
        {
            TexturePath = texturePath;
            SpriteNamePrefix = spriteNamePrefix;
            FrameRate = frameRate;
            Loop = loop;
        }

        public string TexturePath { get; }
        public string SpriteNamePrefix { get; }
        public float FrameRate { get; }
        public bool Loop { get; }
    }
}
