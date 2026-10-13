using UnityEngine;

/// <summary>查找任意层级中的指定子物体。</summary>
public static class DeepFindTransform
{
    /// <summary>先查当前层，再递归查找后代，未找到返回 null。</summary>
    public static Transform DeepFindChild(Transform root, string childName)
    {
        var result = root.Find(childName);
        if (!result)
        {
            foreach (Transform item in root)
            {
                result = DeepFindChild(item, childName);
                if (result != null)
                {
                    return result;
                }
            }
        }
        return result;
    }
}
