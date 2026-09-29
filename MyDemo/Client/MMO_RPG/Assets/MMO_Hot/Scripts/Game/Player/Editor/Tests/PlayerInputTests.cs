using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

/// <summary>验证真实 Input Actions 的采样和组件生命周期。</summary>
public sealed class PlayerInputTests
{
    private GameObject actor;
    private PlayerInputSource source;
    private InputActionAsset asset;
    private Keyboard keyboard;
    private Mouse mouse;
    private readonly List<InputActionReference> references = new List<InputActionReference>();
    private InputSettings.UpdateMode previousUpdateMode;
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
    private InputSettings.BackgroundBehavior previousBackground;

    /// <summary>加载项目输入配置并创建专用测试设备。</summary>
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return new EnterPlayMode();
        Assert.IsTrue(Application.isPlaying, "输入集成测试必须处于 Play Mode。");
        previousUpdateMode = InputSystem.settings.updateMode;
        previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        previousBackground = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        keyboard = InputSystem.AddDevice<Keyboard>();
        mouse = InputSystem.AddDevice<Mouse>();
        asset = Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>(
            "Assets/MMO_Hot/InputSystem/PlayerInput.inputactions"));
        Assert.IsNotNull(asset);
        actor = new GameObject("PlayerInputTest");
        actor.SetActive(false);
        source = actor.AddComponent<PlayerInputSource>();
        var serialized = new SerializedObject(source);
        Assign(serialized.FindProperty("moveAction"), "Move");
        Assign(serialized.FindProperty("runAction"), "Run");
        Assign(serialized.FindProperty("jumpAction"), "Jump");
        Assign(serialized.FindProperty("attackAction"), "Attack");
        var skills = serialized.FindProperty("skillActions");
        for (var i = 0; i < 4; i++) Assign(skills.GetArrayElementAtIndex(i), "Skill" + (i + 1));
        serialized.ApplyModifiedPropertiesWithoutUndo();
        actor.SetActive(true);
        Assert.IsTrue(source.didAwake, "输入组件尚未执行 Awake。");
        InputSystem.Update();
    }

    /// <summary>恢复输入设置并释放设备、引用和测试对象。</summary>
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.DestroyImmediate(actor);
        foreach (var reference in references) Object.DestroyImmediate(reference);
        references.Clear();
        Object.DestroyImmediate(asset);
        InputSystem.RemoveDevice(keyboard);
        InputSystem.RemoveDevice(mouse);
        InputSystem.settings.updateMode = previousUpdateMode;
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
        InputSystem.settings.backgroundBehavior = previousBackground;
        yield return new ExitPlayMode();
    }

    /// <summary>W、Shift、空格能被正确采样，跳跃只响应按下帧。</summary>
    [Test]
    public void SamplesMovementHoldAndJumpEdge()
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift, Key.Space));
        InputSystem.Update();
        Assert.IsTrue(keyboard.wKey.isPressed, "测试设备的 W 输入未被处理。");
        var frame = source.Sample(10, 90f);
        Assert.AreEqual(Vector2.up, frame.Move);
        Assert.IsTrue(frame.IsRun);
        Assert.IsTrue(frame.JumpPressed);
        Assert.AreEqual(10, frame.InputSeq);
        Assert.AreEqual(90f, frame.RotationY);
        InputSystem.Update();
        Assert.IsFalse(source.Sample(11, 90f).JumpPressed);
        Assert.IsTrue(source.Sample(11, 90f).IsRun);
    }

    /// <summary>1234 分别对应四个技能，只响应按下帧。</summary>
    [TestCase(Key.Digit1, 1)]
    [TestCase(Key.Digit2, 2)]
    [TestCase(Key.Digit3, 3)]
    [TestCase(Key.Digit4, 4)]
    public void SamplesFourSkillBindings(Key key, int index)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        InputSystem.Update();
        Assert.AreEqual(index, source.Sample(1, 0f).SkillIndex);
        InputSystem.Update();
        Assert.AreEqual(0, source.Sample(2, 0f).SkillIndex);
    }

    /// <summary>左键只在按下帧产生攻击请求。</summary>
    [Test]
    public void AttackIsPressEdge()
    {
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
        InputSystem.Update();
        Assert.IsTrue(source.Sample(1, 0f).AttackPressed);
        InputSystem.Update();
        Assert.IsFalse(source.Sample(2, 0f).AttackPressed);
    }

    /// <summary>组件禁用后输出空输入，但不关闭共享资产上的 Action。</summary>
    [Test]
    public void DisableDoesNotDisableSharedAction()
    {
        var shared = asset.FindAction("Player/Move", true);
        shared.Enable();
        source.enabled = false;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
        InputSystem.Update();
        Assert.IsTrue(shared.enabled);
        Assert.AreEqual(Vector2.zero, source.Sample(1, 0f).Move);
        source.enabled = true;
        InputSystem.Update();
        Assert.AreEqual(Vector2.up, source.Sample(2, 0f).Move);
    }

    /// <summary>将配置资产中的 Action 绑定到组件序列化字段。</summary>
    private void Assign(SerializedProperty property, string name)
    {
        var reference = InputActionReference.Create(asset.FindAction("Player/" + name, true));
        references.Add(reference);
        property.objectReferenceValue = reference;
    }
}
