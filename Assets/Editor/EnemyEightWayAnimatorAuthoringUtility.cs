using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class EnemyEightWayAnimatorAuthoringUtility
{
    private const string EnemyPrefabFolder = "Assets/Prefabs/Enemies";
    private const string EnemySpriteRoot = "Assets/Sprites/Enemies";
    private const string DirectionParameter = "Direction";
    private const int FramesPerDirection = 15;

    private static readonly DirectionSlot[] Directions =
    {
        new DirectionSlot("E", 0f, 0),
        new DirectionSlot("SE", 1f, 1),
        new DirectionSlot("S", 2f, 2),
        new DirectionSlot("SW", 3f, 3),
        new DirectionSlot("W", 4f, 4),
        new DirectionSlot("NW", 5f, 5),
        new DirectionSlot("N", 6f, 6),
        new DirectionSlot("NE", 7f, 7)
    };

    private static readonly EnemyDefinition[] Enemies =
    {
        new EnemyDefinition("Skeleton", "Skeleton"),
        new EnemyDefinition("SkeletonArcher", "SkeletonArchor"),
        new EnemyDefinition("Spirit", "Spirit"),
        new EnemyDefinition("Specter", "Specter"),
        new EnemyDefinition("Swamp", "Swamp"),
        new EnemyDefinition("Kampe", "Kampe"),
        new EnemyDefinition("Boomer", "Boomer"),
        new EnemyDefinition("FireElemental", "FireElemental")
    };

    private static readonly Dictionary<string, string[]> MotionAliases = new Dictionary<string, string[]>
    {
        { "Idle", new[] { "{0}_Idle", "Idle", "Idle3", "Idle-Sheet" } },
        { "Walk", new[] { "{0}_Walk", "Walk", "Run", "{0}_Run" } },
        { "Attack", new[] { "{0}_Attack", "Attack", "Attack1", "{0}_Attack1" } },
        { "Death", new[] { "{0}_Death", "Death", "Die", "{0}_Die" } }
    };

    [MenuItem("Tools/Enemies/Rebuild Selected 8-Way Enemy Animators")]
    public static void RebuildSelectedEnemyAnimators()
    {
        for (int i = 0; i < Enemies.Length; i++)
            RebuildEnemy(Enemies[i]);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void RebuildSelectedEightWayEnemyAnimators()
    {
        RebuildSelectedEnemyAnimators();
    }

    private static void RebuildEnemy(EnemyDefinition enemy)
    {
        string spriteFolder = $"{EnemySpriteRoot}/{enemy.SpriteFolderName}";
        EnsureFolder(spriteFolder);
        EnsureFolder($"{spriteFolder}/AnimatorPlaceholders");

        string controllerPath = $"{spriteFolder}/{enemy.PrefabName}.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        EnsureParameters(controller);
        RebuildController(controller, enemy, spriteFolder);
        ApplyPrefabSettings(enemy, controller);
    }

    private static void EnsureParameters(AnimatorController controller)
    {
        EnsureParameter(controller, DirectionParameter, AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "IsWalking", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "isWalking", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "AttackEnded", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Die", AnimatorControllerParameterType.Trigger);
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

    private static void RebuildController(AnimatorController controller, EnemyDefinition enemy, string spriteFolder)
    {
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        ClearStateMachine(stateMachine);

        AnimatorState idle = CreateBlendTreeState(controller, stateMachine, enemy, spriteFolder, "Idle", new Vector3(240, 0, 0), true);
        AnimatorState walk = CreateBlendTreeState(controller, stateMachine, enemy, spriteFolder, "Walk", new Vector3(520, 0, 0), true);
        AnimatorState attack = CreateBlendTreeState(controller, stateMachine, enemy, spriteFolder, "Attack", new Vector3(240, 180, 0), false);
        AnimatorState death = CreateBlendTreeState(controller, stateMachine, enemy, spriteFolder, "Death", new Vector3(520, 180, 0), false);

        stateMachine.defaultState = idle;

        AddWalkingTransitions(idle, walk);
        AddWalkingTransitions(walk, idle, reverse: true);
        AddAnyStateTriggerTransition(stateMachine, attack, "Attack");
        AddAnyStateTriggerTransition(stateMachine, death, "Die");
        AddAttackReturnTransitions(attack, idle, walk);

        EditorUtility.SetDirty(controller);
    }

    private static AnimatorState CreateBlendTreeState(
        AnimatorController controller,
        AnimatorStateMachine stateMachine,
        EnemyDefinition enemy,
        string spriteFolder,
        string motionName,
        Vector3 position,
        bool loop)
    {
        BlendTree blendTree;
        AnimatorState state = controller.CreateBlendTreeInController($"{motionName}_8Way", out blendTree, 0);
        MoveState(stateMachine, state, position);

        blendTree.name = $"{motionName}_8Way_BlendTree";
        blendTree.blendType = BlendTreeType.Simple1D;
        blendTree.blendParameter = DirectionParameter;
        blendTree.useAutomaticThresholds = false;

        for (int i = 0; i < Directions.Length; i++)
        {
            DirectionSlot direction = Directions[i];
            blendTree.AddChild(GetOrCreateClip(enemy, spriteFolder, motionName, direction, loop), direction.Threshold);
        }

        return state;
    }

    private static AnimationClip GetOrCreateClip(EnemyDefinition enemy, string spriteFolder, string motionName, DirectionSlot direction, bool loop)
    {
        string clipPath = $"{spriteFolder}/AnimatorPlaceholders/{enemy.PrefabName}_{motionName}_{direction.Name}.anim";
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

        Sprite[] sprites = LoadMotionDirectionSprites(enemy, spriteFolder, motionName, direction.SpriteSheetBlock);
        if (sprites.Length > 0)
            ApplySpriteFrames(clip, sprites, 12f, loop);
        else
            ApplyLoopSetting(clip, loop);

        return clip;
    }

    private static Sprite[] LoadMotionDirectionSprites(EnemyDefinition enemy, string spriteFolder, string motionName, int directionBlock)
    {
        string texturePath = FindMotionTexture(enemy, spriteFolder, motionName);
        if (string.IsNullOrEmpty(texturePath))
            return System.Array.Empty<Sprite>();

        EnsureMultipleSpriteSheet(texturePath);

        List<Sprite> sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(texturePath)
            .OfType<Sprite>()
            .OrderBy(sprite => ResolveSpriteIndex(sprite.name))
            .ToList();

        int start = directionBlock * FramesPerDirection;
        if (start + FramesPerDirection > sprites.Count)
            return System.Array.Empty<Sprite>();

        return sprites.Skip(start).Take(FramesPerDirection).ToArray();
    }

    private static string FindMotionTexture(EnemyDefinition enemy, string spriteFolder, string motionName)
    {
        if (!MotionAliases.TryGetValue(motionName, out string[] aliases))
            return null;

        for (int i = 0; i < aliases.Length; i++)
        {
            string baseName = string.Format(aliases[i], enemy.PrefabName);
            string path = $"{spriteFolder}/{baseName}.png";
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    private static void EnsureMultipleSpriteSheet(string texturePath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
            return;

        if (AssetDatabase.LoadAllAssetRepresentationsAtPath(texturePath).OfType<Sprite>().Count() >= FramesPerDirection * 8)
            return;

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null || texture.width % FramesPerDirection != 0 || texture.height % 8 != 0)
            return;

        int cellWidth = texture.width / FramesPerDirection;
        int cellHeight = texture.height / 8;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.spritePixelsPerUnit = 100f;
        importer.SaveAndReimport();

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        if (dataProvider == null)
            return;

        dataProvider.InitSpriteEditorDataProvider();

        List<SpriteRect> spriteRects = new List<SpriteRect>();
        List<SpriteNameFileIdPair> nameFileIdPairs = new List<SpriteNameFileIdPair>();
        string prefix = Path.GetFileNameWithoutExtension(texturePath);

        for (int row = 0; row < 8; row++)
        {
            for (int column = 0; column < FramesPerDirection; column++)
            {
                int index = row * FramesPerDirection + column;
                GUID spriteId = GUID.Generate();
                string spriteName = $"{prefix}_{index}";
                spriteRects.Add(new SpriteRect
                {
                    name = spriteName,
                    rect = new Rect(column * cellWidth, texture.height - ((row + 1) * cellHeight), cellWidth, cellHeight),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                });
                spriteRects[spriteRects.Count - 1].spriteID = spriteId;
                nameFileIdPairs.Add(new SpriteNameFileIdPair(spriteName, spriteId));
            }
        }

        dataProvider.SetSpriteRects(spriteRects.ToArray());
        ISpriteNameFileIdDataProvider nameFileIdDataProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        nameFileIdDataProvider?.SetNameFileIdPairs(nameFileIdPairs);
        dataProvider.Apply();
        importer.SaveAndReimport();
    }

    private static int ResolveSpriteIndex(string spriteName)
    {
        int separator = spriteName.LastIndexOf('_');
        if (separator >= 0 && separator < spriteName.Length - 1 && int.TryParse(spriteName.Substring(separator + 1), out int index))
            return index;

        string digits = new string(spriteName.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
        return int.TryParse(digits, out index) ? index : 0;
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

        AnimationUtility.SetObjectReferenceCurve(clip, new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        }, keyframes);

        ApplyLoopSetting(clip, loop);
    }

    private static void ApplyLoopSetting(AnimationClip clip, bool loop)
    {
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
    }

    private static void ApplyPrefabSettings(EnemyDefinition enemy, AnimatorController controller)
    {
        string prefabPath = $"{EnemyPrefabFolder}/{enemy.PrefabName}.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            return;

        GameObject instance = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Animator animator = instance.GetComponentInChildren<Animator>(true);
            if (animator != null)
                animator.runtimeAnimatorController = controller;

            EnemyDirectionalAnimator directionalAnimator = instance.GetComponent<EnemyDirectionalAnimator>();
            if (directionalAnimator == null)
                directionalAnimator = instance.AddComponent<EnemyDirectionalAnimator>();

            Transform sprite = animator != null ? animator.transform : null;
            SerializedObject serializedAnimator = new SerializedObject(directionalAnimator);
            serializedAnimator.FindProperty("sprite").objectReferenceValue = sprite;
            serializedAnimator.FindProperty("clearSpriteFlip").boolValue = true;
            serializedAnimator.ApplyModifiedPropertiesWithoutUndo();

            EnemyStats stats = instance.GetComponent<EnemyStats>();
            if (stats != null && animator != null)
            {
                stats.deathAnimator = animator;
                stats.deathAnimationTrigger = "Die";
                stats.deathAnimationStateName = "Death_8Way";
                if (stats.deathDestroyDelay <= 0f)
                    stats.deathDestroyDelay = 1.25f;
                EditorUtility.SetDirty(stats);
            }

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(instance);
        }
    }

    private static void AddWalkingTransitions(AnimatorState source, AnimatorState destination, bool reverse = false)
    {
        AddBoolTransition(source, destination, reverse ? AnimatorConditionMode.IfNot : AnimatorConditionMode.If, "IsWalking");
        AddBoolTransition(source, destination, reverse ? AnimatorConditionMode.IfNot : AnimatorConditionMode.If, "isWalking");
    }

    private static void AddBoolTransition(AnimatorState source, AnimatorState destination, AnimatorConditionMode mode, string parameter)
    {
        AnimatorStateTransition transition = source.AddTransition(destination);
        ConfigureImmediateTransition(transition);
        transition.AddCondition(mode, 0f, parameter);
    }

    private static void AddAnyStateTriggerTransition(AnimatorStateMachine stateMachine, AnimatorState destination, string trigger)
    {
        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(destination);
        ConfigureImmediateTransition(transition);
        transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
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
        public DirectionSlot(string name, float threshold, int spriteSheetBlock)
        {
            Name = name;
            Threshold = threshold;
            SpriteSheetBlock = spriteSheetBlock;
        }

        public string Name { get; }
        public float Threshold { get; }
        public int SpriteSheetBlock { get; }
    }

    private readonly struct EnemyDefinition
    {
        public EnemyDefinition(string prefabName, string spriteFolderName)
        {
            PrefabName = prefabName;
            SpriteFolderName = spriteFolderName;
        }

        public string PrefabName { get; }
        public string SpriteFolderName { get; }
    }
}
