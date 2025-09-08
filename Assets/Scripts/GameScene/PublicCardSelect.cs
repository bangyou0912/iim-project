using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PublicCardSelect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public string cardColorName;
    public Image highlighted;
    private bool isSelected = false;
    public bool isHovering = false;

    //新增：這張公牌在場上的索引（0,1,2,3…）
    public int cardIndex = -1;

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

    // 抽成方法：套用 hover 視覺（可被 RPC 呼叫）
    public void ApplyHoverVisual(bool hover)
    {
        isHovering = hover;

        if (hover)
        {
            targetPos = originalPos + new Vector3(0, floatY, 0);
            targetScale = originalScale * scaleUp;
            targetAngle = 0f;
            if (highlighted != null) highlighted.gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }
        else
        {
            targetPos = originalPos;
            targetScale = originalScale;
            targetAngle = originalAngle;
            if (highlighted != null) highlighted.gameObject.SetActive(isSelected); // 若已被選中，維持黃框
        }
    }

    //抽成方法：套用選取視覺（可被 RPC 呼叫）
    public void ApplySelectVisual(bool selected)
    {
        isSelected = selected;

        if (selected)
        {
            targetPos = originalPos + new Vector3(0, floatY, 0);
            targetScale = originalScale * scaleUp;
            targetAngle = 0f;
            if (highlighted != null) highlighted.gameObject.SetActive(true);
        }
        else
        {
            targetPos = originalPos;
            targetScale = originalScale;
            targetAngle = originalAngle;
            if (highlighted != null) highlighted.gameObject.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 本地立即套用（手感好）
        ApplyHoverVisual(true);

        //廣播給所有人（包含自己），同步 hover
        if (cardIndex >= 0 && GameSceneManager.Instance != null)
            GameSceneManager.Instance.photonView.RPC("RPC_PublicCardHover", Photon.Pun.RpcTarget.All, cardIndex, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ApplyHoverVisual(false);
        if (cardIndex >= 0 && GameSceneManager.Instance != null)
            GameSceneManager.Instance.photonView.RPC("RPC_PublicCardHover", Photon.Pun.RpcTarget.All, cardIndex, false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 先同步選取視覺（會自動把其他公牌的選取取消）
        if (cardIndex >= 0 && GameSceneManager.Instance != null)
            GameSceneManager.Instance.photonView.RPC("RPC_PublicCardSelect", Photon.Pun.RpcTarget.All, cardIndex);

        // 再執行本地邏輯（只有點的人需要跳出確認面板等）
        GameSceneManager.Instance.OnPublicCardClicked(this);
    }

    public void SetSelected(bool selected) => ApplySelectVisual(selected);

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
