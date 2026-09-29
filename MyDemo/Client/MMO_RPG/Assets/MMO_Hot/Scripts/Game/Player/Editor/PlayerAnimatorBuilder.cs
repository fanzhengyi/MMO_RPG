using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>创建与 PlayerAnimator 匹配的动画图，并复用项目中已有的角色动画。</summary>
public static class PlayerAnimatorBuilder
{
    public const string DefaultPath = "Assets/MMO_Hot/ArtRes/Animas/AnimatorController/Player.controller";
    private const string ClipRoot = "Assets/MMO_Hot/ArtRes/Animas/Role/JX/";

    /// <summary>创建新的 Controller；存在同名文件时自动生成新路径。</summary>
    [MenuItem("Tools/Player/Create Animator Controller")]
    public static void Create()
    {
        var path = AssetDatabase.GenerateUniqueAssetPath(DefaultPath);
        var controller = Build(path);
        Selection.activeObject = controller;
        EditorGUIUtility.PingObject(controller);
        Debug.Log("已生成角色 Animator。请补 Walk、Attack4、Dead 的 Motion，并校准角色动作 Duration：" + path);
    }

    /// <summary>建立移动混合树、空中状态、四段普攻、四技能及全部条件连线。</summary>
    public static AnimatorController Build(string path)
    {
        if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            throw new System.InvalidOperationException("目标 Animator 已存在：" + path);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter(PlayerAnimator.StateParameter, AnimatorControllerParameterType.Int);
        controller.AddParameter(PlayerAnimator.SpeedParameter, AnimatorControllerParameterType.Float);
        controller.AddParameter(PlayerAnimator.ComboParameter, AnimatorControllerParameterType.Int);
        controller.AddParameter(PlayerAnimator.SkillParameter, AnimatorControllerParameterType.Int);
        var graph = controller.layers[0].stateMachine;
        graph.anyStatePosition = new Vector3(0f, -120f);
        graph.entryPosition = new Vector3(0f, 0f);

        var locomotion = graph.AddState("Locomotion", new Vector3(260f, 0f));
        locomotion.writeDefaultValues = false;
        var tree = new BlendTree
        {
            name = "Locomotion", blendType = BlendTreeType.Simple1D,
            blendParameter = PlayerAnimator.SpeedParameter, useAutomaticThresholds = false
        };
        AssetDatabase.AddObjectToAsset(tree, controller);
        tree.AddChild(LoadClip("Common/Anim_JX_Idle_1.fbx"), 0f);
        tree.AddChild(null, 4f);
        tree.AddChild(LoadClip("Common/Anim_JX_Run_F.FBX"), 6.5f);
        locomotion.motion = tree;
        graph.defaultState = locomotion;
        // Any State 按列表顺序检查，死亡需要能够中断所有动作过渡。
        AddState(graph, "Dead", PlayerStateType.Dead, null, 260f, 300f);
        AddTransition(graph, locomotion, PlayerStateType.Idle);
        AddTransition(graph, locomotion, PlayerStateType.Move);
        AddTransition(graph, locomotion, PlayerStateType.Run);

        AddState(graph, "Jump", PlayerStateType.Jump, "Common/Anim_JX_Jump_Start.fbx", 260f, 100f);
        AddState(graph, "Fall", PlayerStateType.Fall, "Common/Anim_JX_Jump_Loop.FBX", 260f, 200f);

        for (var i = 1; i <= PlayerSettings.AttackCount; i++)
            AddState(graph, "Attack" + i, PlayerStateType.Attack,
                i < 4 ? "Atk/Anim_JX_Atk0" + i + ".FBX" : null,
                580f, (i - 1) * 100f, PlayerAnimator.ComboParameter, i);
        for (var i = 1; i <= PlayerSettings.SkillCount; i++)
            AddState(graph, "Skill" + i, PlayerStateType.Skill,
                "Atk/Anim_JX_Skill0" + i + (i == 4 ? ".fbx" : ".FBX"),
                900f, (i - 1) * 100f, PlayerAnimator.SkillParameter, i);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    /// <summary>从 FBX 子资源中读取实际动画，排除 Unity 的预览片段。</summary>
    private static AnimationClip LoadClip(string relativePath)
    {
        if (relativePath == null)
            return null;
        return AssetDatabase.LoadAllAssetsAtPath(ClipRoot + relativePath)
            .OfType<AnimationClip>().FirstOrDefault(clip => !clip.name.StartsWith("__preview__"));
    }

    /// <summary>添加一个动画状态及其进入条件。</summary>
    private static void AddState(AnimatorStateMachine graph, string name, PlayerStateType type,
        string clipPath, float x, float y, string indexParameter = null, int index = 0)
    {
        var state = graph.AddState(name, new Vector3(x, y));
        state.motion = LoadClip(clipPath);
        state.writeDefaultValues = false;
        var transition = AddTransition(graph, state, type);
        if (indexParameter != null)
            transition.AddCondition(AnimatorConditionMode.Equals, index, indexParameter);
    }

    /// <summary>由代码状态驱动切换，不等待动画退出时间，也不重复进入自身。</summary>
    private static AnimatorStateTransition AddTransition(AnimatorStateMachine graph,
        AnimatorState target, PlayerStateType type)
    {
        var transition = graph.AddAnyStateTransition(target);
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = 0.06f;
        transition.canTransitionToSelf = false;
        transition.interruptionSource = TransitionInterruptionSource.SourceThenDestination;
        transition.orderedInterruption = true;
        transition.AddCondition(AnimatorConditionMode.Equals, (int)type, PlayerAnimator.StateParameter);
        return transition;
    }
}
