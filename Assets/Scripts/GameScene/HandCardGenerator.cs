using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HandCardGenerator : MonoBehaviour
{
    public GameObject handCardPrefab;          // 卡牌 prefab
    public RectTransform cardContainer;        // 卡牌容器
    public Texture2D[] primaryColors;          // 三原色圖
    public Texture2D[] secondaryColors;        // 二次色圖

    public void StartGeneratingCards()
    {
        StartCoroutine(GenerateCardsWithDelay(2f));
    }

    IEnumerator GenerateCardsWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCoroutine(GenerateCards());
    }

    IEnumerator GenerateCards()
    {
        List<Texture2D> selectedTextures = new List<Texture2D>();

        for (int i = 0; i < 6; i++)
        {
            int index = Random.Range(0, primaryColors.Length);
            selectedTextures.Add(primaryColors[index]);
        }

        for (int i = 0; i < 2; i++)
        {
            int index = Random.Range(0, secondaryColors.Length);
            selectedTextures.Add(secondaryColors[index]);
        }

        Shuffle(selectedTextures);

        int cardCount = selectedTextures.Count;
        float radius = 800f;
        float angleRange = 30f;

        for (int i = 0; i < cardCount; i++)
        {
            GameObject card = Instantiate(handCardPrefab, cardContainer);
            RawImage raw = card.GetComponent<RawImage>();
            raw.texture = selectedTextures[i];

            // 指派顏色與類型
            HandCardSelect sel = card.GetComponent<HandCardSelect>();
            if (sel != null)
            {
                sel.cardColorName = raw.texture.name;
                if (sel.highlighted == null)
                {
                    sel.highlighted = card.GetComponentInChildren<Image>(true);
                }
                sel.InitPosition();
            }

            // 扇形座標與旋轉
            float angle = Mathf.Lerp(-angleRange, angleRange, i / (cardCount - 1f));
            float radians = angle * Mathf.Deg2Rad;
            float x = Mathf.Sin(radians) * radius;
            float y = Mathf.Cos(radians) * radius - radius;

            RectTransform rt = card.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);
            rt.rotation = Quaternion.Euler(0, 0, -angle);

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

    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[rand];
            list[rand] = temp;
        }
    }
}
