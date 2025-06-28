using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Photon.Realtime;
using System.Collections.Generic;
using System.Text;

public class LobbySceneManager : MonoBehaviourPunCallbacks
{
    [SerializeField]
    TMP_InputField inputRoomName;
    [SerializeField]
    TMP_Text textRoomList;
    [SerializeField]
    TMP_InputField inputPlayerName;
    [SerializeField]
    GameObject existRoomPrefab;
    [SerializeField]
    RectTransform existRoomContainer;

    public void Start()
    {
        if (PhotonNetwork.IsConnected == false)
        {
            SceneManager.LoadScene("StartScene");
        }
        else
        {
            if(PhotonNetwork.CurrentLobby == null)
            {
                PhotonNetwork.JoinLobby();
            }
        }
    }

    public override void OnConnectedToMaster()
    {
        print("OnConnectToMaster");
        PhotonNetwork.JoinLobby();
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

    public string GetPlayerName()
    {
        string playerName = inputPlayerName.text;
        return playerName.Trim();
    }

    public void OnClickBackToHomepage()
    {
        print("Back to Home Page!");
        SceneManager.LoadScene("StartScene");
    }

    public void OnClickCreateRoom()
    {
        string roomName = GetRoomName();
        string playerName = GetPlayerName();
        if (roomName.Length > 0 && playerName.Length > 0)
        {
            PhotonNetwork.CreateRoom(roomName);
            PhotonNetwork.LocalPlayer.NickName = playerName;
        }
        else
        {
            print("Invalid Room Name or Player Name !");
        }
    }

    public void OnClickJoinedRoom()
    {
        string roomName = GetRoomName();
        string playerName = GetPlayerName();
        if (roomName.Length > 0 && playerName.Length > 0)
        {
            PhotonNetwork.JoinRoom(roomName);
            PhotonNetwork.LocalPlayer.NickName = playerName;
        }
        else
        {
            print("Invalid Room Name or Player Name !");
        }
    }
    public void OnClickExistRoom(GameObject existRoomPrefab)
    {
        inputRoomName.text = existRoomPrefab.transform.Find("RoomNameText").GetComponent<TMP_Text>().text;
        print("已輸入房間名稱");
    }
    public override void OnJoinedRoom()
    {

        print("Room Joined!");
        SceneManager.LoadScene("RoomScene");
    }

    public override void OnRoomListUpdate( List<RoomInfo> roomList)
    {
        print("Update!");
        StringBuilder sb = new StringBuilder();
        foreach(RoomInfo roomInfo in roomList)
        {
            if(roomInfo.PlayerCount >= 0)
            {
                sb.AppendLine(" → " + roomInfo.Name + "   人數： " + roomInfo.PlayerCount);
                GameObject room = Instantiate(existRoomPrefab, existRoomContainer);
                TMP_Text roomText = room.transform.Find("RoomNameText").GetComponent<TMP_Text>();
                roomText.text = roomInfo.Name;
                Button button = room.GetComponent<Button>();
                button.onClick.AddListener(() => OnClickExistRoom(room));
               // Debug.Log("已綁定房間：" + roomText.text);
            }
        }
        textRoomList.text = sb.ToString();
    }
}
