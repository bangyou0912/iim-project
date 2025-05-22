using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class StartSceneManager : MonoBehaviourPunCallbacks
{
    public void OnClickStart()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            print("ClickStart! Connecting to Photon...");
            SceneManager.LoadScene("LobbyScene");
        }
        else
        {
            print("Already connected to Photon.");
            SceneManager.LoadScene("LobbyScene");
        }
    }
    public override void OnConnectedToMaster()
    {
        print("Connected!");
        SceneManager.LoadScene("LobbyScene");
    }

}
