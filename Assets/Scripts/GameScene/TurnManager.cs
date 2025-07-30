using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class TurnManager : MonoBehaviourPunCallbacks
{
    public static TurnManager Instance;

    public Button endTurnButton;
    public TMP_Text turnTimerText;

    [Header("回合時間設定")]
    [Tooltip("每回合持續時間（秒）")]
    public float turnDuration = 10f; 

    private float timeRemaining = 0f; 

    public int currentTurnActor = -1;
    private int currentPlayerIndex = -1;
    public bool isPaused = false;
    private Coroutine turnCountdown;

    public static bool IsMyTurn => Instance != null && Instance.currentTurnActor == PhotonNetwork.LocalPlayer.ActorNumber;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
        endTurnButton.gameObject.SetActive(false);
        turnTimerText.gameObject.SetActive(false);

        if (PhotonNetwork.IsMasterClient)
        {
            StartGame();
        }
    }

    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient)
            StartNextTurn();
    }

    public void StartNextTurn()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int attempts = 0;
        int totalPlayers = PhotonNetwork.PlayerList.Length;

        do
        {
            currentPlayerIndex = (currentPlayerIndex + 1) % totalPlayers;
            int actorNumber = PhotonNetwork.PlayerList[currentPlayerIndex].ActorNumber;

            bool fallbackAllow = attempts == 0; // 第一圈允許啟動

            if (GameSceneManager.Instance == null)
            {
                Debug.LogWarning("GameSceneManager 尚未初始化，允許 actor " + actorNumber + " 進入回合");
                photonView.RPC("RPC_StartTurn", RpcTarget.All, actorNumber);
                return;
            }

            if (fallbackAllow || GameSceneManager.Instance.PlayerHasHandCard(actorNumber))
            {
                Debug.Log("進入回合：" + actorNumber);
                photonView.RPC("RPC_StartTurn", RpcTarget.All, actorNumber);
                return;
            }

            attempts++;

        } while (attempts < totalPlayers);

        Debug.LogWarning("所有玩家皆無手牌，停止回合輪轉");
    }

    IEnumerator DelayStartNextTurn()
    {
        yield return new WaitForSeconds(0.5f); // 給時間同步資料
        StartNextTurn();
    }

    [PunRPC]
    void RPC_StartTurn(int actorNumber)
    {
        currentTurnActor = actorNumber;

        bool isMyTurn = (PhotonNetwork.LocalPlayer.ActorNumber == actorNumber);
        Debug.Log(isMyTurn ? "輪到我動作" : $"等待玩家 {actorNumber}");

        if (turnCountdown != null) StopCoroutine(turnCountdown);

        if (isMyTurn)
        {
            // 如果我沒有手牌，立即結束回合
            if (GameSceneManager.Instance != null &&
                !GameSceneManager.Instance.PlayerHasHandCard(actorNumber))
            {
                Debug.Log("[自動跳過回合] 玩家無手牌，立即結束回合");
                CompleteMyTurn();
                return;
            }

            timeRemaining = turnDuration;
            endTurnButton.gameObject.SetActive(true);
            turnTimerText.gameObject.SetActive(true);
            turnCountdown = StartCoroutine(CountdownTimer());
        }
        else
        {
            endTurnButton.gameObject.SetActive(false);
            turnTimerText.gameObject.SetActive(false);
        }
    }

    [PunRPC]
    public void RPC_PauseTurnTimer()
    {
        isPaused = true;
    }

    [PunRPC]
    public void RPC_ResumeTurnTimer()
    {
        isPaused = false;
    }

    IEnumerator CountdownTimer()
    {
        while (timeRemaining > 0)
        {
            if (!isPaused)
            {
                timeRemaining -= 0.1f;
                turnTimerText.text = $"{Mathf.CeilToInt(timeRemaining)}";
            }

            yield return new WaitForSeconds(0.1f);
        }

        Debug.Log("時間到，自動結束回合");
        CompleteMyTurn();
    }

    void OnEndTurnButtonClicked()
    {
        if (!IsMyTurn) return;

        Debug.Log("玩家手動點擊：完成回合");
        CompleteMyTurn();
    }

    void CompleteMyTurn()
    {
        if (turnCountdown != null) StopCoroutine(turnCountdown);
        endTurnButton.gameObject.SetActive(false);
        turnTimerText.gameObject.SetActive(false);

        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(DelayStartNextTurn());
        }
        else
        {
            photonView.RPC("RequestNextTurn", RpcTarget.MasterClient);
        }
    }

    [PunRPC]
    void RequestNextTurn()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartNextTurn();
        }
    }
}
