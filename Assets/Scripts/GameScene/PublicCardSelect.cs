using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PublicCardSelect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public string cardColorName;
    public Image highlighted;
    private bool isSelected = false;
    public bool isHovering = false;

    private Vector3 originalPos;
    private Vector3 targetPos;
    private Vector3 originalScale;
    private Vector3 targetScale;
    private float originalAngle;
    private float targetAngle;

    private RawImage rawImage;

    public float floatY = 30f;
    public float scaleUp = 1.1f;
    public float speed = 10f;

    public void Init(Texture2D tex)
    {
        rawImage = GetComponent<RawImage>();
        if (rawImage != null)
        {
            rawImage.texture = tex;
            cardColorName = tex.name;
        }

        originalPos = transform.localPosition;
        targetPos = originalPos;

        originalScale = transform.localScale;
        targetScale = originalScale;

        originalAngle = transform.localEulerAngles.z;
        targetAngle = originalAngle;

        SetSelected(false);
    }

    void Update()
    {
        if (isSelected) return;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * speed);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);

        float currentZ = transform.localEulerAngles.z;
        float newZ = Mathf.LerpAngle(currentZ, targetAngle, Time.deltaTime * speed);
        transform.localEulerAngles = new Vector3(0, 0, newZ);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        targetPos = originalPos + new Vector3(0, floatY, 0);
        targetScale = originalScale * scaleUp;
        targetAngle = 0f;

        if (highlighted != null)
            highlighted.gameObject.SetActive(true);

        transform.SetAsLastSibling();

        if (GameSceneManager.Instance != null &&
        GameSceneManager.Instance.enableRealtimeTips &&
        GameSceneManager.Instance.hoverTipText != null)
        {
            string tip = GameSceneManager.Instance.GetMixingTip(cardColorName);
            GameSceneManager.Instance.hoverTipText.text = tip;
            GameSceneManager.Instance.TipPanel.gameObject.SetActive(true);
            GameSceneManager.Instance.hoverTipText.gameObject.SetActive(true);
            GameSceneManager.Instance.HighlightMatchHandCards(cardColorName);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        targetPos = originalPos;
        targetScale = originalScale;
        targetAngle = originalAngle;

        if (highlighted != null)
            highlighted.gameObject.SetActive(false);

        if (GameSceneManager.Instance != null && GameSceneManager.Instance.hoverTipText != null)
        {
            GameSceneManager.Instance.TipPanel.gameObject.SetActive(false);
            GameSceneManager.Instance.hoverTipText.gameObject.SetActive(false);
            GameSceneManager.Instance.ClearAllHandCardHover();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        GameSceneManager.Instance.OnPublicCardClicked(this);
    }
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (highlighted != null)
            highlighted.gameObject.SetActive(selected);

        if (selected)
        {
            targetPos = originalPos + new Vector3(0, floatY, 0);
            targetScale = originalScale * scaleUp;
            targetAngle = 0f;
        }
        else
        {
            targetPos = originalPos;
            targetScale = originalScale;
            targetAngle = originalAngle;
        }
    }

    public void ChangeCardTo(Texture2D newTex)
    {
        if (rawImage != null)
        {
            rawImage.texture = newTex;
            cardColorName = newTex.name;
        }
        Debug.Log($"[PublicCardSelect] 公牌換成：{newTex.name}");
    }

    public void SetCard(Texture2D tex)
    {
        ChangeCardTo(tex);
        SetSelected(false);  // 確保同步後不會殘留選取狀態
    }
}
