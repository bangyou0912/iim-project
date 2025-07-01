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
    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>(); //儲存房間
    public void Start()
    {
        if (PhotonNetwork.IsConnected == false)
        {
            SceneManager.LoadScene("StartScene");
        }
        else
        {
            if (PhotonNetwork.CurrentLobby == null)
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

            PhotonNetwork.LocalPlayer.NickName = playerName;

            RoomOptions options = new RoomOptions();
            options.MaxPlayers = 4;
            options.EmptyRoomTtl = 20000;   //房間沒人時等20秒才刪除

            PhotonNetwork.CreateRoom(roomName, options);
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
            PhotonNetwork.LocalPlayer.NickName = playerName;
            PhotonNetwork.JoinRoom(roomName);
            print("加入成功");
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

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"加入房間失敗：{message} (code {returnCode})");
    }
    public override void OnJoinedRoom()
    {
        print("已加入房間：" + PhotonNetwork.CurrentRoom.Name);
        SceneManager.LoadScene("RoomScene");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            if (info.RemovedFromList)
            {
                cachedRoomList.Remove(info.Name);
            }
            else
            {
                cachedRoomList[info.Name] = info;
            }
        }

        foreach (Transform child in existRoomContainer)
        {
            Destroy(child.gameObject);
        }

        StringBuilder sb = new StringBuilder();

        foreach (RoomInfo info in cachedRoomList.Values)
        {
            sb.AppendLine(" → " + info.Name + "   人數： " + info.PlayerCount);

            GameObject room = Instantiate(existRoomPrefab, existRoomContainer);
            TMP_Text roomText = room.transform.Find("RoomNameText").GetComponent<TMP_Text>();
            roomText.text = info.Name;

            string currentRoomName = info.Name;
            room.GetComponent<Button>().onClick.AddListener(() =>
            {
                inputRoomName.text = currentRoomName;
                print("填入房間名稱：" + currentRoomName);
            });
        }

        textRoomList.text = sb.ToString();
    }
}
