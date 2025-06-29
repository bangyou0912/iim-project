using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameSceneManager : MonoBehaviourPun
{
    public static GameSceneManager Instance;

    [Header("公牌設定")]
    public GameObject publicCardPrefab;
    public Texture2D[] cards;
    [SerializeField]
    RectTransform publiccardContainer;
    [SerializeField]
    Image yellow_retangular;

    [Header("手牌設定")]
    public HandCardGenerator handCardGenerator;

    [Header("提示面板")]
    [SerializeField]
    GameObject confirmPanel;
    [SerializeField]
    Button confirmButton;
    [SerializeField]
    Button cancelButton;

    private List<string> selectedHandColors = new List<string>();
    private List<HandCardSelect> selectedHandCards = new List<HandCardSelect>();
    private List<string> selectedDiceColors = new List<string>();
    private List<HandCardSelect> selectedDiceCards = new List<HandCardSelect>();
    private PublicCardSelect selectedPublicCard = null;
    private List<PublicCardSelect> publicCards = new List<PublicCardSelect>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        yellow_retangular.gameObject.SetActive(false);
        confirmPanel.SetActive(false);
        confirmButton.onClick.AddListener(OnConfirmHarmonize);
        cancelButton.onClick.AddListener(CloseConfirmPanel);
        handCardGenerator.StartGeneratingCards();
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(GeneratePublicCards(1f));
        }
    }

    public IEnumerator GeneratePublicCards(float delay)
    {
        yield return new WaitForSeconds(delay);

        int cardCount = 4;
        int[] indices = new int[cardCount];

        for (int i = 0; i < cardCount; i++)
        {
            indices[i] = Random.Range(0, cards.Length);
        }

        photonView.RPC("RPC_GeneratePublicCards", RpcTarget.All, indices);
    }

    [PunRPC]
    public void RPC_GeneratePublicCards(int[] indices)
    {
        StartCoroutine(GeneratePublicCardsFromIndices(indices));
    }

    [PunRPC]
    public void RPC_ChangePublicCard(int cardIndex, string newCardName)
    {
        // 找出新貼圖
        Texture2D newTex = System.Array.Find(cards, tex => tex.name == newCardName);
        if (newTex == null) return;

        // 找出對應的公牌物件
        if (cardIndex >= 0 && cardIndex < publiccardContainer.childCount)
        {
            var cardObj = publiccardContainer.GetChild(cardIndex);
            var sel = cardObj.GetComponent<PublicCardSelect>();
            if (sel != null)
            {
                sel.SetCard(newTex);
                sel.SetSelected(false);
            }
        }
    }

    [PunRPC]
    public void RPC_RefreshPublicCard(int cardIndex, string newColorName)
    {
        if (cardIndex < 0 || cardIndex >= publicCards.Count)
        {
            Debug.LogError($"無效的 cardIndex：{cardIndex}");
            return;
        }

        Texture2D newTex = System.Array.Find(cards, tex => tex.name == newColorName);
        if (newTex == null)
        {
            Debug.LogError($"找不到顏色為 {newColorName} 的貼圖！");
            return;
        }

        publicCards[cardIndex].SetCard(newTex);
    }

    public IEnumerator GeneratePublicCardsFromIndices(int[] indices)
    {
        yield return new WaitForSeconds(0.1f); // 可加 buffer 等待場景或資料準備

        int cardCount = indices.Length;
        float cardWidth = 240f;
        float spacing = 40f;
        float totalWidth = cardCount * cardWidth + (cardCount - 1) * spacing;
        float startX = -totalWidth / 2 + cardWidth / 2;
        publicCards.Clear();

        for (int i = 0; i < cardCount; i++)
        {
            int result = indices[i];
            Texture2D tex = cards[result];

            GameObject card = Instantiate(publicCardPrefab, publiccardContainer);
            RectTransform rt = card.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(cardWidth, 360);
            rt.anchoredPosition = new Vector2(startX + i * (cardWidth + spacing), 0);

            PublicCardSelect sel = card.GetComponent<PublicCardSelect>();
            if (sel != null)
            {
                sel.Init(tex);
                publicCards.Add(sel);
            }

            CanvasGroup cg = card.AddComponent<CanvasGroup>();
            cg.alpha = 0;
            StartCoroutine(FadeInCard(cg));

            yield return new WaitForSeconds(0.15f);
        }
    }
    private IEnumerator FadeInCard(CanvasGroup cg)
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

    public void OnHandCardSelected(HandCardSelect card)
    {
        if (card.isCardSelected)
        {
            selectedHandCards.Add(card);
            selectedHandColors.Add(card.cardColorName);
        }
        else
        {
            selectedHandCards.Remove(card);
            selectedHandColors.Remove(card.cardColorName);
        }

        Debug.Log("選取的手牌顏色：" + string.Join(",", selectedHandColors));
    }
    public void SelectDiceColor(string color)
    {
        if (!selectedDiceColors.Contains(color))
            selectedDiceColors.Add(color);
        Debug.Log("選取骰子顏色：" + color);
    }

    public void DeselectDiceColor(string color)
    {
        if (selectedDiceColors.Contains(color))
            selectedDiceColors.Remove(color);
        Debug.Log("取消選取骰子顏色：" + color);
    }

    public void OnPublicCardClicked(PublicCardSelect card)
    {
        // 清除前一張公牌的選取狀態
        if (selectedPublicCard != null && selectedPublicCard != card)
            selectedPublicCard.SetSelected(false);

        selectedPublicCard = card;
        selectedPublicCard.SetSelected(true); // ← 新增這行
        ShowConfirmPanel();

        Debug.Log("想調和的公牌：" + card.cardColorName);
    }

    public void ShowConfirmPanel() => confirmPanel.SetActive(true);
    public void CloseConfirmPanel() => confirmPanel.SetActive(false);

    public void OnConfirmHarmonize()
    {
        if (selectedPublicCard == null) return;

        string targetColor = selectedPublicCard.cardColorName;

        bool success = CanHarmonize(targetColor, selectedHandColors, selectedDiceColors, out var usedHand, out var usedDice);

        if (success)
        {
            Debug.Log("調和成功！");

            // 找出這張公牌在 list 中的 index
            int cardIndex = publicCards.IndexOf(selectedPublicCard);
            if (cardIndex == -1)
            {
                Debug.LogError("找不到選取的公牌！");
                return;
            }

            // 換一張不同的顏色
            Texture2D newTex;
            do
            {
                newTex = cards[Random.Range(0, cards.Length)];
            } while (newTex.name == selectedPublicCard.cardColorName);

            // 呼叫 RPC 同步給所有人（包含自己）
            photonView.RPC("RPC_RefreshPublicCard", RpcTarget.All, cardIndex, newTex.name);

            foreach (var card in selectedHandCards)
            {
                if (usedHand.Contains(card.cardColorName))
                    Destroy(card.gameObject);
            }

            selectedHandCards.Clear();
            selectedHandColors.Clear();
            selectedDiceColors.Clear();
            selectedPublicCard = null;

            RearrangeHandCards();
        }
        else
        {
            Debug.Log("調和失敗！");
        }

        CloseConfirmPanel();
    }

    private Dictionary<string, List<List<string>>> colorMixingRules = new Dictionary<string, List<List<string>>>
    {
        { "紅", new List<List<string>> { new List<string>{ "洋紅", "黃" }, new List<string>{ "紅" } }},
        { "綠", new List<List<string>> { new List<string>{ "青", "黃" }, new List<string>{ "綠" } }},
        { "藍", new List<List<string>> { new List<string>{ "洋紅", "青" }, new List<string>{ "藍" } }},
        { "紫", new List<List<string>> { new List<string>{ "青", "洋紅", "洋紅" }, new List<string>{ "藍", "洋紅" }, new List<string>{ "紫" } }},
        { "朱紅", new List<List<string>> { new List<string>{ "洋紅", "洋紅", "黃" }, new List<string>{ "紅", "洋紅" }, new List<string>{ "朱紅" } }},
        { "黃綠", new List<List<string>> { new List<string>{ "青", "黃", "黃" }, new List<string>{ "綠", "黃" }, new List<string>{ "黃綠" } }},
        { "青藍", new List<List<string>> { new List<string>{ "青", "青", "洋紅" }, new List<string>{ "青", "藍" }, new List<string>{ "青藍" } }},
        { "橙", new List<List<string>> { new List<string>{ "洋紅", "黃", "黃" }, new List<string>{ "紅", "黃" }, new List<string>{ "橙" } }},
        { "藍綠", new List<List<string>> { new List<string>{ "黃", "青", "青" }, new List<string>{ "綠", "青" }, new List<string>{ "藍綠" } }},
        { "黑", new List<List<string>> { new List<string>{ "洋紅", "青", "黃" }, new List<string>{ "紅", "青" }, new List<string>{ "黑" } }},
        { "白", new List<List<string>> { new List<string>{ "白" } }}
    };

    public bool CanHarmonize(string targetColor, List<string> handCards, List<string> diceColors, out List<string> usedFromHand, out List<string> usedFromDice)
    {
        usedFromHand = new List<string>();
        usedFromDice = new List<string>();

        if (!colorMixingRules.ContainsKey(targetColor))
            return false;

        foreach (var recipe in colorMixingRules[targetColor])
        {
            List<string> tempHand = new List<string>(handCards);
            List<string> tempDice = new List<string>(diceColors);
            var matchHand = new List<string>();
            var matchDice = new List<string>();
            bool matched = true;

            foreach (var color in recipe)
            {
                if (tempHand.Contains(color))
                {
                    tempHand.Remove(color);
                    matchHand.Add(color);
                }
                else if (tempDice.Contains(color))
                {
                    tempDice.Remove(color);
                    matchDice.Add(color);
                }
                else
                {
                    matched = false;
                    break;
                }
            }

            if (matched)
            {
                usedFromHand = matchHand;
                usedFromDice = matchDice;
                return true;
            }
        }

        return false;
    }

    public void RearrangeHandCards()
    {
        HandCardSelect[] cards = Object.FindObjectsByType<HandCardSelect>(FindObjectsSortMode.None);
        int count = cards.Length;
        if (count == 0) return;

        float radius = 600f;            // 控制扇形圓弧大小
        float maxAngle = 35f;           // 最大總角度（左右張開角度的一半）
        float anglePerCard = (count > 1) ? (2 * maxAngle) / (count - 1) : 0f;

        for (int i = 0; i < count; i++)
        {
            float angle = -maxAngle + anglePerCard * i;
            float radians = angle * Mathf.Deg2Rad;

            float x = Mathf.Sin(radians) * radius;
            float y = Mathf.Cos(radians) * radius - radius;

            RectTransform rt = cards[i].GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);
            rt.localRotation = Quaternion.Euler(0, 0, -angle);

            cards[i].InitPosition(); // 更新 hover 的原始位置與角度
        }
    }


}
