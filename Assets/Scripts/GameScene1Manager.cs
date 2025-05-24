using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameScene1Manager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
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
                InitGame();
            }
        }
    }

    public void InitGame()
    {
        float x = 7.15f, y = -3.13f;
        PhotonNetwork.Instantiate("Dice", new Vector3(x, y,0), Quaternion.identity);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
