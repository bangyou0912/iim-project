using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class TurnManager : MonoBehaviourPunCallbacks
{
    //單例與狀態判斷
    public static TurnManager Instance;
    public static bool IsMyTurn => Instance != null && Instance.currentTurnActor == PhotonNetwork.LocalPlayer.ActorNumber;

    //回合進度控制
    private bool hasShownFirstTurnNotice = false;
    public int currentTurnActor = -1;
    private int currentPlayerIndex = -1;
    public bool isPaused = false;
    private Coroutine turnCountdown;

    //倒數計時功能（可選）
    [Header("可選功能")]
    public bool enableTurnTimer = false;  // 預設不使用倒數計時

    [Header("回合時間設定")]
    [Tooltip("每回合持續時間（秒）")]
    public float turnDuration = 10f;
    private float timeRemaining = 0f;

    //UI 元件：回合控制按鈕與計時
    public Button endTurnButton;
    public TMP_Text turnTimerText;

    //UI 元件：回合提示面板
    [Header("回合提示面板")]
    public GameObject turnNoticeYouPanel;
    public GameObject turnNoticeOtherPanel;
    public TMP_Text otherPlayerNameText;
    public TMP_Text currentTurnPlayerNameText;
    private Coroutine noticeCoroutine;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
        endTurnButton.gameObject.SetActive(false);
        turnTimerText.gameObject.SetActive(false);
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

            //若未出局或還有手牌就進入回合
            if (!GameSceneManager.Instance.eliminatedPlayers.Contains(actorNumber) &&
                (fallbackAllow || GameSceneManager.Instance.PlayerHasHandCard(actorNumber)))
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
       
        if (GameSceneManager.Instance != null &&
       GameSceneManager.Instance.eliminatedPlayers.Contains(actorNumber))
        {
            Debug.Log($"[自動跳過回合] 玩家 {actorNumber} 已出局");
            CompleteMyTurn();
            return;
        }

        bool isMyTurn = (PhotonNetwork.LocalPlayer.ActorNumber == actorNumber);
        Debug.Log(isMyTurn ? "輪到我動作" : $"等待玩家 {actorNumber}");

        if (turnCountdown != null) StopCoroutine(turnCountdown);
        //停止前一輪提示圖協程，避免多個同時顯示
        if (noticeCoroutine != null) StopCoroutine(noticeCoroutine);
        if (isMyTurn)
        {
            // 在自己回合開始時重置寶石花費與自選色狀態
            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.ResetGemSpent();
            }

            // 若沒手牌則立即跳過
            if (GameSceneManager.Instance != null &&
                !GameSceneManager.Instance.PlayerHasHandCard(actorNumber))
            {
                Debug.Log("[自動跳過回合] 玩家無手牌，立即結束回合");
                CompleteMyTurn();
                return;
            }

            if (enableTurnTimer)
            {
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
        // 顯示提示圖
        noticeCoroutine = StartCoroutine(ShowTurnNoticeWithDelay(actorNumber));

        if (currentTurnPlayerNameText != null)
        {
            var player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            string nickname = player != null ? player.NickName : $"{actorNumber}";
            currentTurnPlayerNameText.text = $"{nickname}";
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

    public void CompleteMyTurn()
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

    void ShowTurnNotice(int actorNumber)
    {
        if (turnNoticeYouPanel != null) turnNoticeYouPanel.SetActive(false);
        if (turnNoticeOtherPanel != null) turnNoticeOtherPanel.SetActive(false);

        if (PhotonNetwork.LocalPlayer.ActorNumber == actorNumber)
        {
            // 自己的回合 → 顯示「輪到你了」圖
            if (turnNoticeYouPanel != null)
                turnNoticeYouPanel.SetActive(true);
        }
        else
        {
            // 別人的回合 → 顯示「輪到 XX」
            var player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            string nickname = player != null ? player.NickName : $"{actorNumber}";

            if (otherPlayerNameText != null)
                otherPlayerNameText.text = $"{nickname}";

            if (turnNoticeOtherPanel != null)
                turnNoticeOtherPanel.SetActive(true);
        }

        StartCoroutine(HideTurnNoticeAfterDelay(2f));
    }

    IEnumerator ShowTurnNoticeWithDelay(int actorNumber)
    {
        // 第一次才等待
        if (!hasShownFirstTurnNotice)
        {
            for (int i = 0; i < 5; i++)
            {
                if (GameSceneManager.Instance == null || !GameSceneManager.Instance.isWhiteCardExchangeInProgress)
                {
                    Debug.Log("等待白卡交換完成...");
                }
                yield return new WaitForSeconds(2f);
            }

            hasShownFirstTurnNotice = true;
        }

        yield return new WaitForSeconds(2f);  // 原本就有的延遲
        //確保當前回合還是這個人再顯示提示圖
        if (currentTurnActor == actorNumber)
        {
            ShowTurnNotice(actorNumber);
        }
    }

    IEnumerator HideTurnNoticeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (turnNoticeYouPanel != null) turnNoticeYouPanel.SetActive(false);
        if (turnNoticeOtherPanel != null) turnNoticeOtherPanel.SetActive(false);
    }
}
