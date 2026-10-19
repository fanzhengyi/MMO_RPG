# 主角三段普攻

鼠标左键开始普通攻击，按 **Atk01 → Atk02 → Atk03 → Atk01** 循环。一次点击只播放一段；提前点击最多缓存一次，缓存只保留 **0.25 秒**，到接段点仍有效才播放下一段。已进入接段窗口时，点击立即续段；连续点击不会积累一整串待播放动作，按住鼠标不会自动连击。

没有续段时，在恢复点解除攻击对移动和跳跃的限制，并淡出攻击层，不等待动画尾部完整播完。恢复后的 **0.35 秒**内再次点击仍续接下一段，超过后重新从第一段开始。每段起手的前 **15%** 允许按移动输入调整朝向，之后固定出刀方向；从跑动进入攻击时按原有减速度自然停下。

## 配置

`Client/MMO_RPG/Assets/MMO_Hot/Config/PlayerAttackCombo.asset`：

| 分段参数 | Atk01 | Atk02 | Atk03 | 用途 |
| --- | --- | --- | --- | --- |
| PlaybackSpeed | 1.7 | 1.7 | 1.8 | 动画播放倍率 |
| ChainAt | 0.65 | 0.65 | 0.62 | 可接下一段的归一化进度 |
| RecoverAt | 0.85 | 0.85 | 0.82 | 无续段时恢复控制的归一化进度 |
| TurnUntil | 0.15 | 0.15 | 0.15 | 允许调整攻击朝向的起手进度 |

| 共用参数 | 默认值 | 用途 |
| --- | --- | --- |
| inputBuffer | 0.25 秒 | 提前点击等待接段点的最长时间，最多缓存一次 |
| resetDelay | 0.35 秒 | 恢复控制后允许续接的时间 |
| blendIn / blendOut | 0.08 / 0.08 秒 | 攻击层进入、退出时的混合时间 |

三段完整动画时长约为 1.275、1.157、1.389 秒，按 Clip.length / PlaybackSpeed 计算；实际接段或解锁发生在各自的 ChainAt、RecoverAt。修改倍率和窗口即可调整节奏。替换 Clip 时，应同时更新 Player Animator 的 Combat 层对应 Motion，使动画资源与计时资源保持一致。

## 职责与中断

- `PlayerInputSource` 只采集输入，并持有自己的 Input Action 副本。
- `AttackComboCore` 是纯 C# 时序规则，处理顺序、单格短缓存、接段点、恢复点、超时和取消；不依赖 Unity、Animator 或网络。
- `AttackComboDefinition` 保存三段动画及节奏参数，不保存玩家的运行状态。
- `PlayerAttackController` 将开始段号映射到独立 Combat 动画层，处理速度和层权重；移动 FSM 继续驱动 Base Layer。
- `PlayerController` 负责协调：攻击中将水平目标速度设为零并自然减速，只在起手窗口允许转向，恢复点前忽略跳跃；保留 CharacterController 的重力和碰撞。未出招时同时按跳跃和攻击，跳跃优先；离地不接受地面普攻。

失去控制、停用角色、出生定位及服务端回正都经过 ResetMovement，立即清除动作、续段历史、输入缓存及 Combat 层权重。恢复后从第一段重新开始。

三段 FBX 为非循环动画，统一复用修复后的 `Modle_Avatar01.FBX` 源 Avatar。角色模型继续使用自身 Avatar；动画根位移不直接驱动游戏角色位置。

三段攻击的 Root Transform Position (Y) 已启用 Bake Into Pose，Based Upon 保留 Original，让下蹲与重心起伏保留在姿态中。Rotation 和 Position (XZ) 继续提取，配合 `applyRootMotion=false` 丢弃根旋转与水平旅行，角色的位置和朝向仍由控制器管理。Foot IK 的开关对本次下沉问题没有实测改善，保持原配置。

本轮实现范围为本地主角的普攻输入、连击和动画表现。移动协议仍只包含移动状态；未增加攻击广播、服务端战斗裁决、命中、伤害或冷却协议。后续联机战斗应以独立战斗命令和服务端确认驱动表现，不能将 Animator 的 Attack 数字塞入移动状态字段。

