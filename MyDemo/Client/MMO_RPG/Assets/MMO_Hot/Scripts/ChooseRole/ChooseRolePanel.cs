using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChooseRolePanel : BasePanel
{
    public Button btnCreatRole;
    public TMP_InputField userName;
    protected override void OnInit()
    {
        btnCreatRole.onClick.AddListener(() =>
        {
            
        });
    }
}
