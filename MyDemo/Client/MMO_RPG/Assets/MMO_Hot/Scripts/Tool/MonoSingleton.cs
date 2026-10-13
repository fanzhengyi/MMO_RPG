using UnityEngine;

/// <summary>常驻 MonoBehaviour 单例；销毁时清除静态引用。</summary>
public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    public static T Instance { get; private set; }

    /// <summary>登记唯一实例，重复实例销毁，保留实例跨场景存在。</summary>
    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = (T)this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>仅由当前实例清理静态引用。</summary>
    protected virtual void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
