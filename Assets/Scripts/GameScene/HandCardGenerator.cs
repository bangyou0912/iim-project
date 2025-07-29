using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class HandCardGenerator : MonoBehaviour
{
    public GameObject handCardPrefab;          // 卡牌 prefab
    public RectTransform cardContainer;        // 卡牌容器
    public Texture2D[] primaryColors;          // 三原色圖
    public Texture2D[] secondaryColors;        // 二次色圖

    private List<Texture2D> primaryDeck = new List<Texture2D>();
    private List<Texture2D> secondaryDeck = new List<Texture2D>();

    public void StartGeneratingCards()
    {
        PrepareFixedDecks();
        StartCoroutine(GenerateCardsWithDelay(2f));
    }

    private void PrepareFixedDecks()
    {
        primaryDeck.Clear();
        secondaryDeck.Clear();

        foreach (var tex in primaryColors)
        {
            for (int i = 0; i < 8; i++)
                primaryDeck.Add(tex);
        }

        foreach (var tex in secondaryColors)
        {
            for (int i = 0; i < 3; i++)
                secondaryDeck.Add(tex);
        }

        Shuffle(primaryDeck);
        Shuffle(secondaryDeck);

        if (secondaryDeck.Count > 8)
            secondaryDeck = secondaryDeck.GetRange(0, 8);
    }

    IEnumerator GenerateCardsWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCoroutine(GenerateCards());
    }

    IEnumerator GenerateCards()
    {
        int playerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;

        List<Texture2D> myCards = new List<Texture2D>();

        for (int i = 0; i < 6; i++)
        {
            int index = playerIndex * 6 + i;
            if (index < primaryDeck.Count)
                myCards.Add(primaryDeck[index]);
        }

        for (int i = 0; i < 2; i++)
        {
            int index = playerIndex * 2 + i;
            if (index < secondaryDeck.Count)
                myCards.Add(secondaryDeck[index]);
        }

        // 開始生成手牌
        float radius = 800f;
        float angleRange = 30f;

        for (int i = 0; i < myCards.Count; i++)
        {
            GameObject card = Instantiate(handCardPrefab, cardContainer);
            RawImage raw = card.GetComponent<RawImage>();
            raw.texture = myCards[i];

            HandCardSelect sel = card.GetComponent<HandCardSelect>();
            if (sel != null)
            {
                sel.cardColorName = raw.texture.name;
                if (sel.highlighted == null)
                    sel.highlighted = card.GetComponentInChildren<Image>(true);
                sel.InitPosition();
            }

            //扇形與旋轉
            float angle = Mathf.Lerp(-angleRange, angleRange, i / (myCards.Count - 1f));
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

        GameSceneManager.Instance.TrySyncOnceAfterGenerate();
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

