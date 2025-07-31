using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class EndSceneManager : MonoBehaviourPun
{
    public GameObject panelGameOver;
    public GameObject panelRanking;
    public Button replayButton;

    public Transform rankingContainer; // 排行榜父物件
    public GameObject rankingPrefab;   // 單個玩家排位UI prefab（顯示名稱與寶石數）

    private void Start()
    {
        panelGameOver.SetActive(true);
        panelRanking.SetActive(false);
        replayButton.onClick.AddListener(OnReplayClicked);

        StartCoroutine(ShowRankingAfterDelay(2f));
    }

    IEnumerator ShowRankingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        panelGameOver.SetActive(false);
        panelRanking.SetActive(true);
        DisplayRanking();
    }

    void DisplayRanking()
    {
        Player[] players = PhotonNetwork.PlayerList;

        List<(string name, int gem)> playerData = new List<(string, int)>();
        foreach (Player p in players)
        {
            object gemVal;
            p.CustomProperties.TryGetValue("finalGem", out gemVal);
            int gem = gemVal != null ? (int)gemVal : 0;
            playerData.Add((p.NickName, gem));
        }

        playerData.Sort((a, b) => b.gem.CompareTo(a.gem)); // 高分在前

        foreach (var pd in playerData)
        {
            GameObject item = Instantiate(rankingPrefab, rankingContainer);
            item.transform.Find("PlayerName").GetComponent<TMP_Text>().text = pd.name;
            item.transform.Find("GemCount").GetComponent<TMP_Text>().text = pd.gem.ToString();
        }
    }

    void OnReplayClicked()
    {
        StartCoroutine(LeaveAndReturnToStart());
    }

    IEnumerator LeaveAndReturnToStart()
    {
        PhotonNetwork.LeaveRoom();

        // 等待成功離開房間
        while (PhotonNetwork.InRoom)
        {
            yield return null;
        }

        // 進入 StartScene
        SceneManager.LoadScene("StartScene");
    }
}
