# 登录、加载与选角界面

本次修改直接使用现有的 `Assets/Main/Init.unity` 和 `Assets/MMO_Hot/Scenes/Login.unity`。临时验证场景与临时编辑器脚本已删除。

## 日常调整

- 登录与注册布局：`Client/MMO_RPG/Assets/MMO_Hot/Prefabs/UI/LoginPanel.prefab`。
- 选角布局：`Client/MMO_RPG/Assets/MMO_Hot/Prefabs/UI/ChooseRolePanel.prefab`。
- 选角模型、名称和介绍：`Client/MMO_RPG/Assets/MMO_Hot/Configs/CharacterSelectionCatalog.asset`。
- 按钮、输入框、进度条底框和填充图片：`Client/MMO_RPG/Assets/Main/UI/Frontend/`。进度条位于启动资源目录，资源包尚未下载时也能显示。
- 选角背景：`Client/MMO_RPG/Assets/MMO_Hot/ArtRes/BK/ChoicePanel.jpg`。
- 生成图片的提示词及文件对应关系：`Docs/FrontendArtwork.json`。

选角列表目前包含使用 Role_JX_fbx_1 Avatar 的 MainRole 展示预制体和使用 Adolescent_U Avatar 的 NPC 展示预制体。左右按钮循环浏览，模型和文案由同一条配置更新。

选择下标仅控制展示，不提交给服务器。创建请求仍然只提交昵称；GameScene 和实际游戏角色的生成逻辑未改动，因此进入游戏仍使用原来的 Role_JX_fbx_1。

预览组件单独拥有相机、灯光、模型副本和透明渲染纹理；关闭面板时释放，重新打开时重建。预览关闭 MSAA：本机 D3D12 曾在使用 MSAA 的预览打开后发生设备错误，关闭后完成了两种模型和反复切换检查。

TMP 字体已开启多图集并预先补充界面文字、数字及符号，避免原单图集已满导致缺字。密码输入仍使用 TMP 密码遮罩，进度填充和百分比绑定原有下载事件。

## 验证记录（2026-10-21）

- 在原 Init 场景检查 50% 进度的填充与百分比。
- 通过 Host 模式从现有 HFS 地址加载版本 `20261021_133042`，进入原 Login 场景。
- 检查登录、注册面板及三个密码输入框的遮罩，打开与关闭注册页正常。
- 两个角色展示正常，介绍文字未溢出；下一项与上一项均可循环。
- 关闭选角后专用渲染纹理数量为 0；重新打开后为 1。
- HTTP 校验版本入口、清单、清单校验文件及抽样 Bundle，与本地发布文件一致。
- 最后一次 Unity 控制台错误数为 0。未提交注册、登录或创角业务请求，未新增测试账号。
- 验证后退出播放，恢复 Init 的 Host 模式与启用状态。

资源包已构建并同步到本机当前 HFS 使用的桌面 CDN 目录。这次增加了运行时组件并修改了内置 Init 界面，使用独立客户端时需要重新构建客户端；旧 exe 只更新 AB 不足以获得全部改动。
