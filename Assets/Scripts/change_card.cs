using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class change_card : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Texture2D[] cards;
    public RawImage rawImages;

    private Vector3 originalPos;
    private Vector3 targetPos;
    private Vector3 originalScale;
    private Vector3 targetScale;
    private float originalAngle;
    private float targetAngle;

    public float floatY = 0f;
    public float scaleUp = 1.2f;
    public float speed = 10f;
    public bool IsLarged = false;
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
        if (IsLarged && Input.GetKeyDown(KeyCode.Return))
        {
            chouca();
        }
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * speed);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);

        float currentZ = transform.localEulerAngles.z;
        float newZ = Mathf.LerpAngle(currentZ, targetAngle, Time.deltaTime * speed);
        transform.localEulerAngles = new Vector3(0, 0, newZ);

    }

    public void chouca()
    {
        rawImages = rawImages.GetComponent<RawImage>();
        int result = Random.Range(0, 11);
        rawImages.texture = cards[result];
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetPos = originalPos + new Vector3(0f, floatY, 0f);
        targetScale = originalScale * scaleUp;
        targetAngle = 0f;
        IsLarged = true;

        // 提升層級 → 避免被其他卡片擋住
        transform.SetAsLastSibling();
        
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetPos = originalPos;
        targetScale = originalScale;
        targetAngle = originalAngle;
        IsLarged = false;
    }
   
    
    /*public void big()
    {
        if(isEnlarged == false)
        {
            transform.localScale += new Vector3(scale, scale, 0f);
            isEnlarged = true;
        }
        else
        {
            small();
        }
    }
    public void small()
    {
        transform.localScale -= new Vector3(scale, scale, 0f);
        isEnlarged = false;
    } */
    

}



