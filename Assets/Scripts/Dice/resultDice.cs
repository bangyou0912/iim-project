using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.UI;
public class resultDice : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum DiceSelectMode { Multi, SingleManual }
    public DiceSelectMode selectMode = DiceSelectMode.Multi;

    public static resultDice currentlySelectedManual = null;

    public Vector3 hoverScale = new Vector3(1.2f, 1.2f, 1.2f);
    public float scaleSpeed = 10f;

    private Vector3 originalScale;
    private Coroutine scaleCoroutine;

    public string cardColorName;
    public Image highlighted;
    public bool isCardSelected { get; private set; } = false;

    void Start()
    {
        originalScale = transform.localScale;

        Sprite sprite = GetComponent<Image>().sprite;
        if (sprite != null && string.IsNullOrEmpty(cardColorName))
            cardColorName = sprite.name.Split('_')[0];
    }
    public void SetHoverVisual(bool isOn)
    {
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);

        if (isOn)
            scaleCoroutine = StartCoroutine(ScaleTo(hoverScale));
        else
            scaleCoroutine = StartCoroutine(ScaleTo(originalScale));
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleTo(hoverScale));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleTo(originalScale));
    }

    IEnumerator ScaleTo(Vector3 targetScale)
    {
        while (Vector3.Distance(transform.localScale, targetScale) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
            yield return null;
        }
        transform.localScale = targetScale;
    }

    public void SetSelected(bool selected)
    {
        isCardSelected = selected;
        if (highlighted != null)
            highlighted.gameObject.SetActive(selected);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (selectMode == DiceSelectMode.Multi)
        {
            // 多選模式（原本的骰子）
            isCardSelected = !isCardSelected;
            SetSelected(isCardSelected);

            if (isCardSelected)
                GameSceneManager.Instance.SelectDiceColor(cardColorName);
            else
                GameSceneManager.Instance.DeselectDiceColor(cardColorName);
        }
        else if (selectMode == DiceSelectMode.SingleManual)
        {
            // 單選模式（自選顏色）
            if (currentlySelectedManual == this)
            {
                // 取消選取
                SetSelected(false);
                currentlySelectedManual = null;
                GameSceneManager.Instance.DeselectDiceColor(cardColorName);
                return;
            }

            // 取消其他自選的
            if (currentlySelectedManual != null)
            {
                currentlySelectedManual.SetSelected(false);
                GameSceneManager.Instance.DeselectDiceColor(currentlySelectedManual.cardColorName);
            }

            // 選擇這張
            SetSelected(true);
            currentlySelectedManual = this;
            GameSceneManager.Instance.SelectDiceColor(cardColorName);
        }
    }
}
