using Fantasy;
using Fantasy.Async;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>昵称输入与角色创建结果展示。</summary>
public sealed class ChooseRolePanel : BasePanel
{
    /// <summary>提交创建角色的按钮，等待期间禁用。</summary>
    public Button createRoleButton;
    /// <summary>游戏内角色昵称输入框，不是登录账号。</summary>
    public TMP_InputField nicknameInput;
    /// <summary>只用于选角展示的模型和介绍配置。</summary>
    [SerializeField] private CharacterSelectionCatalog catalog;
    /// <summary>负责隔离渲染角色副本的展示组件。</summary>
    [SerializeField] private CharacterSelectionPreview preview;
    /// <summary>切换到上一个展示角色。</summary>
    [SerializeField] private Button previousButton;
    /// <summary>切换到下一个展示角色。</summary>
    [SerializeField] private Button nextButton;
    /// <summary>右侧角色名称。</summary>
    [SerializeField] private TMP_Text roleTitle;
    /// <summary>右侧角色身份简介。</summary>
    [SerializeField] private TMP_Text roleSubtitle;
    /// <summary>右侧角色背景介绍。</summary>
    [SerializeField] private TMP_Text roleDescription;
    /// <summary>右侧风格关键词。</summary>
    [SerializeField] private TMP_Text roleTraits;
    /// <summary>当前展示序号，例如 01 / 02。</summary>
    [SerializeField] private TMP_Text selectionCounter;
    /// <summary>当前选中展示项的数组下标；不发送给服务器。</summary>
    private int selectedIndex;
    /// <summary>供界面检查当前展示项，不代表游戏场景中的角色类型。</summary>
    public int SelectedIndex => selectedIndex;
    /// <summary>是否正在创建角色并进入游戏；防止重复提交昵称。</summary>
    private bool requesting;

    /// <summary>绑定创建按钮。</summary>
    protected override void OnInit()
    {
        createRoleButton.onClick.AddListener(() => CreateRoleAsync().Coroutine());
        previousButton.onClick.AddListener(() => ChangeSelection(-1));
        nextButton.onClick.AddListener(() => ChangeSelection(1));
    }

    protected override void OnShow() => RefreshSelection();

    private void ChangeSelection(int direction)
    {
        selectedIndex = (selectedIndex + direction + catalog.Characters.Length) % catalog.Characters.Length;
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        var entry = catalog.Characters[selectedIndex];
        roleTitle.text = entry.Title;
        roleSubtitle.text = entry.Subtitle;
        roleDescription.text = entry.Description;
        roleTraits.text = entry.Traits;
        selectionCounter.text = $"{selectedIndex + 1:00} / {catalog.Characters.Length:00}";
        preview.Show(entry.PreviewPrefab);
    }

    /// <summary>创建期间禁用按钮；成功才隐藏面板，异常不会让按钮永久失效。</summary>
    private async FTask CreateRoleAsync()
    {
        if (requesting) return;
        requesting = true;
        createRoleButton.interactable = false;
        previousButton.interactable = nextButton.interactable = false;
        try
        {
            // result：创建角色并进入世界的最终业务结果。
            var result = await RoleController.Instance.CreateRoleAsync(nicknameInput.text);
            switch (result)
            {
                case ResultCode.Success:
                    TipManager.Instance.ShowTip("角色创建成功");
                    Hide();
                    break;
                case ResultCode.InvalidNickname:
                    TipManager.Instance.ShowTip("昵称不能为空，且不能超过24个字符");
                    break;
                case ResultCode.NotAuthenticated:
                    Hide();
                    UIManager.Instance.ShowPanel<LoginPanel>();
                    TipManager.Instance.ShowTip("登录已失效，请重新登录");
                    break;
                default:
                    TipManager.Instance.ShowTip("服务器错误，请稍后重试");
                    break;
            }
        }
        finally
        {
            requesting = false;
            if (this != null)
            {
                createRoleButton.interactable = true;
                previousButton.interactable = nextButton.interactable = true;
            }
        }
    }
}