## 验证

- 当前规则回归 **34/34**。连击检查覆盖单击恢复、短缓存及过期、三段循环、续段超时、低帧率边界、取消与暂停；新增边界检查确认按实际接段时刻判断缓存有效期、迟到输入不回溯、恢复点早于动画尾部。
- 导入配置 A/B 采样中，旧 Atk02、Atk03 的脚底最低点相对角色 root 约为 -0.312、-0.351 米；仅启用 Root Y Bake 后约为 -0.053、-0.048 米。这是相对 root 的姿态测量，不等同于脚底与实际地面的距离。
- Unity MCP 在 GameScene 的独立角色副本上，用真实 Input System 临时设备完成 `1 → 2 → 3 → 1`；实际接段间隔约为 0.839、0.758、0.864 秒，三个动画都实际播放，结束后回到待机且 Combat 权重为 0。该轮 214 个满权重、接地姿态样本均未穿地；完整地面输入测试另采集 448 个样本，脚底距所在位置碰撞面最小约 0.027 米。采样强制 Animator 持续更新，避免离屏剔除造成静止姿态假结果。
- 真实地图输入测试同时覆盖单击恢复、恢复后移动和跳跃、起手转向、跑动出招自然减速、宽限期续第二段和超时重回第一段。控制台无错误。循环、动画资源、接地、回到待机及静止出招五项自动断言全部通过。记录位于 `Library/Verification/AttackFeel/`；临时角色、地板、输入设备和导入对比资源已删除，原输入绑定与设置已恢复。

此前版本已用 Unity MCP 的临时 Input System 鼠标、键盘验证三段循环、续段超时、回正与失控清空、空中忽略攻击，以及动作结束后恢复移动和跳跃。旧版“攻击期间无位移”检查对应当时立即清零速度的实现；当前改为自然减速，不能直接套用该结论。此前截图与记录位于 `Client/MMO_RPG/Library/Verification/Combo/`，输入验证结束后恢复原绑定和设置。

### Combat 层资源重载修复

后续编辑器重新读取资源时出现 `Statemachine for layer 'Combat' is missing`。当时磁盘虽有状态机数据，导入后的层引用却为空；创建完成时的内存测试未覆盖这个问题。

已在保持 Player.controller GUID 的前提下重建 Combat 状态机及四个状态，将每个对象显式保存到控制器资源，重新序列化并强制导入。额外通过新 GUID 的独立副本导入、脚本域重载后再次读取验证状态机及三段动画引用，避免只验证仍在内存中的对象。运行代码没有加入隐藏错误的判空回退。

重载后在真实 GameScene 再次使用临时鼠标输入播放 `1 → 2 → 3 → 1`，三个运行时状态均可解析，三个动画均实际播放；结束后返回待机且 Combat 权重为 0，测试期间错误列表为空。结果保留在 `Library/Verification/Combo/reload-fix.json`。验证用控制器副本与临时输入已删除。

播放中脚本热重载还会丢弃未序列化的纯 C# 运行对象，而不会重新调用 Awake。PlayerController 现在在 OnEnable 重建 FSM 和连击控制器，PlayerInputSource 在 OnEnable 创建并启用输入副本、OnDisable 关闭并释放；PlayerMovementSync 也在角色初始化完成后的 OnEnable 绑定控制权和回正事件。台阶高度仍只在 Awake 缓存，避免空中热重载把临时的零高度记为配置。

生命周期验证使用独立角色副本：禁用后清除 FSM、连击和输入运行字段，模拟热重载，再重新启用。核心对象、四个启用中的输入副本及字段/数组对象身份均恢复，Place 和销毁也无异常；结果保留在 `Library/Verification/Combo/lifecycle-fix.json`。后续节奏调整的当前规则回归为 34/34。

运行规则回归：`dotnet run --project Server/Tests/Regression/Regression.csproj --no-restore`。
