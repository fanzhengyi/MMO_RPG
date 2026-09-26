using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public Button btnTest;
    void Start()
    {
        btnTest.onClick.AddListener(() =>
        {
           TipManager.Instance.ShowTip("测试"); 
        });
    }
}
