using System.Collections;
using System.Collections.Generic;
using UIFramework;
using UIFramework.Examples;
using UnityEngine;
using Utils;
using UIFramework.Rolling;

public class BattleUIController : MonoBehaviour
{

    [SerializeField] private UISettings uiSettings;

    private UIFrame uiFrame;


    void Awake()
    {
        // 创建 UI 实例
        uiFrame = uiSettings.CreateUIInstance();
        
    }

    void Start()
    {
        uiFrame.ShowPanel(ScreenIds.PlayerHUDPanel);
    }

    void Update()
    {
        
    }
}
