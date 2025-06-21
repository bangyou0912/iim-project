using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public List<PlayerState> allPlayers = new List<PlayerState>();
    private int currentPlayerIndex = 0;

    public float turnTimeLimit = 10f;
    private float currentTimer;
    private bool isTurnActive = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        StartGame();
        foreach (var player in allPlayers)
        {
            Debug.Log("玩家手牌：" + string.Join(", ", player.handCards));
            Debug.Log("寶石數：" + player.gems);
        }
    }

    void Update()
    {
        if (isTurnActive)
        {
            currentTimer -= Time.deltaTime;
            if (currentTimer <= 0)
            {
                Debug.Log("玩家 " + (currentPlayerIndex + 1) + " 時間到！");
                EndTurn();
            }
        }
    }

    public void StartGame()
    {
        Debug.Log("開始遊戲！");
        allPlayers.Clear();

        for (int i = 0; i < 4; i++)
        {
            List<string> cards = GenerateInitialHand();
            PlayerState player = new PlayerState(cards);
            allPlayers.Add(player);
        }

        currentPlayerIndex = 0;
        StartTurn();
    }

    void StartTurn()
    {
        if (allPlayers[currentPlayerIndex].isEliminated)
        {
            Debug.Log("玩家 " + (currentPlayerIndex + 1) + " 已出局，跳過回合。");
            EndTurn();
            return;
        }

        currentTimer = turnTimeLimit;
        isTurnActive = true;

        Debug.Log("輪到玩家 " + (currentPlayerIndex + 1) + " 開始回合");
        // 這裡可以加：顯示倒數、更新 UI、允許操作等
    }

    public void EndTurn()
    {
        isTurnActive = false;

        currentPlayerIndex = (currentPlayerIndex + 1) % allPlayers.Count;
        Debug.Log("玩家 " + (currentPlayerIndex + 1) + " 結束回合，進入下一位");
        StartTurn();
    }

    List<string> GenerateInitialHand()
    {
        List<string> hand = new List<string>();
        string[] primary = { "洋紅", "黃", "青" };
        string[] secondary = { "紅", "綠", "藍" };

        for (int i = 0; i < 6; i++)
        {
            int r = Random.Range(0, primary.Length);
            hand.Add(primary[r]);
        }

        for (int i = 0; i < 2; i++)
        {
            int r = Random.Range(0, secondary.Length);
            hand.Add(secondary[r]);
        }

        return hand;
    }
}
