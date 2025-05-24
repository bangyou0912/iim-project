using UnityEngine;
using UnityEngine.SceneManagement;

public class RulePage1Manager: MonoBehaviour
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

