using UnityEngine;

/// <summary>
/// 最简单的 MonoBehaviour 单例。
/// 将子类脚本挂到场景物体上即可，重复的对象会被删除。
/// </summary>
public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    public static T Instance { get; private set; }

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
}
