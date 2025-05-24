using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class  publicCardGenerate : MonoBehaviour
{
    public GameObject publicCardPrefab;          // 卡牌 prefab
    public RectTransform cardContainer;        // 卡牌容器
    public Texture2D[] cards;
    public Image yellow_retangular;
    void Start()
    {
        StartCoroutine(GenerateCardsWithDelay(2f));
    }

    IEnumerator GenerateCardsWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCoroutine(ck());
    }

    IEnumerator ck()
    {

        for (int i = 0; i < 4; i++)
        {

            int result = Random.Range(0, 11);
            GameObject card = Instantiate(publicCardPrefab, cardContainer);
            RawImage raw = card.GetComponent<RawImage>();
            raw.texture = cards[result];
            card.transform.localPosition = new Vector3(-378f + i * 248f, 141f, 0f);
            CanvasGroup cg = card.AddComponent<CanvasGroup>();
            cg.alpha = 0;

            StartCoroutine(FadeInCard(cg)); 

            yield return new WaitForSeconds(0.15f);

        }

    }

    IEnumerator FadeInCard(CanvasGroup cg)
    {
        float duration = 0.3f;
        float t = 0f;

        while (t < duration)
        {
            cg.alpha = Mathf.Lerp(0, 1, t / duration);
            t += Time.deltaTime;
            yield return null;
        }

        cg.alpha = 1;
    }

}
