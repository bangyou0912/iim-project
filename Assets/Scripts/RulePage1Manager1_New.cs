using UnityEngine;
using UnityEngine.SceneManagement;

public class RulePage1Manager1_New : MonoBehaviour //因為舊的打不開所以開新的不然進不去遊戲
{
    public void OnClickGoToFrontPage()
    {
        SceneManager.LoadScene("RoomScene");
    }

    public void OnClickGoToNextPage()
    {
        SceneManager.LoadScene("RulePage2");
    }
    public void OnClickStartGame()
    {
        SceneManager.LoadScene("BeforeGameScene");
    }
}

