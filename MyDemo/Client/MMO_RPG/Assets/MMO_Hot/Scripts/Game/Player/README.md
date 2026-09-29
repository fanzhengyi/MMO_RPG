# 本地角色接入

## 职责

| 文件 | 职责 |
| --- | --- |
| PlayerInputSource | 把 Input Actions 转成输入帧，只在本地玩家上启用 |
| PlayerController | 组织一帧流程，向外提供状态和事件 |
| PlayerFsmCore | 缓存状态对象，调用 Enter / Tick / Exit，维护技能冷却 |
| States/ | 每种状态自己的进入、更新、退出逻辑 |
| PlayerMotor | CharacterController 位移、转向、重力、起跳、撞顶处理 |
| PlayerAnimator | 将角色状态和实际速度写入 Animator 参数 |
| PlayerSettings | 移动、四段普攻和四个技能的可调参数 |

只有 PlayerInputSource、PlayerController、PlayerAnimator 是要挂载的业务组件。
Motor 和所有 State 都是普通 C# 对象，由角色入口创建；每次切换复用实例。
地面状态复用 PlayerGroundedState，普攻和技能复用 PlayerActionState，避免重复计时与判断。

## 人物层级

```text
PlayerRoot                         scale = (1, 1, 1)
  CharacterController
  PlayerInputSource
  PlayerController
  PlayerAnimator
  Model
    Animator
```

1. 在角色根节点添加上述组件。不要再用 Rigidbody 或其他脚本同时驱动位置。
2. 调整 CharacterController 的 Height / Radius / Center，使胶囊包住模型、底部对齐脚底。Min Move Distance 设为 0；地面要有 Collider。
3. 在 PlayerController 的 Camera Root 指定实际渲染的主相机 Transform。Cinemachine 改变主相机朝向后，移动自动按该水平朝向换算。不要将相机旋转代码写入人物状态。
4. PlayerInputSource 按名字拖入 PlayerInput.inputactions 的 Move、Run、Jump、Attack；Skill Actions 下标 0-3 分别放 Skill1-4。
5. Input System 的 Update Mode 使用 Process Events In Dynamic Update。玩家输入组件无需再额外挂 Unity 的 PlayerInput。
6. 在 PlayerAnimator 指定 PlayerController 和模型上的 Animator。关闭 Animator 的 Apply Root Motion，Update Mode 使用 Normal，Speed 保持 1。
7. 其他玩家由远端状态驱动，不能启用这套本地输入控制器。聊天框、菜单需要屏蔽人物操作时，可禁用 PlayerInputSource；此时输出空输入。默认点击 UI 不触发普攻。

现有动作资产已补齐 Skill3 / Skill4，绑定键盘 3 / 4。所有按钮按本次配置不勾 Initial State Check；Move 保持 Value / Vector2。
输入组件在 Awake 复制动作并自行管理启停，不会误关相机或其他对象共享的 Action。运行中修改原资产的重绑定不会自动更新这些副本。

## Animator 参数与连线

已生成 `Assets/MMO_Hot/ArtRes/Animas/AnimatorController/Player.controller`，包含全部状态和连线，可直接使用。
执行 Unity 菜单 **Tools > Player > Create Animator Controller** 可以另外生成一份。
工具会复用 JX 目录中已存在的动画，同名 Controller 已存在时生成新文件，不覆盖你的修改。
将生成的 Controller 拖到模型 Animator 的 Controller 槽。

| 参数 | 类型 | 来源 |
| --- | --- | --- |
| State | Int | PlayerStateType，决定当前行为 |
| Speed | Float | 碰撞处理后的实际水平速度，单位米/秒 |
| ComboStep | Int | 当前普攻 1-4；非普攻为 0 |
| SkillIndex | Int | 当前技能 1-4；非技能为 0 |

Base Layer 默认状态设为 Locomotion，使用 **1D Blend Tree**，Blend Parameter 为 Speed。
关闭自动阈值：Idle = 0，Walk = 4，Run = 6.5。修改角色 WalkSpeed / RunSpeed 时一并调整阈值。
这是转向行进方向的移动方案，S 会使人物转身向相机后方走，并非保持朝向后退。

| 连线 | 条件（同一行的条件同时成立） |
| --- | --- |
| Entry → Locomotion | 默认状态 |
| Any State → Locomotion | State Equals 0 |
| Any State → Locomotion | State Equals 1，单独一条连线 |
| Any State → Locomotion | State Equals 6，单独一条连线 |
| Any State → Jump | State Equals 2 |
| Any State → Fall | State Equals 7 |
| Any State → Attack1 / 2 / 3 / 4 | State Equals 3，ComboStep Equals 对应的 1 / 2 / 3 / 4 |
| Any State → Skill1 / 2 / 3 / 4 | State Equals 4，SkillIndex Equals 对应的 1 / 2 / 3 / 4 |
| Any State → Dead | State Equals 5 |

