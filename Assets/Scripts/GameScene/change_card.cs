using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class change_card : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Texture2D[] cards;
    public RawImage rawImages;
    public Image yellow_retangular;

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
    public bool IsRec = false;
    public float originalRotationZ = 0f;

    void Start()
    {
        yellow_retangular.gameObject.SetActive(false);
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
        if (IsRec && IsLarged && Input.GetKeyDown(KeyCode.Return))
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
        IsRec = false;
        yellowrec(IsRec: IsRec);

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetPos = originalPos + new Vector3(0f, floatY, 0f);
        targetScale = originalScale * scaleUp;
        targetAngle = 0f;
        IsLarged = true;
        IsRec = true;

        // 將黃框移動到這張卡牌下
        yellow_retangular.gameObject.SetActive(true);
        yellow_retangular.transform.SetParent(transform, false); // false = 保留 local transform
        yellow_retangular.transform.localPosition = Vector3.zero; // 放在卡牌正中央
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetPos = originalPos;
        targetScale = originalScale;
        targetAngle = originalAngle;
        IsLarged = false;
        IsRec = false;
        yellow_retangular.gameObject.SetActive(false);
    }

    public void yellowrec(bool IsRec)
    {
        yellow_retangular.gameObject.SetActive(IsRec);
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



