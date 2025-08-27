using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HandCardSelect : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public string cardColorName;
    public Image highlighted;

    public float floatY = 30f;
    public float scaleUp = 1.2f;
    public float speed = 10f;

    private Vector3 originalPos;
    private Vector3 targetPos;
    private Vector3 originalScale;
    private Vector3 targetScale;
    private float originalAngle;
    private float targetAngle;

    private bool isHovering = false;
    public bool isCardSelected { get; private set; } = false;

    private HandCardMode currentMode = HandCardMode.Normal;
    private System.Action<HandCardSelect> onDiscardSelectedCallback;
    void Start()
    {
        InitPosition();

    }

    public enum HandCardMode //棄牌模式or一般
    {
        Normal,
        DiscardSelection
    }
    public void SetMode(HandCardMode mode, System.Action<HandCardSelect> discardCallback = null)
    {
        currentMode = mode;
        onDiscardSelectedCallback = discardCallback;
        isCardSelected = false;
        ApplySelectionVisual();
    }
/// <summary>
/// 
/// </summary>
    public void InitPosition()
    {
        RectTransform rt = GetComponent<RectTransform>();
        originalPos = rt.anchoredPosition;
        targetPos = originalPos;

        originalScale = transform.localScale;
        targetScale = originalScale;

        originalAngle = transform.localEulerAngles.z;
        targetAngle = originalAngle;

        if (highlighted != null)
            highlighted.gameObject.SetActive(false);
    }

    void Update()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * speed);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);
        float currentZ = transform.localEulerAngles.z;
        float newZ = Mathf.LerpAngle(currentZ, targetAngle, Time.deltaTime * speed);
        transform.localEulerAngles = new Vector3(0, 0, newZ);
    }

    public void OnPointerClick(PointerEventData eventData)
    {

        switch (currentMode)
        {
            case HandCardMode.Normal:
                isCardSelected = !isCardSelected;
                ApplySelectionVisual();
                GameSceneManager.Instance.OnHandCardSelected(this);
                break;

            case HandCardMode.DiscardSelection:
                var allCards = Object.FindObjectsByType<HandCardSelect>(FindObjectsSortMode.None);
                foreach (var card in allCards)
                {
                    if (card != this)
                    {
                        card.isCardSelected = false;
                        card.ApplySelectionVisual();
                    }
                }

                // 只選中自己
                isCardSelected = true;
                ApplySelectionVisual();

                // 通知 callback
                onDiscardSelectedCallback?.Invoke(this);
                break;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        if (!isCardSelected)
        {
            targetPos = originalPos + new Vector3(0, floatY, 0);
            targetScale = originalScale * scaleUp;
            targetAngle = 0f;
        }
        transform.SetAsLastSibling();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        if (!isCardSelected)
        {
            targetPos = originalPos;
            targetScale = originalScale;
            targetAngle = originalAngle;
        }
    }

    private void ApplySelectionVisual()
    {
        if (highlighted != null)
            highlighted.gameObject.SetActive(isCardSelected);

        if (isCardSelected)
        {
            targetPos = originalPos + new Vector3(0, floatY, 0);
            targetScale = originalScale * scaleUp;
            targetAngle = 0f;
        }
        else if (!isHovering)
        {
            targetPos = originalPos;
            targetScale = originalScale;
            targetAngle = originalAngle;
        }
    }

    public void SetHighlight(bool show)         //白色卡選卡時的效果
    {
        if (highlighted != null)
            highlighted.gameObject.SetActive(show);
    }

    public void SetHoverVisual(bool isOn)
    {
        if (isOn)
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


}
