using UnityEngine;

/// <summary>登录背景特效；公开方法由 Timeline 动画事件调用。</summary>
public class Frostwurmnorthrend : MonoBehaviour
{
    /// <summary>龙落地时播放的烟雾粒子，由动画事件控制。</summary>
    private ParticleSystem smokePS;
    /// <summary>龙吐息时播放的冰霜粒子，沿用 fire 命名但表现为冰霜。</summary>
    private ParticleSystem firePS;

    /// <summary>缓存特效组件，初始隐藏。</summary>
    private void Awake()
    {
        smokePS = transform.Find("Smoke").GetComponent<ParticleSystem>();
        firePS = DeepFindTransform.DeepFindChild(transform, "FrostSpray")
            .GetComponent<ParticleSystem>();
        HideAll();
    }


    /// <summary>动画事件：播放落地烟雾。</summary>
    public void PlayGroundSmoke()
    {
        smokePS.gameObject.SetActive(true);
        smokePS.Play();
    }

    /// <summary>动画事件：播放冰霜吐息。</summary>
    public void PlayIceFire()
    {
        firePS.gameObject.SetActive(true);
        firePS.Play();
    }

    /// <summary>动画事件：停止吐息发射，等待已有粒子消散。</summary>
    public void StopIceFire()
    {
        firePS.Stop();
    }

    /// <summary>动画事件：停止并隐藏所有特效。</summary>
    public void HideAll()
    {
        firePS.Stop();
        smokePS.Stop();
        firePS.gameObject.SetActive(false);
        smokePS.gameObject.SetActive(false);
    }
}
