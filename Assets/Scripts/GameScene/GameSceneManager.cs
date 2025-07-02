// 整合第一版 + 第二版，保留所有功能並加入敵方手牌顯示功能
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class GameSceneManager : MonoBehaviourPunCallbacks
{
    public static GameSceneManager Instance;

    [Header("卡牌設定")]
    public GameObject publicCardPrefab;
    public RectTransform publiccardContainer;
    public Texture2D[] cards;
    public Image yellow_retangular;
    public HandCardGenerator handCardGenerator;

    [Header("UI 元素")]
    public GameObject confirmPanel;
    public Button confirmButton;
    public Button cancelButton;

    [Header("敵方 UI")]
    [SerializeField] private GameObject cardBackPrefab;
    [SerializeField] private Transform enemyZone_Top;
    [SerializeField] private Transform enemyZone_Left;
    [SerializeField] private Transform enemyZone_Right;

    private List<string> selectedHandColors = new List<string>();
    private List<HandCardSelect> selectedHandCards = new List<HandCardSelect>();
    private List<string> selectedDiceColors = new List<string>();
    private List<HandCardSelect> selectedDiceCards = new List<HandCardSelect>();
    private PublicCardSelect selectedPublicCard = null;
    private List<PublicCardSelect> publicCards = new List<PublicCardSelect>();
    
    public GameObject darkBackground;
    private Dictionary<int, List<string>> playerHands = new Dictionary<int, List<string>>();

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
        StartCoroutine(DelaySyncHandCards());

        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(GeneratePublicCards(1f));
            TurnManager.Instance.StartGame();
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

    public IEnumerator GeneratePublicCardsFromIndices(int[] indices)
    {
        yield return new WaitForSeconds(0.1f);

        int cardCount = indices.Length;
        float cardWidth = 240f;
        float spacing = 40f;
        float totalWidth = cardCount * cardWidth + (cardCount - 1) * spacing;
        float startX = -totalWidth / 2 + cardWidth / 2;
        publicCards.Clear();

        for (int i = 0; i < cardCount; i++)
        {
            Texture2D tex = cards[indices[i]];
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

    public void OnPublicCardClicked(PublicCardSelect card)
    {
        if (!TurnManager.IsMyTurn)
        {
            Debug.Log("不是你的回合，不能調和公牌！");
            //darkBackground.SetActive(TurnManager.IsMyTurn);
            return;
        }

        if (selectedPublicCard != null && selectedPublicCard != card)
            selectedPublicCard.SetSelected(false);

        selectedPublicCard = card;
        selectedPublicCard.SetSelected(true);
        ShowConfirmPanel();
    }

    public void ShowConfirmPanel() => confirmPanel.SetActive(true);
    public void CloseConfirmPanel() => confirmPanel.SetActive(false);

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
    }

    public void SelectDiceColor(string color)
    {
        if (!selectedDiceColors.Contains(color))
            selectedDiceColors.Add(color);
    }

    public void DeselectDiceColor(string color)
    {
        if (selectedDiceColors.Contains(color))
            selectedDiceColors.Remove(color);
    }

    public void OnConfirmHarmonize()
    {
        if (!TurnManager.IsMyTurn || selectedPublicCard == null)
        {
            Debug.Log("非回合或未選擇公牌");

            return;
        }

        string targetColor = selectedPublicCard.cardColorName;
        bool success = CanHarmonize(targetColor, selectedHandColors, selectedDiceColors, out var usedHand, out var usedDice);

        if (success)
        {
            int cardIndex = publicCards.IndexOf(selectedPublicCard);
            if (cardIndex == -1) return;

            Texture2D newTex;
            do
            {
                newTex = cards[Random.Range(0, cards.Length)];
            } while (newTex.name == selectedPublicCard.cardColorName);

            photonView.RPC("RPC_RefreshPublicCard", RpcTarget.All, cardIndex, newTex.name);

            foreach (var card in selectedHandCards)
                if (usedHand.Contains(card.cardColorName)) Destroy(card.gameObject);

            selectedHandCards.Clear();
            selectedHandColors.Clear();
            selectedDiceColors.Clear();
            selectedPublicCard = null;
            
            DiceManager.Instance.ResetDiceUI();
            
            RearrangeHandCards();
            SyncMyHandCardsToSystem();
        }
        else
        {
            Debug.Log("調和失敗");
        }
        CloseConfirmPanel();
    }

    [PunRPC]
    public void RPC_RefreshPublicCard(int cardIndex, string newColorName)
    {
        if (cardIndex < 0 || cardIndex >= publicCards.Count) return;
        Texture2D tex = System.Array.Find(cards, c => c.name == newColorName);
        if (tex != null)
            publicCards[cardIndex].SetCard(tex);
    }

    [PunRPC]
    public void RPC_SyncHandCards(int actorNumber, string[] colorArray)
    {
        playerHands[actorNumber] = new List<string>(colorArray);
        UpdateEnemyHandUI();
    }

    public void SyncMyHandCardsToSystem()
    {
        int actor = PhotonNetwork.LocalPlayer.ActorNumber;
        HandCardSelect[] cards = Object.FindObjectsByType<HandCardSelect>(FindObjectsSortMode.None);
        List<string> colorList = new List<string>();
        foreach (var c in cards) colorList.Add(c.cardColorName);

        photonView.RPC("RPC_SyncHandCards", RpcTarget.All, actor, colorList.ToArray());
    }

    public void UpdateEnemyHandUI()
    {
        foreach (Transform t in enemyZone_Top) Destroy(t.gameObject);
        foreach (Transform t in enemyZone_Left) Destroy(t.gameObject);
        foreach (Transform t in enemyZone_Right) Destroy(t.gameObject);

        Player[] players = PhotonNetwork.PlayerList;
        int self = PhotonNetwork.LocalPlayer.ActorNumber;

        foreach (var p in players)
        {
            if (p.ActorNumber == self || !playerHands.ContainsKey(p.ActorNumber)) continue;

            int count = playerHands[p.ActorNumber].Count;
            Transform zone = GetEnemyZoneByIndex(players, p);
            for (int i = 0; i < count; i++)
            {
                GameObject card = Instantiate(cardBackPrefab, zone);
                RectTransform rt = card.GetComponent<RectTransform>();

                if (zone == enemyZone_Right)
                {
                    rt.localRotation = Quaternion.Euler(0, 0, 90f);
                    rt.sizeDelta = new Vector2(120f, 180f);
                    rt.anchoredPosition = new Vector2(0, -i * 30); // 垂直往下排列
                }
                else if (zone == enemyZone_Left)
                {
                    rt.localRotation = Quaternion.Euler(0, 0, -90f);
                    rt.sizeDelta = new Vector2(120f, 180f);
                    rt.anchoredPosition = new Vector2(0, -i * 30);
                }
                else if (zone == enemyZone_Top)
                {
                    rt.localRotation = Quaternion.identity;
                    rt.sizeDelta = new Vector2(120f, 180f);
                    rt.anchoredPosition = new Vector2(i * 30, 0); // 水平排
                }
            }
        }
    }

    private Transform GetEnemyZoneByIndex(Player[] players, Player enemy)
    {
        List<Player> sorted = new List<Player>(players);
        sorted.Remove(PhotonNetwork.LocalPlayer);
        sorted.Sort((a, b) => a.ActorNumber.CompareTo(b.ActorNumber));

        int index = sorted.IndexOf(enemy);
        if (index == 0) return enemyZone_Right;
        else if (index == 1) return enemyZone_Top;
        else return enemyZone_Left;
    }

    private IEnumerator DelaySyncHandCards()
    {
        yield return new WaitForSeconds(0.5f);
        SyncMyHandCardsToSystem();
    }

    private Dictionary<string, List<List<string>>> colorMixingRules = new Dictionary<string, List<List<string>>>
    {
        { "紅", new List<List<string>> { new List<string>{ "洋紅", "黃" }, new List<string>{ "紅" } } },
        { "綠", new List<List<string>> { new List<string>{ "青", "黃" }, new List<string>{ "綠" } } },
        { "藍", new List<List<string>> { new List<string>{ "洋紅", "青" }, new List<string>{ "藍" } } },
        { "紫", new List<List<string>> { new List<string>{ "青", "洋紅", "洋紅" }, new List<string>{ "藍", "洋紅" }, new List<string>{ "紫" } } },
        { "朱紅", new List<List<string>> { new List<string>{ "洋紅", "洋紅", "黃" }, new List<string>{ "紅", "洋紅" }, new List<string>{ "朱紅" } } },
        { "黃綠", new List<List<string>> { new List<string>{ "青", "黃", "黃" }, new List<string>{ "綠", "黃" }, new List<string>{ "黃綠" } } },
        { "青藍", new List<List<string>> { new List<string>{ "青", "青", "洋紅" }, new List<string>{ "青", "藍" }, new List<string>{ "青藍" } } },
        { "橙", new List<List<string>> { new List<string>{ "洋紅", "黃", "黃" }, new List<string>{ "紅", "黃" }, new List<string>{ "橙" } } },
        { "藍綠", new List<List<string>> { new List<string>{ "黃", "青", "青" }, new List<string>{ "綠", "青" }, new List<string>{ "藍綠" } } },
        { "黑", new List<List<string>> { new List<string>{ "洋紅", "青", "黃" }, new List<string>{ "紅", "青" }, new List<string> { "黃", "藍" }, new List<string> { "洋紅", "綠" },new List<string>{ "黑" } } },
        { "白", new List<List<string>> { new List<string>{ "白" } } },
    };

    public bool CanHarmonize(string targetColor, List<string> handCards, List<string> diceColors, out List<string> usedFromHand, out List<string> usedFromDice)
    {
        usedFromHand = new List<string>();
        usedFromDice = new List<string>();

        if (!colorMixingRules.ContainsKey(targetColor)) return false;

        foreach (var recipe in colorMixingRules[targetColor])
        {
            List<string> tempHand = new List<string>(handCards);
            List<string> tempDice = new List<string>(diceColors);
            List<string> matchHand = new List<string>();
            List<string> matchDice = new List<string>();
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

        float radius = 600f;
        float maxAngle = 35f;
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
            cards[i].InitPosition();
        }
    }
}
