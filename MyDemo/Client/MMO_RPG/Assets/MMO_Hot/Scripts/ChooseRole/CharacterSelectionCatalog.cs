using System;
using UnityEngine;

/// <summary>选角页的展示配置；只描述外观和文案，不决定进入世界后的角色模型。</summary>
[CreateAssetMenu(menuName = "Game/UI/Character Selection Catalog", fileName = "CharacterSelectionCatalog")]
public sealed class CharacterSelectionCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        /// <summary>选角页使用的模型预制体，不会作为游戏场景的出生角色。</summary>
        public GameObject PreviewPrefab;
        /// <summary>右侧显示的角色名称。</summary>
        public string Title;
        /// <summary>名称下方的一句话身份介绍。</summary>
        public string Subtitle;
        /// <summary>角色的背景故事，不代表已经实现对应战斗技能。</summary>
        [TextArea(3, 8)] public string Description;
        /// <summary>展示角色风格的简短关键词。</summary>
        public string Traits;
    }

    /// <summary>左右按钮循环浏览的角色列表。</summary>
    public Entry[] Characters;
}
