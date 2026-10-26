# 主角动作、特效与联机同步

## 当前操作

| 输入 | 动作 | 完整动作时长 | 冷却（从起手计算） |
| --- | --- | --- | --- |
| 鼠标左键 | Anim_JX_Atk01 → Atk02 → Atk03 → Atk01 | 1.275 / 1.157 / 1.389 秒 | 无独立冷却，受接段窗口约束 |
| 数字行 1 | Anim_JX_Skill01 | 2 秒 | 4 秒 |
| 数字行 2 | Anim_JX_Skill02 | 2.1 秒 | 6 秒 |
| 数字行 3 | Anim_JX_Skill03 | 2.2 秒 | 8 秒 |
| 数字行 4 | Anim_JX_Skill04 | 1.2 秒 | 10 秒 |
| F | Anim_JX_Slider_F | 0.65 秒 | 1.2 秒 |

数字键使用 QWER 上方的 1～4。F 一次按下只执行一次，持续按住不会重复；再次滑行需松开、等待冷却后重新按下。滑行沿起手朝向，以 7.5 米/秒移动，仍经过 CharacterController 碰撞和重力，撞墙会停住。

普攻每次点击播放一段，最多缓存一次提前输入，缓存有效期 0.25 秒；恢复控制后 0.35 秒内点击可以续段，超过则从第一段重来。按住鼠标不会自动连击。普攻起手前 15% 可随移动输入调整朝向。技能和滑行不打断尚未恢复的动作，也不缓存忙碌期间的技能/F 输入；同帧空闲输入优先级为跳跃、F、较小编号的技能、普攻。地面动作不接受空中起手。

完整动画时长与恢复控制时间不同：普攻恢复点为 85% / 85% / 82%，技能为 90% / 90% / 90% / 95%，滑行为 100%。表现层在恢复后淡出，特效按自己的尾迹生命周期结束。

## 特效配置

统一配置入口：`Client/MMO_RPG/Assets/MMO_Hot/Config/PlayerCombat.asset`。每个动作配置动画及特效列表，每条特效指定起始进度、挂点、偏移、旋转、缩放与生命周期。

| 动作 | 特效与位置 |
| --- | --- |
| 三段普攻 | Effect_Attack01，绑定实际右手刀刃，进度 0.12 触发 |
| 技能 1 | Effect_Skill01_1，角色挂点，进度 0.18 |
| 技能 2 | Effect_Skill02_Juqi，角色挂点，起手播放 |
| 技能 3 | Effect_Skill3，起手朝向前方 2 米，进度 0.35 |
| 技能 4 | Effect_Skill04_Juqi 起手蓄气；进度 0.35 在起手朝向前方 3 米生成 Effect_Skill04 |

前方特效以起手位置与朝向定位，后续转向或移动不会拖动已经落地的世界特效。附着特效跟随刀刃或角色，结束后停止发射并保留必要尾迹。每个角色独立管理自己的特效，远端角色复制外观时不会复制正在运行的特效。

修复了这六个特效所引用的 65 个失效材质，使用 `MMO/Effects/Combat Particle` 兼容现有贴图、颜色、混合、遮罩、溶解等参数。原始扭曲着色器缺失且仅有法线图的粒子没有足够信息还原，当前透明跳过这一部分；不代表完整复原原作者的扭曲效果。原材质备份和修复报告位于本机 `Client/MMO_RPG/Library/Verification/Skills/`，Library 不属于发布资源。

## 职责与同步

| 模块 | 职责 |
| --- | --- |
| PlayerInputSource | 采集按键，持有独立 Input Action 副本 |
| PlayerCombatCore / AttackComboCore | 本地动作、连击、冷却和短输入缓存，不依赖 Unity 或网络 |
| PlayerActionRules | 客户端与服务端共用的动作编号、时序与命令规则；服务端项目直接引用同一文件 |
| PlayerController | 协调输入、移动和动作，角色碰撞与重力 |
| PlayerActionPresentation / PlayerActionEffects | Animator 和特效表现，不读取按键、不发网络消息 |
| PlayerActionSync | 上报本地动作，匹配预测代次，拒绝时只撤销对应动作 |
| RemotePlayerManager / RemotePlayerView | 独立接收远端动作，在移动使用的同一延迟服务端时间轴播放 |
| PlayerRouteType / Game 路由处理器 | Gate 在进入世界后把 `PlayerRouteType` 绑定到玩家 `OnlineInfo.Address`；Fantasy 自动把移动和动作请求送到该实体，Game 判断动作顺序、锁定与冷却，然后广播 |

