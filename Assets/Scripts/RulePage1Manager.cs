using UnityEngine;
using UnityEngine.SceneManagement;

public class RuleDescripton : MonoBehaviour
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
        SceneManager.LoadScene("GameScene");
    }
}

