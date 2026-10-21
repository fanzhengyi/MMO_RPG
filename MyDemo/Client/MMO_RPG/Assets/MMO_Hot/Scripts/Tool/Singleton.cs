/// <summary>
/// 最简单的普通 C# 单例。
/// 子类只要继承：public class XxxManager : Singleton<XxxManager> { }
/// </summary>
public class Singleton<T> where T : new()
{
    /// <summary>首次访问 Instance 时创建并缓存的普通 C# 对象；不挂场景，也不接收 Unity 生命周期函数。</summary>
    private static T _instance;
    public static T Instance
    {
        get
        {
            if (_instance == null) _instance = new T();
            return _instance;
        }
    }
}
