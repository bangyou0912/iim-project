using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    // 要切換到的場景名稱（記得填入 Inspector）
    public string sceneToLoad;

    // 給 UI 按鈕使用的方法
    public void LoadNextScene()
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}