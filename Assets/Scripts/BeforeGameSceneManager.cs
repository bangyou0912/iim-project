using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class BeforeGameSceneManager : MonoBehaviourPunCallbacks
{
    public void Start()
    {
        if (PhotonNetwork.IsConnected == false)
        {
            SceneManager.LoadScene("StartScene");
        }
    }
    public void OnClickStart()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("GameScene");
        }
    }
}
