using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class HandCard : MonoBehaviour, IPointerClickHandler
{
    public Texture2D cardTexture;
    private RawImage rawImage;

    private bool hasPlayed = false;

    void Start()
    {
        rawImage = GetComponent<RawImage>();
        rawImage.texture = cardTexture;
    }

    // 點擊事件：選牌／出牌
    public void OnPointerClick(PointerEventData eventData)
    {
        if (hasPlayed) return;

        hasPlayed = true;

        Debug.Log("卡牌已出：" + cardTexture.name);

        // 出牌動畫：移到中央 or 淡出
        StartCoroutine(PlayCardAnimation());
    }

    IEnumerator PlayCardAnimation()
    {
        Vector3 startPos = transform.localPosition;
        Vector3 endPos = Vector3.zero;

        float t = 0f;
        float duration = 0.5f;

        while (t < duration)
        {
            transform.localPosition = Vector3.Lerp(startPos, endPos, t / duration);
            t += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = endPos;

        // 之後可以加：發送 RPC 給其他玩家
        // photonView.RPC("OnCardPlayed", RpcTarget.Others, cardTexture.name);
    }
}
