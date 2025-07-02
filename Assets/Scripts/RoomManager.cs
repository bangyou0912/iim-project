using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Text;
using Photon.Realtime;
using System.Linq;
public class RoomManager : MonoBehaviourPunCallbacks
{
    [SerializeField]
        TMP_Text textRoomName;
    [SerializeField]
    TMP_Text textPlayerList;
    [SerializeField]
    Button buttonStartGame;
    void Start()
    {
        if (PhotonNetwork.CurrentRoom == null)
        {
            SceneManager.LoadScene("LobbyScene");
        }
        else
        {
            textRoomName.text = PhotonNetwork.CurrentRoom.Name;
            UpdatePlayerList();
        }
        buttonStartGame.interactable = PhotonNetwork.IsMasterClient;
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        buttonStartGame.interactable = PhotonNetwork.IsMasterClient;
    }

    public void UpdatePlayerList()
    {
        StringBuilder sb = new StringBuilder();
        var sortedPlayers = PhotonNetwork.CurrentRoom.Players.OrderBy(kvp => kvp.Key); 
        foreach (var kvp in sortedPlayers)
        {
            sb.AppendLine(kvp.Value.NickName);
        }
        textPlayerList.text = sb.ToString();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();
    }

    public void OnClickStartGame()
    {
        SceneManager.LoadScene("RulePage1");
    }
    public void OnClickBackToHomePage()
    {
        SceneManager.LoadScene("StartScene");
    }

    public void OnClickLeaveGame()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("LobbyScene");
    }
}
