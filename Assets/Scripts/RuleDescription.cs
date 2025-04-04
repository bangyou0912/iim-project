using UnityEngine;
using UnityEngine.SceneManagement;

public class RuleDescripton : MonoBehaviour
{
    public string previousScene;
    public string nextScene;

    public void GoToPreviousScene()
    {
        if (!string.IsNullOrEmpty(previousScene))
        {
            SceneManager.LoadScene(previousScene);
        }
    }

    public void GoToNextScene()
    {
        if (!string.IsNullOrEmpty(nextScene))
        {
            SceneManager.LoadScene(nextScene);
        }
    }
}

