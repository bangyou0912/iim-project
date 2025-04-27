using UnityEngine;
using UnityEngine.UI;

public class ShowOrHideImageOnClick : MonoBehaviour
{
    public Button toggleButton;   // 拖曳按鈕進來
    public GameObject imageObj;   // 拖曳要切換的Image進來

    void Start()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleImage);
        }

        if (imageObj != null)
        {
            imageObj.SetActive(false); // 一開始圖片隱藏
        }
    }

    void ToggleImage()
    {
        if (imageObj != null)
        {
            imageObj.SetActive(!imageObj.activeSelf); // 切換目前狀態
        }
    }
}
