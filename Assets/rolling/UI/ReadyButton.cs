using UnityEngine;
using UnityEngine.UI;

public class ReadyButton : MonoBehaviour
{
    Button button;
    Text buttonText;

    void Start()
    {
        button = GetComponent<Button>();
        buttonText = GetComponentInChildren<Text>();

        // 防御检查
        if (button == null)
        {
            Debug.LogError("[ReadyButton] 找不到 Button 组件！");
            return;
        }
        if (buttonText == null)
        {
            //Debug.LogError("[ReadyButton] 找不到 Text 组件！");
        }

        button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        // 层层防御
        if (KcpNetworkManager.Instance == null)
        {
            Debug.LogError("[ReadyButton] KcpNetworkManager.Instance 为 null！尝试查找...");

            // 尝试强制查找
            var manager = FindObjectOfType<KcpNetworkManager>();
            if (manager != null)
            {
                Debug.Log("[ReadyButton] 找到 KcpNetworkManager，尝试赋值");
                // 如果 KcpNetworkManager 的 Awake 没执行，这里手动触发一下
                var prop = typeof(KcpNetworkManager).GetField("Instance",
                    System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.Public);
                if (prop != null)
                {
                    prop.SetValue(null, manager);
                    Debug.Log("[ReadyButton] 强制赋值 Instance 成功");
                }
            }
            else
            {
                Debug.LogError("[ReadyButton] 场景里根本找不到 KcpNetworkManager 物体！");
                return;
            }
        }

        // 再次检查
        if (KcpNetworkManager.Instance == null)
        {
            Debug.LogError("[ReadyButton] Instance 仍然为 null，无法继续");
            return;
        }

        // 发送准备
        KcpNetworkManager.Instance.SendReady();

        // 更新 UI
        if (button != null)
            button.interactable = false;
        if (buttonText != null)
            buttonText.text = "已准备";
        else
            Debug.Log("[ReadyButton] 已准备（但找不到 Text 组件更新文字）");
    }
}