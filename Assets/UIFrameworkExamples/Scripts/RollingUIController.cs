using System.Collections;
using System.Collections.Generic;
using UIFramework;
using UIFramework.Examples;
using UnityEngine;
using Utils;
using UIFramework.Rolling;


namespace UIFramework.Rolling {
    public class RollingUIController : MonoBehaviour
    {
        [SerializeField] private UISettings uiSettings;
        [SerializeField] private NetworkBattleController nbcPrefab;

        private UIFrame uiFrame;
        private NetworkBattleController currentNBC;
        private PlayerReadyWindowController currentReadyWindow;

        private void Awake()
        {
            CleanupResidualNBC();

            uiFrame = uiSettings.CreateUIInstance();

            // 监听你需要的信号
            Signals.Get<StartGameSignal>().AddListener(OnStartGame);
            Signals.Get<ExitGameSignal>().AddListener(OnExitGame);
            Signals.Get<SelectModeSignal>().AddListener(OnSelectMode);
            Signals.Get<BackToStartSignal>().AddListener(OnBackToStart);
            
        }

        void CleanupResidualNBC()
        {
            // 清理静态Instance引用
            if (NetworkBattleController.Instance != null)
            {
                Debug.Log("[UI] 清理残留的 NBC");
                Destroy(NetworkBattleController.Instance.gameObject);
                NetworkBattleController.Instance = null;
            }

            // 同时清理本地引用
            currentNBC = null;
        }

        private void OnDestroy()
        {
            Signals.Get<StartGameSignal>().RemoveListener(OnStartGame);
            Signals.Get<ExitGameSignal>().RemoveListener(OnExitGame);
            Signals.Get<SelectModeSignal>().RemoveListener(OnSelectMode);
            Signals.Get<BackToStartSignal>().RemoveListener(OnBackToStart);
            
        }

        private void Start()
        {
            Debug.Log($"{ SceneRouter.TargetWindow}");
            if (!string.IsNullOrEmpty(SceneRouter.TargetWindow))
            {
                uiFrame.OpenWindow(SceneRouter.TargetWindow);
                SceneRouter.TargetWindow = null; // 用完清空
            }
            else
            {
                uiFrame.OpenWindow(ScreenIds.Start);
            }
        }
        private void OnStartGame()
        {
            uiFrame.CloseCurrentWindow();
            uiFrame.OpenWindow(ScreenIds.ModeSelectionWindow);
        }

        private void OnExitGame()
        {
            Debug.Log("exit");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        //选择模式，可以补充
        private void OnSelectMode(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.pve:
                    Debug.Log("进入单人模式");
                    CleanupResidualNBC();
                    Signals.Get<ToPVESignal>().Dispatch();
                    break;

                case GameMode.pvpvp:
                    Debug.Log("进入联机模式");
                    ShowPlayerReadyPopup();
                    break;
            }
        }

        private void OnNavigateToWindow(string windowId)
        {
            // 可以直接打开目标窗口，也可以先关闭当前窗口再打开
            uiFrame.OpenWindow(windowId);
        }

        // 返回开始界面
        private void OnBackToStart()
        {
            //CleanupResidualNBC();
            uiFrame.CloseCurrentWindow();  // 关闭模式选择
            uiFrame.OpenWindow(ScreenIds.Start);
        }

        // 处理准备状态变化（信号参数为 bool）
        private void OnReady(bool ready)
        {
            Debug.Log($"玩家准备状态: {ready}");
            // 可在此处执行后续操作（如收集所有玩家准备状态）
        }

        // 处理准备窗口关闭
        private void OnReadyWindowClosed()
        {
            Debug.Log("准备窗口关闭，返回模式选择");
            uiFrame.CloseWindow(ScreenIds.PlayerReadyWindow); // 确保窗口关闭
            uiFrame.OpenWindow(ScreenIds.ModeSelectionWindow);
        }

        private void ShowPlayerReadyPopup()
        {
            var props = new PlayerReadyProperties(
                title: "pvpvp",
                confirmText: "ready",
                matchingText: "loading...",
                closeText: "close",
                onConfirm: () =>
                {
                    Debug.Log("开始匹配...");
                    // 调用网络管理器开始匹配
                    NetworkBattleController.Instance.StartMatching();
                },
                onCancel: () =>
                {
                    Debug.Log("取消匹配");
                    NetworkBattleController.Instance.CancelMatching();
                }
            );

            uiFrame.OpenWindow(ScreenIds.PlayerReadyWindow, props);
        }

        void DestroyNBC()
        {
            if (currentNBC != null)
            {
                currentNBC.CancelMatching();
                Destroy(currentNBC.gameObject);
                currentNBC = null;
            }
            // 同时确保静态引用也清空
            if (NetworkBattleController.Instance != null)
            {
                NetworkBattleController.Instance = null;
            }
        }
    }

}

