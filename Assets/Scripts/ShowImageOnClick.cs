/*using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShowOrHideImageOnClick : MonoBehaviour
{
    public Button toggleButton;   // 拖曳按鈕進來
    public GameObject imageObj;   // 拖曳要切換的Image進來
    public GameObject darkBackground;//黑色背景

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
        //取消按鈕的選取狀態，避免Enter觸發
        EventSystem.current.SetSelectedGameObject(null);
    }
        void ToggleImage()
     {
         if (imageObj != null)
         {

            imageObj.SetActive(!imageObj.activeSelf);// 切換目前狀態
            darkBackground.SetActive(imageObj.activeSelf);
            imageObj.transform.SetAsLastSibling();
            toggleButton.transform.SetAsLastSibling();
         }
     }
}*/
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShowOrHideImageOnClick : MonoBehaviour
{
    public Button toggleButton;       // 拖曳按鈕
    public GameObject imageObj;       // 提示面板
    public GameObject darkBackground; // 黑色背景

    private bool isHintOpen = false;

    void Start()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(ToggleImage);

        if (imageObj != null)
            imageObj.SetActive(false);

        EventSystem.current.SetSelectedGameObject(null);
    }

    void ToggleImage()
    {
        if (imageObj == null) return;

        bool nowActive = !imageObj.activeSelf;
        imageObj.SetActive(nowActive);
        darkBackground.SetActive(nowActive);

        imageObj.transform.SetAsLastSibling();
        toggleButton.transform.SetAsLastSibling();
//只記錄打開提示
        if (nowActive && !isHintOpen)
        {
            isHintOpen = true;
            GameSceneManager.Instance?.OnHintButtonClicked();
        }
        else if (!nowActive)
        {
            isHintOpen = false;
        }
    }
}
