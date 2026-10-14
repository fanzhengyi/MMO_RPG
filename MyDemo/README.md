# 客户端与服务端代码说明

当前实现一个公共世界、每个账号一个角色，支持移动、奔跑、跳跃、远端角色显示和断线保存。

## 客户端职责

- `PlayerInputSource`：读取 Input Actions，返回一帧输入，不处理移动或网络。
- `PlayerFsmCore` 和 `States`：保留 Idle、Move、Run、Jump、Fall 五个独立状态。
- `PlayerController`：执行 CharacterController 位移、转向和重力，更新 Animator。
- `PlayerMovementSync`：进入世界后发送移动快照，收到服务端回正时重新定位。
- `PlayerStateMapping`：网络状态与本地 FSM 的转换，放在网络集成层，不给 Player.Runtime 添加网络依赖。
- `RemotePlayerManager` / `RemotePlayerView`：管理其他玩家的模型、最新快照和位置平滑。
- `NetworkManager`：连接、心跳和断线事件；登录、角色加载、UI 分别由对应 Controller 和 Manager 处理。

人物根节点挂 `CharacterController`、`PlayerInputSource`、`PlayerController`、`PlayerMovementSync`。
Animator 放在独立模型子物体，输入和角色控制脚本放在根节点；远端玩家只克隆外观。
Animator 继续使用 `State(int)`、`Speed(float)`：Idle=0、Move=1、Jump=2、Run=6、Fall=7。
Speed 是实际水平速度，角色和远端外观都关闭 Root Motion。

### Unity 挂载位置

以下配置已通过 Unity MCP 检查，已有的正确组件不重复添加。

| 位置 | 组件与引用 |
| --- | --- |
| `Assets/Main/Init.unity` 的 `YooAssestIniit` | `MyYooAsset`，初始化资源后加载 Login |
| Init 的 `Canvas/LoadUi` | `LoadUi`，引用 LoadBar、txtContent 和启动入口；关闭时解除事件订阅 |
| Login 的 `GlobalManager` | `GlobalManager`，初始化 Fantasy 后创建登录 UI |
| Scene Night 的 `PlayerRoot` | CharacterController、PlayerInputSource、PlayerController、PlayerMovementSync |
| PlayerRoot 的 PlayerInputSource | 引用 Player/Move、Player/Run、Player/Jump |
| PlayerRoot 的 PlayerController | 引用本物体的输入、模型子物体的 Animator、实际 Main Camera 的 Transform |
| Scene Night 的 `Main Camera` | Camera、AudioListener、CinemachineBrain |
| Scene Night 的 `CinemachineCamera` | OrbitalFollow、RotationComposer、Deoccluder、ThirdPersonCameraInput；跟踪 Tracking Target，输入引用 Camera/Look 和 Camera/Zoom |
| UI 预制体根节点 | LoginPanel、ChooseRolePanel、TipPanel 分别挂对应面板脚本，控件引用已配置 |

从 `Assets/Main/Init.unity` 启动完整流程，直接运行 Login 会缺少 YooAsset 初始化。
`MainRole.prefab`、`NPC.prefab` 是外观资源，不挂本地输入和移动同步。
RemotePlayerManager、RemotePlayerView 由进入世界流程动态创建；普通 C# 管理器、FSM 状态和 Fantasy Handler 不挂场景物体。

### 未引用代码处理

检查范围为自有客户端和服务端代码，并核对场景、预制体、Timeline、动画事件及框架入口。
NetworkProtocolHelper 已删除 14 个未使用的重载和服务端发送入口，仅保留客户端调用的六个方法。
ResManager 删除未使用的回调式 LoadAsync，资源异步加载统一使用 await 版本；TipPanel 删除未使用的背景图片字段。
删除客户端 JwtHelper，鉴权响应直接返回 GateAddress，客户端不再依赖 Token 的内部结构。
删除内网 RoleData 和重复映射，Game 与 Gate 直接复用 PlayerInfo；创建角色不再返回未被使用的完整资料。
原 CreatRoleController 改为 RoleController，保留脚本 GUID；角色的停用、暂停和传送共用速度清理逻辑。
登录不再重复清理角色数据，连接结束后的清理统一由 GateDisconnected 的订阅者执行。
NLog 按当前 ILog 的真实签名保留框架入口，删除未使用的 Fatal 重载和旧版本 Initialize 重载。
创建面板将账号含义的 userName 字段改为 nicknameInput，并同步修改预制体引用。

