using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class StartSceneManager : MonoBehaviourPunCallbacks
{
    private bool startClicked = false;

    void Start()
    {
        // 進入 StartScene 時，清除舊的 CustomProperties（如 finalGem）
        if (PhotonNetwork.LocalPlayer != null)
        {
            ExitGames.Client.Photon.Hashtable cleanProps = new ExitGames.Client.Photon.Hashtable();
            cleanProps["finalGem"] = null;
            PhotonNetwork.LocalPlayer.SetCustomProperties(cleanProps);
        }

        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public void OnClickStart()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            print("ClickStart! Connecting to Photon...");
            startClicked = true;
        }
        else if (PhotonNetwork.IsConnectedAndReady)
        {
            print("Already connected to Photon.");
            SceneManager.LoadScene("LobbyScene");
        }
    }

    public override void OnConnectedToMaster()
    {
        print("Connected to Master!");

        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
        else if (startClicked)
        {
            SceneManager.LoadScene("LobbyScene");
        }
    }

    public override void OnJoinedLobby()
    {
        print("Joined Lobby!");
        if (startClicked)
        {
            SceneManager.LoadScene("LobbyScene");
        }
    }
}
