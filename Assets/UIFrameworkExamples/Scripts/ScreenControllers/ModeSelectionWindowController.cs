using UIFramework;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using UIFramework.Examples;


namespace UIFramework.Rolling
{
    //
    public enum GameMode
    {
        pvpvp,
        pve,
    }
    public class SelectModeSignal : ASignal<GameMode> { }
    public class BackToStartSignal : ASignal { }


    public class ModeSelectionWindowController : WindowController
    {
        //单人模式按钮点击时调用
        public void OnSingleClick()
        {
            Signals.Get<SelectModeSignal>().Dispatch(GameMode.pve);
        }

        // 联机模式按钮点击时调用
        public void OnMultiClick()
        {
            Signals.Get<SelectModeSignal>().Dispatch(GameMode.pvpvp);
        }

        // 返回按钮点击时调用
        public void OnBackClick()
        {
            // 发送导航信号，返回开始窗口
            Signals.Get<BackToStartSignal>().Dispatch();
        }

    }

}

