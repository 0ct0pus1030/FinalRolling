using System;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using TMPro;

namespace UIFramework.Rolling
{
    public class ShowPlayerReadyPopupSignal : ASignal<PlayerReadyProperties> { }

    public class MatchSuccessSignal : ASignal { }



    [Serializable]
    public class PlayerReadyProperties : WindowProperties
    {
        public readonly string Title;
        public readonly string ConfirmText;      // 初始确认按钮文字
        public readonly string MatchingText;     // 匹配中按钮文字
        public readonly string CloseText;
        public readonly Action OnConfirm;        // 点击确认
        public readonly Action OnCancel;         // 点击关闭/取消匹配

        public PlayerReadyProperties(
            string title = "pvp",
            string confirmText = "already",
            string matchingText = "loading...",
            string closeText = "close",
            Action onConfirm = null,
            Action onCancel = null)
        {
            Title = title;
            ConfirmText = confirmText;
            MatchingText = matchingText;
            CloseText = closeText;
            OnConfirm = onConfirm;
            OnCancel = onCancel;
        }
    }

    public class PlayerReadyWindowController : WindowController<PlayerReadyProperties>
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text confirmButtonText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text closeButtonText;
        

        private bool isMatching = false;

        protected override void OnPropertiesSet()
        {
            isMatching = false;
            UpdateUIState();
        }

        // 确认/准备按钮
        public void OnConfirm()
        {
            if (isMatching) return;  // 已经在匹配中，忽略

            isMatching = true;
            UpdateUIState();

            // 通知外部开始匹配
            Properties.OnConfirm?.Invoke();
            
        }

        // 关闭/取消按钮
        public void OnCancel()
        {
            if (isMatching)
            {
                // 如果在匹配中，点击关闭=取消匹配
                isMatching = false;
                Properties.OnCancel?.Invoke();
            }

            UI_Close();
        }

        // 外部调用：匹配成功
        public void OnMatchFound()
        {
            UI_Close();
            // 或者切换到"进入游戏"按钮
        }

        // 外部调用：匹配失败
        public void OnMatchFailed(string error)
        {
            isMatching = false;
            UpdateUIState();
            // 显示错误提示
            titleText.text = $"false: {error}";
        }

        private void UpdateUIState()
        {
            titleText.text = Properties.Title;
            closeButtonText.text = isMatching ? "cancel" : Properties.CloseText;

            if (isMatching)
            {
                confirmButtonText.text = Properties.MatchingText;
                confirmButton.interactable = false;
                
            }
            else
            {
                confirmButtonText.text = Properties.ConfirmText;
                confirmButton.interactable = true;
                
            }
        }
    }
}