- 龙特效的 PlayGroundSmoke、PlayIceFire、StopIceFire、HideAll 由 Timeline 动画事件调用，保留并标注用途。
- ClearStatics 通过 RuntimeInitializeOnLoadMethod 执行；Unity 回调和 Fantasy Handler/System 同样属于框架入口。
- UIManager.GetPanel/Close、ResManager 的卸载接口保留，供后续背包、商店、邮件等界面使用。
- FSM 的 Enter/Exit 保留，作为后续攻击和技能状态的生命周期。
- `Test'.cs` 仍挂在 BeginScen 的测试按钮上，不是闲置脚本，演示场景也保留。

### 协议导出

`Assets/MMO_Hot/Proto/NetworkProtocolHelper.cs` 现由项目维护，每个方法注明用途。
使用 `Tools/ProtocolExportTool/Run.bat` 或 `Run.sh` 导出；它们会保留这份精简 helper，正常更新其他协议文件。
导出后统一清除生成代码的行尾空白，避免每次导出产生无意义的格式差异。
Windows 也可直接执行 `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/ProtocolExportTool/Export.ps1`。
不要直接运行导出 DLL，否则工具仍会覆盖 helper；新增客户端请求时，再添加实际需要的发送方法。
Create 创建的请求交给 Fantasy 发送后自动回收，不要跨 await 再用 using 释放请求；返回的响应由调用者 using 释放。
OpCode.Cache 中废弃协议的编号继续保留，防止未来导出时被新协议复用，不属于可删除的业务代码。

### 协议约定

协议源在 `Tools/NetworkProtocol/Outer/OuterMessage.proto` 和 `Inner/InnerMessage.proto`，生成的协议文件不要手改。

- 所有 RPC 使用 `ResultCode Result` 表示业务结果，只有 Success 是成功；默认 Unknown 不会误判为成功。
- Fantasy 自带的 ErrorCode 表示框架错误，非零时先按失败处理，不与业务结果混用。
- Authentication 返回 Token 和 GateAddress；Gate 从验签后的 Token 提取账号，并校验签名中的 GateSceneId。
- EnterGame 成功必须有 Info；RoleNotFound 表示需要创建角色，不再用 HaveRole 充当成功码。
- CreateRole 只返回 Success、RoleAlreadyExists 或失败原因，初始资料统一通过 EnterGame 获取。
- PlayerInfo 只给本人；PlayerPresence 给其他玩家，两者都不含登录账号。
- MovementSnapshot 是共用的世界位置、朝向、移动状态和实际水平速度数据，不包含连接身份。
- 网络 PlayerMotionState 使用 Idle、Walk、Jump、Run、Fall；本地 Animator 的 Run=6、Fall=7 不变。

| 移动消息 | 职责 | 序号 |
| --- | --- | --- |
| C2G_PlayerMove | 本地采样上报 | Sequence：当前 Gate 连接的上行序号 |
| G2C_PlayerMove | 其他玩家移动 | Version：Game 生成的移动版本 |
| G2C_PlayerPositionCorrection | 回正本人 | AckSequence：本次被拒绝的上行序号 |
| G2C_PlayerEntered | 远端初始资料 | Player.Version：与远端移动版本一致 |

内网消息额外携带可信账号或目标 GateSessionRuntimeId，不重复定义另一套移动与角色数据。
移动和回正不走 RPC，协议不依赖 TCP 或 KCP 的具体实现。

进入流程：鉴权登录 -> Gate 登录 -> EnterGame -> 加载场景 -> 放置角色 -> 开启移动同步。
CreateRole 只创建数据，成功后仍然调用 EnterGame。
连接失效时清空角色数据、远端模型，关闭创建界面并显示登录界面。

## 服务端职责

- Authentication：账号查库、密码校验和 Token 签发。
- Gate：验证 Token 和当前 Session，给请求附加可信账号身份，转发消息。
- Game：加载角色、维护在线角色、验证移动、广播快照和保存数据。
- `PlayerMessageMapper`：实体与协议数据的转换，Gate 不访问 Game 的数据库实体。
- `OnlinePlayerSystem`：统一处理角色加载、上线通知和定时保存。
- `PlayerMovementRules`：只校验和更新内存，不访问网络或数据库。

创建、进入、离开和保存共用账号对应的角色锁，防止并发加载两个实例。
移动消息只更新内存；每五秒保存脏角色，正常断线时保存最终位置。
保存失败会保留离线角色，下一轮重试；保存成功后释放离线角色。
新建角色不使用对象池，避免复用旧角色的持久字段；Session 释放时清理身份和路由。

## 同步规则

