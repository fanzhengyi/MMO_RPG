# 客户端与服务端代码说明

当前实现一个公共世界、每个账号一个角色，支持移动、奔跑、跳跃、远端角色显示和断线保存。

## 客户端职责

- `PlayerInputSource`：读取 Input Actions，返回一帧输入，不处理移动或网络。
- `PlayerFsmCore` 和 `States`：保留 Idle、Move、Run、Jump、Fall 五个独立状态。
- `PlayerController`：执行 CharacterController 位移、转向和重力，更新 Animator。
- `PlayerMovementSync`：进入世界后发送移动快照，收到服务端回正时重新定位。
- `RemotePlayerManager` / `RemotePlayerView`：管理其他玩家的模型、最新快照和位置平滑。
- `NetworkManager`：连接、心跳和断线事件；登录、角色加载、UI 分别由对应 Controller 和 Manager 处理。

人物根节点挂 `CharacterController`、`PlayerInputSource`、`PlayerController`、`PlayerMovementSync`。
Animator 放在独立模型子物体，输入和角色控制脚本放在根节点；远端玩家只克隆外观。
Animator 继续使用 `State(int)`、`Speed(float)`：Idle=0、Move=1、Jump=2、Run=6、Fall=7。
Speed 是实际水平速度，角色和远端外观都关闭 Root Motion。

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
上行 Sequence 属于当前连接，用来丢弃重复或旧包；下行 Sequence 由 Game 递增生成。
序号比较支持 uint 回绕；换连接只重置上行序号。
场景加载期间只缓存每个远端角色的最新快照，避免持有会被回收的协议对象。

服务端检查有限数值、状态、速度和可累计的位移预算。
水平上限 9 米/秒，垂直上限 45 米/秒，报告速度上限 8 米/秒；修改客户端速度时同步调整规则常量。
站立时间最多累计一秒的移动预算；高频消息不会反复获得额外宽限。
拒绝位移后，把确认位置发给本人回正；收到旧包则直接丢弃。
`G2C_PlayerMove.RoleId` 等于本地角色时表示回正，否则表示远端移动。

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

## 验证

```powershell
dotnet build Server/Server.sln --no-restore
dotnet build Client/MMO_RPG/Assembly-CSharp.csproj --no-restore
dotnet run --project Server/Tests/Regression/Regression.csproj --no-restore
```

回归检查覆盖角色 DTO、持久化字段、正常移动、位移预算、非法快照、序号回绕、重连、密码、Token 和客户端回正。
检查不启动服务端、不连接数据库，Token 检查使用独立临时密钥。
首次运行测试项目时先执行 `dotnet restore Server/Tests/Regression/Regression.csproj`。
Unity 的生成项目文件会在导入时刷新，新增脚本无需手工维护 Assembly-CSharp.csproj。

场景联调需要在 Unity 用两个不同账号验证：互相看见移动/奔跑/跳跃、重复登录关闭旧连接、断线后重登恢复最终位置。
资源失败后的重新打开、相机跟随和具体模型动画也需要在编辑器中验证。
