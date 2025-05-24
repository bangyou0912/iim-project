using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HandCardGenerator : MonoBehaviour
{
    public GameObject handCardPrefab;          // 卡牌 prefab
    public RectTransform cardContainer;        // 卡牌容器
    public Texture2D[] primaryColors;          // 三原色圖
    public Texture2D[] secondaryColors;        // 二次色圖
void Start()
    {
        if (PhotonNetwork.IsConnected == false)
        {
            SceneManager.LoadScene("StartScene");
        }
        else
        {
            StartCoroutine(GenerateCardsWithDelay(2f));
        }
    }

    IEnumerator GenerateCardsWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCoroutine(GenerateCards());
    }

    IEnumerator GenerateCards()
    {
        List<Texture2D> selectedTextures = new List<Texture2D>();

        // 隨機選 6 張三原色
        for (int i = 0; i < 6; i++)
        {
            int index = Random.Range(0, primaryColors.Length);
            selectedTextures.Add(primaryColors[index]);
        }

        // 隨機選 2 張二次色
        for (int i = 0; i < 2; i++)
        {
            int index = Random.Range(0, secondaryColors.Length);
            selectedTextures.Add(secondaryColors[index]);
        }

        // 打亂順序
        Shuffle(selectedTextures);

        // 扇形參數
        int cardCount = selectedTextures.Count;
        float radius = 800f;
        float angleRange = 30f;

        for (int i = 0; i < cardCount; i++)
        {
            GameObject card = Instantiate(handCardPrefab, cardContainer);
            RawImage raw = card.GetComponent<RawImage>();
            raw.texture = selectedTextures[i];

            // 傳圖進 HandCard 腳本
            HandCard hand = card.GetComponent<HandCard>();
            if (hand != null)
            {
                hand.cardTexture = selectedTextures[i];
            }

            // 扇形角度與位置
            float angle = Mathf.Lerp(-angleRange, angleRange, i / (cardCount - 1f));
            float radians = angle * Mathf.Deg2Rad;

            float x = Mathf.Sin(radians) * radius;
            float y = Mathf.Cos(radians) * radius - radius;  // 中間高，兩側低

            RectTransform rt = card.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);
            rt.rotation = Quaternion.Euler(0, 0, -angle);

            CanvasGroup cg = card.AddComponent<CanvasGroup>();
            cg.alpha = 0;

            StartCoroutine(FadeInCard(cg));  // 淡入動畫

            yield return new WaitForSeconds(0.15f);  // 每張間隔 0.15 秒（可自行調整）

            // 將原始角度儲存給 Hover 用
            CardHoverEffect hover = card.GetComponent<CardHoverEffect>();
            if (hover != null)
            {
                hover.originalRotationZ = angle;
            }
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
