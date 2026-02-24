using FGLogic.State;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

//信号定义
public class ToStartSignal : ASignal { }
public class ToPVESignal : ASignal { }
public class ToPVPVPSignal : ASignal { }

//
public class BackToSignal : ASignal<string> { }

//
public class GameEventSignal : ASignal<GameEvent> { }

//
public class GameStateChangedSignal : ASignal<GameState> { }

public class SceneRouter : MonoBehaviour
{
    private static SceneRouter _instance;

    public static string TargetWindow { get; set; }

    void Awake()
    {
        // 单例模式
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        OnToStart();

        // 注册信号监听
        Signals.Get<ToStartSignal>().AddListener(OnToStart);
        Signals.Get<ToPVESignal>().AddListener(OnToPVE);
        Signals.Get<ToPVPVPSignal>().AddListener(OnToPVPVP);
        Signals.Get<BackToSignal>().AddListener(OnBackTo);
    }

    void OnDestroy()
    {
        Signals.Get<ToStartSignal>().RemoveListener(OnToStart);
        Signals.Get<ToPVESignal>().RemoveListener(OnToPVE);
        Signals.Get<ToPVPVPSignal>().RemoveListener(OnToPVPVP);
        Signals.Get<BackToSignal>().RemoveListener(OnBackTo);
    }

    private void OnToStart()
    {
        SceneManager.LoadScene("Start");  // 改成你的开始场景名
    }

    private void OnToPVE()
    {
        SceneManager.LoadScene("pve");    // 改成你的单机场景名
    }

    private void OnToPVPVP()
    {
        SceneManager.LoadScene("pvpvp");  // 改成你的联机场景名
        Debug.Log("lianjilianji");
    }

    private void OnBackTo(string targetWindowId)
    {
        // 静态记录目标窗口
        TargetWindow = targetWindowId;
        SceneManager.LoadScene("Start");
    }

}
