using UnityEngine;
using System.Collections;

public class DicePopup : MonoBehaviour
{
    public GameObject popupPanel;

    public void OnDiceClick()
    {
        popupPanel.SetActive(true);              // 顯示視窗
        StartCoroutine(AutoClose());             // 開始倒數關閉
    }

    IEnumerator AutoClose()
    {
        yield return new WaitForSeconds(3f);     // 等待 3 秒
        popupPanel.SetActive(false);             // 關閉視窗
    }

    // 可選：如果你有一個手動關閉的按鈕可以呼叫這個
    public void ClosePopup()
    {
        popupPanel.SetActive(false);
    }
}