所有 Any State 连线：**Has Exit Time 关闭，Can Transition To Self 关闭，Fixed Duration 开启，Duration = 0.06 秒**。
Interruption Source = Source Then Destination，Ordered Interruption 开启，避免同一条 Any State 连线反复打断自身过渡。将 Dead 的连线放在 Any State 列表首位，使死亡拥有最高中断优先级。
所有状态 Write Defaults 关闭。Idle / Walk / Run / Fall 的动画开启 Loop Time；Jump 起跳、Attack、Skill、Dead 关闭 Loop Time。
关闭动画 Root Motion 并使用原地动作；检查导入动画与模型的 Rig / Avatar 兼容。

不用额外连接 Attack1 → Attack2 或 Attack → Locomotion，也不用添加攻击 Trigger。代码只在有效点击窗口预约下一段，到了衔接点才修改 ComboStep；动作结束后 State 恢复地面状态，Animator 按条件返回。
状态图不会反过来决定移动、技能冷却或连招窗口，也不需要依赖 Animation Event 才能解锁角色。

## 四段普攻

在 PlayerController > Settings > Attacks 中配置四项，数组下标 0 对应第一段。

| 字段 | 含义 | 默认值 |
| --- | --- | --- |
| Duration | 本段完整时长，秒 | 0.8 |
| Impact Time | 本段触发表现事件的进度 | 0.4 |
| Combo Open | 开始接受下一次点击的进度 | 0.25 |
| Combo Close | 停止接受下一次点击的进度 | 0.7 |
| Combo Transition | 已预约下一段时的衔接进度 | 0.8 |

例如 Duration = 0.8 秒：本段开始后的 0.20-0.56 秒内再次点击会预约下一段，0.64 秒时衔接；没有预约则在 0.80 秒收招。
每段最多预约下一段一次。窗口外的点击忽略，按住左键不会自动连击；第四段不再接受连段预约，完整收招后下一次点击从第一段开始。
默认攻击与施法时立即停止水平移动并锁定朝向；重力仍生效。当前动作不被移动、跳跃或其他技能取消；死亡和失去地面会中断。
地面同帧输入优先级：跳跃 > 可用技能 > 普攻 > 移动。空中允许方向调整，但不允许二段跳、空中普攻或空中技能。

**将每段 Duration 设置为动画片段长度 / Animator State 的 Speed。** 此处是角色动作的逻辑时间，不能在更换动画后仍任意保留默认值。
Impact Time 与连招窗口按实际挥击时机调整。若修改攻速，需要同时修改动作持续时间与动画播放速度。
四个技能分别设置 Duration、Impact Time、Cooldown；冷却从施放开始计时，中断不返还冷却。

## 资源缺口

目前 JX/Atk 中有 Atk01、Atk02、Atk03 和 Skill01-04，没有第四段普攻；Common 中没有名称明确的 Walk 和 Dead 片段。
生成工具保留 Walk、Attack4、Dead 的空 Motion，避免替你选择错误的动作。需要把对应动画拖进去；临时测试可明确选择复用已有动画，但正式资源应补齐。
Jump 使用 Jump_Start，Fall 使用 Jump_Loop；已有 Jump_End 未自动接入。本版落地直接混合回 Locomotion，没有额外的落地硬直状态。
角色胶囊运动、输入和逻辑可独立运行，完整视觉效果需要这些资源及上述参数配置。

## 外围接入

- StateChanged(previous, next)：UI 或表现监听状态变化。
- JumpTriggered()：成功发出本次起跳指令。
- ActionStarted(type, index)：一次普攻段或技能开始；index 为 1-4。
- ActionImpact(type, index)：到达配置的表现点，每段一次，可接挥击音效、特效。
- InputSampled(frame)：输入适配层每帧的原始命令，供同步模块采集；它不是已执行动作或权威状态。
- SetDead / Revive / Teleport：外部系统操作入口。
- GetSkillCooldown(1-4)：读取剩余冷却。

这里实现客户端控制与动作生命周期，不包含 Fantasy 移动协议、预测校正、伤害结算、目标选择、弹道、蓝耗或服务器技能验证。
ActionImpact 不代表目标命中或服务器承认伤害；战斗系统接入时应另行验证。需要批量发送输入时保存跳跃、攻击、技能的边沿事件，不能只取每 50ms 的最后一帧。
移动缓存（如 PlayerSelfModel.ReportMove）和服务器连接由独立适配层处理，角色代码不直接依赖网络单例。

角色逻辑现为 Player.Runtime 程序集，Editor 与测试独立。以后启用 HybridCLR 热更新时，要将 Player.Runtime 纳入项目的热更新程序集与加载流程；本次没有修改 Fantasy / HybridCLR 的全局设置。

## 验证

Unity Test Runner 的 EditMode 中运行 Player.Tests。
已在 Unity 6000.3.8f1 的隔离项目中通过全部 35 项测试，使用本项目的角色脚本、输入资产和本机依赖。
状态与物理测试覆盖移动、奔跑、单次起跳、最高点、落地、撞顶、斜向限速、摇杆幅度、连招窗口、四段收招、冷却及死亡中断。
输入和 Animator 集成用例会进入 Play Mode，检查真实设备事件、组件启停和参数驱动切换。
实际人物的 Avatar、动作观感、台阶、斜坡及 Cinemachine 镜头手感仍需在你的场景中调试。
