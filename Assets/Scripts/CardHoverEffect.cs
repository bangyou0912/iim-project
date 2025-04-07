using UnityEngine;
using UnityEngine.EventSystems;

public class CardHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalPos;
    private Vector3 targetPos;
    private Vector3 originalScale;
    private Vector3 targetScale;
    private float originalAngle;
    private float targetAngle;

    public float floatY = 30f;
    public float scaleUp = 1.2f;
    public float speed = 10f;

    public float originalRotationZ = 0f;
    void Start()
    {
        originalPos = transform.localPosition;
        targetPos = originalPos;

        originalScale = transform.localScale;
        targetScale = originalScale;

        // 抓目前角度為初始值（避免一開始被轉回 0）
        originalAngle = transform.localEulerAngles.z;
        targetAngle = originalAngle;
    }

    void Update()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * speed);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);

        float currentZ = transform.localEulerAngles.z;
        float newZ = Mathf.LerpAngle(currentZ, targetAngle, Time.deltaTime * speed);
        transform.localEulerAngles = new Vector3(0, 0, newZ);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetPos = originalPos + new Vector3(0f, floatY, 0f);
        targetScale = originalScale * scaleUp;
        targetAngle = 0f;

        // 提升層級 → 避免被其他卡片擋住
        transform.SetAsLastSibling();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetPos = originalPos;
        targetScale = originalScale;
        targetAngle = originalAngle;
    }
}
