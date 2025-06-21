using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance;

    [Header("生成設定")]
    public GameObject publicCardPrefab;
    public RectTransform cardContainer;
    public Texture2D[] cards;
    public Image yellow_retangular;
    public HandCardGenerator handCardGenerator;

    public GameObject confirmPanel;
    public Button confirmButton;
    public Button cancelButton;

    private List<string> selectedHandColors = new List<string>();
    private List<CardSelectable> selectedHandCards = new List<CardSelectable>();

    private List<string> selectedDiceColors = new List<string>();
    private List<CardSelectable> selectedDiceCards = new List<CardSelectable>();

    private CardSelectable selectedPublicCard = null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        yellow_retangular.gameObject.SetActive(false);
        StartCoroutine(GeneratePublicCards(1f));
        handCardGenerator.StartGeneratingCards();
        confirmPanel.SetActive(false); // 開場隱藏
        confirmButton.onClick.AddListener(OnConfirmHarmonize);
        cancelButton.onClick.AddListener(CloseConfirmPanel);
    }

    public IEnumerator GeneratePublicCards(float delay)
    {
        yield return new WaitForSeconds(delay);

        for (int i = 0; i < 4; i++)
        {
            int result = Random.Range(0, cards.Length);
            GameObject card = Instantiate(publicCardPrefab, cardContainer);
            RawImage raw = card.GetComponent<RawImage>();
            raw.texture = cards[result];

            change_card changer = card.GetComponent<change_card>();
            if (changer != null)
            {
                changer.cards = cards;
                changer.rawImages = raw;
                changer.yellow_retangular = yellow_retangular;
            }

            CardSelectable sel = card.GetComponent<CardSelectable>();
            if (sel != null)
            {
                sel.cardColorName = GetColorNameFromTexture(raw.texture);
                sel.cardtype = CardSelectable.CardType.Public;
                if (sel.highlighted != null)
                    sel.highlighted.gameObject.SetActive(false);
            }

            card.transform.localPosition = new Vector3(-378f + i * 248f, 141f, 0f);
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

    public void OnCardSelected(CardSelectable card)
    {
        if (card.cardtype == CardSelectable.CardType.Hand)
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
        }
        else if (card.cardtype == CardSelectable.CardType.Dice)
        {
            if (card.isCardSelected)
            {
                selectedDiceCards.Add(card);
                selectedDiceColors.Add(card.cardColorName);
            }
            else
            {
                selectedDiceCards.Remove(card);
                selectedDiceColors.Remove(card.cardColorName);
            }
        }

        Debug.Log("選取的手牌顏色：" + string.Join(",", selectedHandColors));
        Debug.Log("選取的骰子顏色：" + string.Join(",", selectedDiceColors));
    }

    public void OnPublicCardClicked(CardSelectable card)
    {
        selectedPublicCard = card;
        ShowConfirmPanel();

        Debug.Log("想調和的公牌：" + card.cardColorName);
        // TODO: ShowConfirmPanel(card.cardColorName);
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

        List<List<string>> options = colorMixingRules[targetColor];

        foreach (var recipe in options)
        {
            List<string> tempHand = new List<string>(handCards);
            List<string> tempDice = new List<string>(diceColors);

            List<string> matchedHand = new List<string>();
            List<string> matchedDice = new List<string>();

            bool match = true;

            foreach (string color in recipe)
            {
                if (tempHand.Contains(color))
                {
                    tempHand.Remove(color);
                    matchedHand.Add(color);
                }
                else if (tempDice.Contains(color))
                {
                    tempDice.Remove(color);
                    matchedDice.Add(color);
                }
                else
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                usedFromHand = matchedHand;
                usedFromDice = matchedDice;
                return true;
            }
        }

        return false;
    }

    private string GetColorNameFromTexture(Texture texture)
    {
        return texture.name; // 根據圖片名稱來當作顏色名（你可以自訂規則）
    }

    public void ShowConfirmPanel()
    {
        confirmPanel.SetActive(true);
    }
    public void CloseConfirmPanel()
    {
        confirmPanel.SetActive(false);
    }
    public void OnConfirmHarmonize()
    {
        if (selectedPublicCard == null) return;

        string targetColor = selectedPublicCard.cardColorName;

        bool success = CanHarmonize(targetColor, selectedHandColors, selectedDiceColors, out var usedHand, out var usedDice);

        if (success)
        {
            Debug.Log("調和成功！");
            // TODO: 換公牌、移除手牌等效果
        }
        else
        {
            Debug.Log("調和失敗！");
            // TODO: 顯示失敗提示
        }

        CloseConfirmPanel();
    }
}
