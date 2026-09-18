/// <summary>
/// 最简单的普通 C# 单例。
/// 子类只要继承：public class XxxManager : Singleton<XxxManager> { }
/// </summary>
public class Singleton<T> where T : new()
{
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