客户端默认每 0.1 秒发送有变化的快照，状态变化立即发送；Gate 每条连接最多转发 50 次/秒。
上行 Sequence 由 NetworkManager 管理，用来丢弃重复或旧包；切场景、重新入场或停用组件不会重置。
下行 Version 由 Game 生成，序号比较支持 uint 回绕；换连接重置上行记录，并增加待机状态的下行版本。
场景加载期间只缓存每个远端角色的最新快照，避免持有会被回收的协议对象。

服务端检查有限数值、状态、速度和可累计的位移预算。
水平上限 9 米/秒，垂直上限 45 米/秒，报告速度上限 8 米/秒；修改客户端速度时同步调整规则常量。
站立时间最多累计一秒的移动预算；高频消息不会反复获得额外宽限。
拒绝位移后，把确认位置发给本人回正；收到旧包则直接丢弃。
本人回正只走 G2C_PlayerPositionCorrection，不再根据 RoleId 猜测一条消息的含义。

当前由客户端计算位移，服务端不计算场景碰撞；广播范围是整个 Game 场景。
面向大量玩家时需要加入 AOI 范围订阅和世界分区；涉及战斗判定时再补服务端权威模拟。

## 配置与账号兼容

启服配置只维护 `Server/Entity/Fantasy.config`，Main 从这里复制输出，Hotfix 不再重复生成配置类型。
当前外网使用 TCP；切换 KCP 时同时修改客户端 `NetworkManager.Protocol` 和 Authentication、Gate 的 `networkProtocol`。
跨机器连接时同步修改客户端鉴权地址以及配置中的机器 `outerIP`。

密码存储改为 PBKDF2-SHA256，采用随机盐；旧明文密码在正确登录时自动升级。
Token 绑定账号，五分钟有效；Gate 验证签名、账号及有效期。升级后重启服务端并重新登录。
密码计算在后台执行，多个鉴权场景共用最多四个计算任务的并发上限。

本机开发自动在启动程序的 `JwtKeys` 目录生成 RSA 文件，多个进程共用同一输出目录的密钥。
分开部署时配置环境变量 `FANTASY_JWT_KEY_DIRECTORY`：Authentication 读取 `private.key`，Gate 读取对应的 `public.key`。
文件格式分别是 `ExportRSAPrivateKey` / `ExportRSAPublicKey` 导出的 DER 字节；私钥只分发给鉴权服务。
配置部署目录后不会自动生成密钥；`JwtKeys` 已加入 Git 忽略规则。

### 本次升级

本次修改了协议字段、状态编码和 Token 内容，不兼容旧客户端或旧服务端。
两端必须一起更新并重新编译，重启所有服务端进程；客户端重新打包热更新代码与资源后再登录。
账号和角色的数据库字段未变，不需要迁移已有账号和位置数据。旧 Token 作废，重新登录即可。
正式部署还需要加密传输、请求总量限制和完整反作弊；当前位移预算不能代替服务端碰撞与战斗判定。

## 验证

```powershell
dotnet build Server/Server.sln --no-restore
dotnet build Client/MMO_RPG/Assembly-CSharp.csproj --no-restore
dotnet run --project Server/Tests/Regression/Regression.csproj --no-restore
```

回归检查覆盖角色 DTO、持久化字段、正常移动、位移预算、非法快照、序号回绕、重连、密码、Token 和客户端回正。
额外检查协议职责、真实 ProtoBuf 嵌套序列化，以及 RPC 等待期间请求被对象池复用的生命周期。
检查不启动服务端、不连接数据库，Token 检查使用独立临时密钥。
客户端 helper 检查固定当前六个发送入口；新增业务发送方法时同步更新该检查。
首次运行测试项目时先执行 `dotnet restore Server/Tests/Regression/Regression.csproj`。
Unity 的生成项目文件会在导入时刷新，新增脚本无需手工维护 Assembly-CSharp.csproj。

场景联调需要在 Unity 用两个不同账号验证：互相看见移动/奔跑/跳跃、重复登录关闭旧连接、断线后重登恢复最终位置。
资源失败后的重新打开、相机跟随和具体模型动画也需要在编辑器中验证。

本次 Unity MCP 检查确认角色、相机、启动与登录场景、UI 预制体挂载完整，没有缺失脚本。
运行检测覆盖 Init -> Login、五状态切换、实际奔跑速度、本人回正、远端动画映射和旧版本丢弃。
远端 Animator 的参数在外观激活后写入，避免初始化时被重置。
Scene Night 的旧 LightingData 有版本不兼容和源文件缺失警告，需要在 Unity 重新烘焙光照；本次没有重烘焙资源。
