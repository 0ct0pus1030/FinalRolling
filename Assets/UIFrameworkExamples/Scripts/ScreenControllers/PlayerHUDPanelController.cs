using FGLogic.State;
using TMPro;
using UIFramework;
using UIFramework.Examples;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using EventType = FGLogic.State.EventType;

namespace UIFramework.Rolling
{
    public class PlayerHUDPanelController : PanelController
    {
        [Header("UI元素")]
        [SerializeField] private Slider healthSlider;
        //[SerializeField] private TextMeshProUGUI frameText;
        [SerializeField] private TextMeshProUGUI eventText;
        [SerializeField] private Button exitButton;

        [Header("设置")]
        
        [SerializeField] private int maxHealth = 100;
        
        [Header("网络调试")] // 新增
        [SerializeField] private TextMeshProUGUI p0StateText;  // P0数据
        [SerializeField] private TextMeshProUGUI p1StateText;  // P1数据

        private BattleUIManager battleUI;

        void Start()
        {
            battleUI = FindObjectOfType<BattleUIManager>();
            if (exitButton != null)
            {
                exitButton.onClick.AddListener(OnExitClick);
            }
        }

        void OnDestroy()
        {
            // 移除按钮监听
            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(OnExitClick);
            }
        }

        void Update()
        {
            if (battleUI == null) return;

            var state = battleUI.GetCurrentState();
            if (state.PlayerCount == 0) return;

            int playerId = battleUI.GetLocalPlayerId();
            var player = state.Players[playerId];

            // 血条直接赋值（不需要插值回复）
            healthSlider.value = (float)player.Health / maxHealth;

            // 帧号
            //frameText.text = $"F:{state.FrameId}";
            
            //syncHashText.text = $"Hash: {battleUI.SyncHash:X8}";
            
            var p0 = state.Players[0];
            var p1 = state.Players[1];
            
            // P0: 位置(X,Y) 状态ID 血量
            p0StateText.text = $"P0:({p0.Position.X},{p0.Position.Y}) S{p0.StateId} HP{p0.Health}";
            
            // P1: 位置(X,Y) 状态ID 血量  
            p1StateText.text = $"P1:({p1.Position.X},{p1.Position.Y}) S{p1.StateId} HP{p1.Health}";

            //MDZ5检测

            //事件播报
            var evt = battleUI.GetLastEvent();
            eventText.text = FormatEvent(evt);
        }


        private string FormatEvent(GameEvent evt)
        {
            if (evt.Type == EventType.None) return "";

            string eventName = evt.Type switch
            {
                EventType.HitConfirm => "HIT",
                _ => "unknwon"
            };

            return $"P{evt.SourceId} {eventName} P{evt.TargetId} D{evt.Damage}";
        }


        private void OnExitClick()
        {
            Signals.Get<BackToSignal>().Dispatch(ScreenIds.ModeSelectionWindow);
        }
    }
}