# 人物移动

人物上只挂两个自定义脚本：

- `PlayerInputSource` 读取 Move、Run、Jump。
- `PlayerFsmCore` 管理 Idle、Move、Run、Jump、Fall。
- `PlayerController` 根据状态移动胶囊并处理相机方向、转向和重力。

人物根节点挂 `CharacterController`、`PlayerInputSource`、`PlayerController`。模型子物体保留 Unity `Animator`。FSM 是普通 C# 对象，由 `PlayerController` 创建；控制器同时更新 Animator 参数。

InputSource 中配置 Move、Run、Jump 三个 Input Actions。Controller 的 Camera Root 留空时会查找带 `MainCamera` 标签的相机；Cinemachine 场景应让实际输出相机带此标签。

Animator 使用 `State(int)` 和 `Speed(float)`。状态编号沿用现有 Controller：Idle=0、Move=1、Jump=2、Run=6、Fall=7。Speed 使用实际水平速度，供 Locomotion Blend Tree 使用。
