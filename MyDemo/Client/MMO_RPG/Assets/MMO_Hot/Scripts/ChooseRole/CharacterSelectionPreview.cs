using UnityEngine;
using UnityEngine.UI;

/// <summary>独立管理选角模型、灯光和透明预览纹理，不依赖登录、协议或游戏角色控制器。</summary>
public sealed class CharacterSelectionPreview : MonoBehaviour
{
    /// <summary>接收角色透明渲染结果的 UI 控件。</summary>
    [SerializeField] private RawImage output;
    /// <summary>预览模型和相机的临时根节点，离开选角页时销毁。</summary>
    private GameObject stage;
    /// <summary>本次正在显示的模型副本。</summary>
    private GameObject model;
    /// <summary>本组件拥有的渲染纹理，不与其他界面共享。</summary>
    private RenderTexture target;
    /// <summary>选角模型专用层；相机只渲染该层。</summary>
    private const int PreviewLayer = 6;

    /// <summary>换模型时仅操作展示副本；保留源预制体的材质、骨骼和待机控制器。</summary>
    public void Show(GameObject prefab)
    {
        if (stage == null) CreateStage();
        if (model != null)
        {
            model.SetActive(false);
            Destroy(model);
        }
        model = Instantiate(prefab, stage.transform, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        SetLayer(model.transform);
        foreach (var animator in model.GetComponentsInChildren<Animator>())
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Update(0f);
        }

        // bounds 是模型当前世界包围盒；统一身高和脚底位置，避免切换后忽大忽小或漂浮。
        var bounds = ModelBounds();
        model.transform.localScale *= 2.7f / bounds.size.y;
        bounds = ModelBounds();
        model.transform.position += new Vector3(stage.transform.position.x - bounds.center.x,
            stage.transform.position.y - bounds.min.y, stage.transform.position.z - bounds.center.z);
    }

    private void CreateStage()
    {
        stage = new GameObject("CharacterSelectionStage");
        // 放在远离游戏和登录场景的位置，专用层和相机视锥共同隔离其他场景物体。
        stage.transform.position = new Vector3(10000f, 10000f, 10000f);
        // UI 根节点跨场景保留，预览也由面板管理，关闭面板时统一释放。
        DontDestroyOnLoad(stage);
        target = new RenderTexture(960, 900, 24, RenderTextureFormat.ARGB32)
        {
            name = "CharacterSelectionPreview", antiAliasing = 1
        };
        target.Create();
        output.texture = target;
        var cameraObject = new GameObject("PreviewCamera");
        cameraObject.transform.SetParent(stage.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 1.45f, -6f);
        cameraObject.transform.LookAt(stage.transform.position + Vector3.up * 1.4f);
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.clear;
        camera.cullingMask = 1 << PreviewLayer;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 15f;
        camera.fieldOfView = 30f;
        camera.targetTexture = target;
        camera.allowHDR = false;
        camera.allowMSAA = false;
        AddLight("KeyLight", new Vector3(25f, -25f, 0f), new Color(1f, 0.91f, 0.8f), 0.8f);
        AddLight("FillLight", new Vector3(10f, 145f, 0f), new Color(0.55f, 0.78f, 1f), 0.45f);
    }

    private void AddLight(string lightName, Vector3 angles, Color color, float intensity)
    {
        var lightObject = new GameObject(lightName);
        lightObject.transform.SetParent(stage.transform, false);
        lightObject.transform.localRotation = Quaternion.Euler(angles);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = color;
        light.intensity = intensity;
        light.cullingMask = 1 << PreviewLayer;
        light.shadows = LightShadows.None;
    }

    private Bounds ModelBounds()
    {
        var renderers = model.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void SetLayer(Transform root)
    {
        root.gameObject.layer = PreviewLayer;
        foreach (Transform child in root) SetLayer(child);
    }

    /// <summary>关闭界面立即释放模型和渲染纹理，下次显示时重新准备。</summary>
    private void OnDisable()
    {
        if (stage == null) return;
        stage.SetActive(false);
        Destroy(stage);
        stage = null;
        model = null;
        output.texture = null;
        target.Release();
        Destroy(target);
        target = null;
    }
}
