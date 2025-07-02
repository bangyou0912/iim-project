using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.UI;


public class resultDice : MonoBehaviour,  IPointerClickHandler,IPointerEnterHandler, IPointerExitHandler
{
    public Vector3 hoverScale = new Vector3(1.2f, 1.2f, 1.2f); // ©ñ¤j¤ñ¨Ò
    public float scaleSpeed = 10f; 

    private Vector3 originalScale;
    private Coroutine scaleCoroutine;

    public string cardColorName;
    public Image highlighted;

    private bool isHovering = false;
    public bool isCardSelected { get; private set; } = false;

    void Start()
    {
        originalScale = transform.localScale;
        Sprite sprite = GetComponent<Image>().sprite;
        if (sprite != null)
           cardColorName = sprite.name.Split('_')[0];
        
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

    public void OnPointerClick(PointerEventData eventData)
    {
        isCardSelected = !isCardSelected;
        if (highlighted != null)
            highlighted.gameObject.SetActive(isCardSelected);

        if (isCardSelected)
        {
            GameSceneManager.Instance.SelectDiceColor(cardColorName);
            print(cardColorName);
        }
            
        else
            GameSceneManager.Instance.DeselectDiceColor(cardColorName);
       

    }
}
