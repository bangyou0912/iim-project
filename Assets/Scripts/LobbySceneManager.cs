using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LobbySceneManager : MonoBehaviourPunCallbacks
{
    [SerializeField]
    TMP_InputField inputRoomName;
    public void Start()
    {
        if (PhotonNetwork.IsConnected == false)
        {
            SceneManager.LoadScene("StartScene");
        }
        else
        {
            PhotonNetwork.JoinLobby();
        }
    }
    public override void OnJoinedLobby()
    {
        print("Lobby joined!");
    }

    public string GetRoomName()
    {
        string roomName = inputRoomName.text;
        return roomName.Trim();
    }
    public void OnClickCreateRoom()
    {
        string roomName = GetRoomName();
        if (roomName.Length > 0)
        {
            PhotonNetwork.CreateRoom(roomName);
        }
        else
        {
            print("Invalid Room Name!");
        }
    }

    public override void OnJoinedRoom()
    {
        print("Room Joined!");
    }
}
