using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ShowOrHideImageOnClick : MonoBehaviour
{
    [Header("UI 參考")]
    [SerializeField] private Button toggleButton;       // 觸發顯示/隱藏的按鈕
    [SerializeField] private GameObject imageObj;       // 要顯示/隱藏、並記錄開啟時長的物件
    [SerializeField] private GameObject darkBackground; // 暗色背景

    // 內部狀態
    private bool isHintOpen = false;

    private void Start()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(ToggleImage);

        if (imageObj != null)
            imageObj.SetActive(false);

        if (darkBackground != null)
            darkBackground.SetActive(false);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    /// <summary>
    /// 由按鈕點擊觸發：切換 imageObj 顯示/隱藏
    /// </summary>
    private void ToggleImage()
    {
        if (imageObj == null) return;
        SetHintVisible(!imageObj.activeSelf);
    }

    /// <summary>
    /// 封裝顯示/隱藏行為（含時間紀錄通知）
    /// </summary>
    private void SetHintVisible(bool visible)
    {
        if (imageObj == null) return;
        if (imageObj.activeSelf == visible) return; // 狀態相同就不重做

        imageObj.SetActive(visible);
        if (darkBackground != null) darkBackground.SetActive(visible);

        if (visible)
        {
            // 置頂以確保顯示在最上層
            imageObj.transform.SetAsLastSibling();
            if (toggleButton != null) toggleButton.transform.SetAsLastSibling();

            if (!isHintOpen)
            {
                isHintOpen = true;
                GameSceneManager.Instance?.NotifyHintImageOpened(); //通知開始計時
            }
        }
        else
        {
            if (isHintOpen)
            {
                isHintOpen = false;
                GameSceneManager.Instance?.NotifyHintImageClosed(); //通知結束計時
            }
        }
    }

    private void Update()
    {
        // 在輸入框打字時不處理 Esc（避免誤關）
        if (Input.GetKeyDown(KeyCode.Escape) && !IsTypingInField())
        {
            if (imageObj != null && imageObj.activeSelf)
            {
                SetHintVisible(false); // Esc 只關閉，不開啟
            }
        }

        // 保險：若外部程式把 imageObj 關掉，也補記關閉事件，避免漏算尾巴
        if (imageObj != null && !imageObj.activeSelf && isHintOpen)
        {
            isHintOpen = false;
            GameSceneManager.Instance?.NotifyHintImageClosed();
        }
    }

    // 避免在輸入欄位聚焦時誤觸 Esc
    private bool IsTypingInField()
    {
        if (EventSystem.current == null) return false;
        var go = EventSystem.current.currentSelectedGameObject;
        if (go == null) return false;

        // 支援 Unity 的 InputField 與 TextMeshPro 的 TMP_InputField
        return go.GetComponent<InputField>() != null || go.GetComponent<TMP_InputField>() != null;
    }

    private void OnDisable()
    {
        // 腳本被停用時，若還開著就補關閉與通知（避免漏記）
        if (imageObj != null && imageObj.activeSelf)
        {
            SetHintVisible(false);
        }
    }

    // 若你需要在其他腳本直接開/關，可公開這兩個方法：
    public void OpenHint() => SetHintVisible(true);
    public void CloseHint() => SetHintVisible(false);
}
