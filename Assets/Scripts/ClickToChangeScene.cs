using UnityEngine;
using UnityEngine.SceneManagement;

public class ClickToChangeScene : MonoBehaviour
{
    public string sceneName;  // 在 Inspector 填入要切換的場景名

    void OnMouseDown()
    {
        if (sceneName == "GameScene")
        {
            PlayerPrefs.SetInt("ShouldGenerateCards", 1);  // 要生成卡
        }
        else
        {
            PlayerPrefs.SetInt("ShouldGenerateCards", 0);  // 不生成
        }

        SceneManager.LoadScene(sceneName);
    }
}