移动快照与战斗命令分离，不把攻击编号塞进移动 FSM。协议版本为 4，移动和动作请求声明为 Fantasy 自定义路由消息，路由类型为 `PlayerRouteType = 1000`。登录仍由 Gate 验证 Token，进入世界仍由 Gate 编排；Game 返回当前玩家的 `OnlineInfo.Address` 后，Gate 只建立一次 `PlayerRouteType -> OnlineInfo.Address` 映射。之后客户端不携带实体地址，Fantasy 的消息调度器按 Session 路由自动把请求送到对应的 Game 实体，Gate 不再为每条移动或动作消息写重复转发处理器。

Game 端的 `C2G_PlayerMoveRouteHandler` 和 `C2G_PlayerActionRouteHandler` 直接接收玩家实体，继续复用共享移动、动作规则和限流状态。Game 只通过原有内部消息向 Gate 发送权威广播、位置回正和生命周期通知。断线、离开世界或切换场景时清理旧路由，重新进入世界时绑定新的 `OnlineInfo.Address`。`RoleId` 是持久角色身份，`OnlineInfo.Address` 是当前运行时实体地址，两者不混用。

预留 `ChatRouteType = 1001` 给 Chat 服务。以后 Chat 服务返回 Chat 实体地址后，可以在同一 Gate Session 上增加 Chat 路由；聊天协议声明 `ICustomRouteMessage` 或 `ICustomRouteRequest` 并使用该路由类型，客户端仍只发业务消息，不需要知道 Chat 地址。每个路由类型在重连或换服前先移除旧地址，再绑定新地址。

本地先预测播放以保持响应速度，服务端使用自己的时钟校验，不接受客户端自报起手时间。动作取消只针对对应的已接受序号；迟到拒绝不会取消下一次动作。位置回正、断线、离开世界或角色停用会清理当前动作与特效；取消不会退还已经使用的冷却。

本轮范围是动作、特效、冷却和状态同步，尚未接入目标选择、命中判定、伤害、生命值及消耗结算。

## 编辑器运行与验证

当前 Init 使用 EditorSimulateMode，直接读取项目资源。没有打 AB、没有打客户端包，也没有创建新验证场景。现有 GameScene 已保存技能输入、动作配置和 PlayerActionSync 的引用。开发服务器已按协议 4 更新并启动；以后修改协议或服务端代码仍需重新编译、重启对应服务端进程。

2026-10-22 验证结果：

- 服务端构建 0 警告、0 错误；回归 41/41 通过；隔离数据库和端口的真实 TCP 集成验证 5/5 通过。
- 在现有 Init → Login → GameScene 流程中使用真实 Input System 输入，验证三段连击、超时回第一段、四个技能、冷却限制，以及 F 长按两秒只启动一次滑行。
- 临时运行时墙体验证滑行受碰撞阻挡；动作结束后移动和跳跃恢复。采样的脚底与地面最小间距约 0.05 米，未检测到该次采样中的穿地。
- 第二个真实网络客户端收到本地主角全部八类动作；Unity 同样收到并播放远端八类动作及特效，测试中没有触发移动回正。
- Animator 状态机保存、重导入后引用有效；特效引用材质均支持当前渲染器。结束后本地特效数量归零，Unity 控制台 0 错误。
- 临时验证脚本、输入设备、墙体已清理；隔离测试进程已停止，隔离数据库已删除。保留正常开发服务器供继续使用。

本机证据位于 `Client/MMO_RPG/Library/Verification/Skills/`：`runtime.json`、`animation-setup.json`、`material-setup.json`、`asset-setup.json` 和各动作截图。验证脚本源码只存于该目录的 `AuthoringSources/`，没有留在 Assets 中自动运行。

回归入口：`dotnet run --project Server/Tests/Regression/Regression.csproj --no-restore`。
