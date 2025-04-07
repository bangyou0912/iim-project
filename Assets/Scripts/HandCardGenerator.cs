using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HandCardGenerator : MonoBehaviour
{
    public GameObject handCardPrefab;          // 卡牌 prefab
    public RectTransform cardContainer;        // 卡牌容器
    public Texture2D[] primaryColors;          // 三原色圖
    public Texture2D[] secondaryColors;        // 二次色圖

    void Start()
    {
        GenerateCards();
    }

    void GenerateCards()
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

            // 扇形角度與位置
            float angle = Mathf.Lerp(-angleRange, angleRange, i / (cardCount - 1f));
            float radians = angle * Mathf.Deg2Rad;

            float x = Mathf.Sin(radians) * radius;
            float y = Mathf.Cos(radians) * radius - radius;  // 中間高，兩側低

            RectTransform rt = card.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);
            rt.rotation = Quaternion.Euler(0, 0, -angle);    

            // 將原始角度儲存給 Hover 用
            CardHoverEffect hover = card.GetComponent<CardHoverEffect>();
            if (hover != null)
            {
                hover.originalRotationZ = angle;
            }
        }
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
