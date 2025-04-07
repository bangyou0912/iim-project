using UnityEngine;
using UnityEngine.SceneManagement;

public class ClickToChangeScene : MonoBehaviour
{
    public string sceneName;  // 在 Inspector 填入要切換的場景名

    void OnMouseDown()
    {
        SceneManager.LoadScene(sceneName);
    }
}
