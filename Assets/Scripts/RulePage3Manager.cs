using UnityEngine;
using UnityEngine.SceneManagement;

public class RulePage3Manager: MonoBehaviour
{
    public void OnClickGoToFrontPage()
    {
            SceneManager.LoadScene("RulePage2");
    }
    public void OnClickStartGame()
    {
        SceneManager.LoadScene("BeforeGameScene");
    }
}

