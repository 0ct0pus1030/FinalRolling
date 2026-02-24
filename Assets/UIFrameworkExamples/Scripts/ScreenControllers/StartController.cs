using UIFramework;
using UnityEngine;
using UnityEngine.UI;
using Utils;


namespace UIFramework.Rolling

{

    public class StartGameSignal : ASignal { }   // 开始游戏信号
    public class ExitGameSignal : ASignal { }    // 退出游戏信号

    public class StartController : WindowController
    {

       

        public void UI_Start()
        {
            Signals.Get<StartGameSignal>().Dispatch();
        }

        public void UI_Exit()
        {
            Signals.Get<ExitGameSignal>().Dispatch();
        }

    }

}

