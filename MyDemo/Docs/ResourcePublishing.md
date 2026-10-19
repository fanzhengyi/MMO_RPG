# Windows AB 资源发布

运行入口为 `Assets/Main/Init.unity`。客户端固定使用 YooAsset HostPlayMode；编辑器 Inspector 可选择本地模拟或远程资源。Login、GameScene 由资源包加载，不重复内置到安装包场景列表。

## 构建和本地发布

1. Unity 构建目标使用 Windows x86_64。
2. 运行菜单 `Tools/Resources/Prepare Bootstrap` 配置本地启动入口与重试按钮。
3. 运行 `Tools/Resources/Build and Stage Windows Bundles`。
4. 构建成功后，待发布文件位于项目仓库内的 `ResourceServer/CDN/StandaloneWindows64/DefaultPackage`。每次构建生成新的资源版本号。
5. 静态服务器将 `ResourceServer` 作为根目录；若使用文件服务器虚拟目录，则将其内部的 `CDN` 文件夹映射为 `/CDN`。

客户端的本机测试地址为 `http://127.0.0.1/CDN/StandaloneWindows64/DefaultPackage`。这里是固定发布地址，资源版本由 `DefaultPackage.version` 决定，更新时无需修改客户端 URL。

必须能直接下载以下文件，不能返回目录列表、登录页或重定向后的 HTML：

```text
DefaultPackage.version
DefaultPackage_<资源版本>.hash
DefaultPackage_<资源版本>.bytes
<文件哈希>.bundle
```

项目使用 LZ4、内容哈希 Bundle 文件名、共享依赖和构建结果验证。场景只收集 `.unity` 文件并逐个打包，避免与 LightingSettings 等普通资源混装。当前安装包只内置启动界面，Host 模式使用远程资源和下载缓存，不初始化不存在的内置 Catalog。构建会把 AB 引用的脚本类型防裁剪清单写入 `Assets/ResourceBuild/link.xml`，避免只在 AB 场景中出现的组件被 IL2CPP 裁剪；因此先构建资源，再打客户端。

版本文件在其余文件准备完成后原子替换，旧资源保留供旧清单继续使用。构建日志、报告和 link.xml 不作为资源下载内容发布。

## 验证

```powershell
./Tools/Test-ResourceServer.ps1
# 检查所有 Bundle 的 HTTP 下载内容与本地发布目录一致
./Tools/Test-ResourceServer.ps1 -AllBundles
```

默认检查版本、清单、校验文件和一个 Bundle 的 SHA-256，不依赖浏览器页面看起来是否正常。服务器挂载、端口、防火墙错误会直接报错。

编辑器验证远程更新时，将 Init 的 Play Mode 临时改为 HostPlayMode 后运行；不要使用 EditorSimulateMode 验证下载。启动失败时显示重试按钮；重试不会重复初始化已经成功的资源包，也不会并发启动多个下载流程。登录场景加载成功后才清理未使用缓存，缓存清理失败不会阻止进入游戏。

至少验证首次下载、缓存启动、失败后恢复重试和新版本增量更新。编辑器沙盒与安装包沙盒不同，安装包仍需单独验证。

本轮 Unity MCP 验证记录位于 `Client/MMO_RPG/Library/Verification/Resources/`：

- 主动让版本文件返回 404，启动显示重试按钮；资源包初始化已成功。
- 恢复 HTTP 服务后点击实际按钮，下载 135 个资源文件并进入 Login；资源包和初始化操作对象均未重复创建。
- 同版本第二次启动进入 Login，资源下载数为 0。
- 再发布一版，134 个 Bundle 保持不变、1 个 Bundle 变化；客户端只下载 1 个文件后进入 Login。
- HTTP 校验覆盖版本、清单、校验文件、普通 Bundle 及无扩展名的内置 Shader/脚本 Bundle，逐文件比较 SHA-256。
- Windows IL2CPP 验证包构建成功，0 个错误；以无图形模式独立启动后，从 HTTP 服务加载资源并输出 Login 场景就绪日志，没有运行时异常。该检查验证资源和代码加载流程，不包含画面或联网战斗体验。

Windows 验证包位于 `Client/MMO_RPG/Builds/ResourceSmoke/`，使用本轮临时回环服务端口 18081，不用于分发。临时服务测试结束后已停止；当前项目配置已恢复为 HFS 的 80 端口，并保持 HostPlayMode。正式安装包在挂载 HFS 或配置正式 HTTPS 地址后重新构建。

这些检查使用临时回环 HTTP 服务，不代表已经完成 HFS 的虚拟目录挂载或公网部署。HFS 当前需要将 `ResourceServer/CDN` 作为**真实文件夹**添加到根目录；不要额外嵌套一层 `ResourceServer`。

## 其他设备及正式服务

- 其他设备不能使用 `127.0.0.1`；改成服务器的局域网地址，并允许对应端口的入站访问。不要使用 Clash 的 `198.18.0.1` 虚拟网卡地址。
- HTTP 仅用于本机或受控开发环境。当前项目只对 Development Build 放开非本机 HTTP；正式域名使用 HTTPS。
- 服务器按平台、资源包和客户端兼容版本分开部署；不把新编辑器/不兼容脚本生成的资源清单直接交给旧安装包。
- 正式部署设置 `.version` 不缓存，内容哈希 Bundle 可长期缓存；先上传全部依赖与清单，最后发布版本指针，并保留可回退的旧版本。
- HFS 2.4 RC7 有已知严重漏洞，不用于公网正式服务。项目不自动修改正在运行的旧版 HFS，也不开放公网端口。
- 当前实现为资源更新。C# 源文件不会通过 AB 热更新；HybridCLR 程序集拆分、DLL 加载和 AOT 元数据是独立工作。
