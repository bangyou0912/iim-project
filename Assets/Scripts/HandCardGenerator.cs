using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HandCardGenerator : MonoBehaviour
{
    public GameObject handCardPrefab;          // 卡牌預製物件
    public Transform cardContainer;            // 卡片顯示在哪個物件下
    public Texture2D[] primaryColors;          // 三原色（母體 3 張）
    public Texture2D[] secondaryColors;        // 二次色（母體 3 張）

    void Start()
    {
        GenerateCards();
    }

    void GenerateCards()
    {
        List<Texture2D> selectedTextures = new List<Texture2D>();

        // 三原色中隨機選 6 張（可重複）
        for (int i = 0; i < 6; i++)
        {
            int index = Random.Range(0, primaryColors.Length);
            selectedTextures.Add(primaryColors[index]);
        }

        // 二次色中隨機選 2 張（可重複）
        for (int i = 0; i < 2; i++)
        {
            int index = Random.Range(0, secondaryColors.Length);
            selectedTextures.Add(secondaryColors[index]);
        }

        // 打亂順序
        Shuffle(selectedTextures);

        // 產生 8 張卡牌
        foreach (Texture2D tex in selectedTextures)
        {
            GameObject card = Instantiate(handCardPrefab, cardContainer);
            card.GetComponent<RawImage>().texture = tex;
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
