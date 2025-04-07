using UnityEngine;
using UnityEngine.EventSystems;

public class CardHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalPos;
    private Vector3 targetPos;
    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool hovering;

    public float floatY = 30f;
    public float scaleUp = 1.2f;
    public float speed = 10f;

    void Start()
    {
        originalPos = transform.localPosition;
        originalScale = transform.localScale;
        targetPos = originalPos;
        targetScale = originalScale;
    }

    void Update()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * speed);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetPos = originalPos + new Vector3(0f, floatY, 0f);
        targetScale = originalScale * scaleUp;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetPos = originalPos;
        targetScale = originalScale;
    }
}
