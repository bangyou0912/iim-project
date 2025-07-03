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

        currentPlayerIndex = (currentPlayerIndex + 1) % PhotonNetwork.PlayerList.Length;
        int actorNumber = PhotonNetwork.PlayerList[currentPlayerIndex].ActorNumber;

        photonView.RPC("RPC_StartTurn", RpcTarget.All, actorNumber);
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

    IEnumerator CountdownTimer()
    {
        while (timeRemaining > 0)
        {
            turnTimerText.text = $"{Mathf.CeilToInt(timeRemaining)}"; 
            yield return new WaitForSeconds(0.1f);
            timeRemaining -= 0.1f;
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
            StartNextTurn();
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
