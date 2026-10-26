# 主角移动与战斗

`PlayerInputSource` 采集移动、奔跑、跳跃、鼠标左键普攻、数字行 1～4 技能及 F 滑行。每名本地角色持有自己的九个 Input Action 副本，启用时创建，停用时释放。技能键使用 QWER 上方的数字行；F 只读取按下边沿，持续按住不会重复滑行。

`PlayerFsmCore` 管理 Idle、Move、Run、Jump、Fall；`PlayerCombatCore` 管理三段连击、技能、滑行、输入缓存和冷却。它们都是普通 C# 对象，由 `PlayerController` 协调。`PlayerActionPresentation` 负责独立 Combat 动画层，`PlayerActionEffects` 按配置时间点播放特效。

人物根节点挂 `CharacterController`、`PlayerInputSource`、`PlayerController`。模型子物体保留 `Animator`。Controller 的 `cameraRoot` 必须引用实际输出相机，`combatDefinition` 引用 `Assets/MMO_Hot/Config/PlayerCombat.asset`；InputSource 必须配置全部九个动作引用。

联机场景另挂 `PlayerMovementSync` 和 `PlayerActionSync`：前者发送移动快照并处理位置回正，后者发送独立动作命令并处理服务端拒绝。本地控制器及两个核心不依赖网络；远端由 `RemotePlayerManager` / `RemotePlayerView` 使用同一套动作表现配置。

动画 Base Layer 使用 `State(int)` 和 `Speed(float)`。移动状态编号为 Idle=0、Move=1、Jump=2、Run=6、Fall=7；Speed 为碰撞处理后的实际水平速度。Combat 层使用 Attack1～3、Skill1～4、Slide 和 `ActionSpeed(float)`，速度由素材长度与共享动作时长计算。角色位置由 CharacterController 驱动，关闭 Animator 根位移。

技能冷却和动作时序统一放在 `Combat/PlayerActionRules.cs`，服务端直接引用同一份文件。动画、特效、挂点、触发进度与缩放配置放在 `PlayerCombat.asset`。修改协议后需要重新生成并重启服务端；仅调整特效表现无需修改协议。

完整按键、规则、同步方式和验证记录见仓库根目录的 `Docs/CombatSystem.md`。
