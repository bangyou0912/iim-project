using UnityEngine;
using UnityEngine.SceneManagement;

public class RulePage2Manager: MonoBehaviour
{
    public void OnClickGoToFrontPage()
    {
            SceneManager.LoadScene("RulePage1");
    }

    public void OnClickGoToNextPage()
    {
        SceneManager.LoadScene("RulePage3");
    }
}

