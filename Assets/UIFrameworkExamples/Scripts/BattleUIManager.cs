using UnityEngine;
using FGLogic.State;
using Utils; 

public class BattleUIManager : MonoBehaviour
{
    [Header("模式")]
    [SerializeField] private bool autoDetectNetworkMode = true;



    private GameState currentState;
    private GameEvent lastEvent;
    //联机临时不做，本地默认0
    private int localPlayerId = 0;
    private bool isNetworkMode = false;

    void Start()
    {
        if (autoDetectNetworkMode)
        {
            var netController = NetworkBattleController.Instance;
            if (netController != null)
            {
                isNetworkMode = true;
                localPlayerId = netController.GetMyPlayerId();  // ← 关键！
                Debug.Log($"[BattleUIManager] 网络模式，本地玩家: {localPlayerId}");
            }
            else
            {
                isNetworkMode = false;
                localPlayerId = 0;  // 本地默认 P0
            }
        }
    }

    public int GetLocalPlayerId() => localPlayerId;

    void OnEnable()
    {
        Signals.Get<GameStateChangedSignal>().AddListener(OnStateChanged);
        Signals.Get<GameEventSignal>().AddListener(OnGameEvent);
    }

    void OnDisable()
    {
        Signals.Get<GameStateChangedSignal>().RemoveListener(OnStateChanged);
        Signals.Get<GameEventSignal>().RemoveListener(OnGameEvent);
    }

    private void OnStateChanged(GameState newState)
    {
        currentState = newState;
    }

    
    public GameState GetCurrentState()
    {
        return currentState;
    }
   
    public PlayerState GetPlayerState(int playerId)
    {
        return currentState.Players[playerId];
    }

    private void OnGameEvent(GameEvent evt)
    {
        lastEvent = evt;
    }

    public GameEvent GetLastEvent()
    {
        return lastEvent;
    }
}