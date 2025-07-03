using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class GameSceneManager : MonoBehaviourPunCallbacks
{
    public static GameSceneManager Instance;

    [Header("¥dµP³]©w")]
    public GameObject publicCardPrefab;
    public RectTransform publiccardContainer;
    public Texture2D[] cards;
    public Image yellow_retangular;
    public HandCardGenerator handCardGenerator;

    [Header("¤¬°Êµøµ¡")]
    public GameObject confirmPanel;
    public Button confirmButton;
    public Button cancelButton;

    public GameObject failPanel;
    public Button confirmfailButton;
    public Button cancelfailButton;

    [Header("¼Ä¤è UI")]
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
    private bool hasSynced = false;

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
        failPanel.SetActive(false);
        confirmfailButton.onClick.AddListener(GiveupCard);
        cancelfailButton.onClick.AddListener(ClosefailPanel);

        handCardGenerator.StartGeneratingCards();

        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(GeneratePublicCards(1f));
            TurnManager.Instance.StartGame();
        }
    }

    public void TrySyncOnceAfterGenerate()
    {
        if (hasSynced) return;
        hasSynced = true;
        SyncMyHandCardsToSystem();
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

    private IEnumerator DelaySyncAfterDestroy()
    {
        yield return new WaitForEndOfFrame();  
        SyncMyHandCardsToSystem();             
    }

    public void OnPublicCardClicked(PublicCardSelect card)
    {
        if (!TurnManager.IsMyTurn)
        {
            Debug.Log("¤£¬O§Aªº¦^¦X¡A¤£¯à½Õ©M¤½µP¡I");
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
    public void ShowfailPanel() => failPanel.SetActive(true);
    public void ClosefailPanel() => failPanel.SetActive(false);

    public void GiveupCard()
    {
        ClosefailPanel();
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
            Debug.Log("«D¦^¦X©Î¥¼¿ï¾Ü¤½µP");
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
            {
                if (usedHand.Contains(card.cardColorName))
                    Destroy(card.gameObject); 
            }

            selectedHandCards.Clear();
            selectedHandColors.Clear();
            selectedDiceColors.Clear();
            selectedPublicCard = null;

            DiceManager.Instance.ResetDiceUI();

            RearrangeHandCards();

            
            StartCoroutine(DelaySyncAfterDestroy());
        }
        else
        {
            Debug.Log("½Õ©M¥¢±Ñ");
            CloseConfirmPanel();
            ShowfailPanel();
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
                    rt.sizeDelta = new Vector2(160f, 240f);
                    rt.anchoredPosition = new Vector2(0, -i * 50);
                }
                else if (zone == enemyZone_Left)
                {
                    rt.localRotation = Quaternion.Euler(0, 0, -90f);
                    rt.sizeDelta = new Vector2(160f, 240f);
                    rt.anchoredPosition = new Vector2(0, -i * 50);
                }
                else if (zone == enemyZone_Top)
                {
                    rt.localRotation = Quaternion.Euler(0, 0, 180f);
                    rt.sizeDelta = new Vector2(160f, 240f);
                    rt.anchoredPosition = new Vector2(i * 50, 0);
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

    private Dictionary<string, List<List<string>>> colorMixingRules = new Dictionary<string, List<List<string>>>
    {
        { "¬õ", new List<List<string>> { new List<string>{ "¬v¬õ", "¶À" }, new List<string>{ "¬õ" } } },
        { "ºñ", new List<List<string>> { new List<string>{ "«C", "¶À" }, new List<string>{ "ºñ" } } },
        { "ÂÅ", new List<List<string>> { new List<string>{ "¬v¬õ", "«C" }, new List<string>{ "ÂÅ" } } },
        { "µµ", new List<List<string>> { new List<string>{ "«C", "¬v¬õ", "¬v¬õ" }, new List<string>{ "ÂÅ", "¬v¬õ" }, new List<string>{ "µµ" } } },
        { "¦¶¬õ", new List<List<string>> { new List<string>{ "¬v¬õ", "¬v¬õ", "¶À" }, new List<string>{ "¬õ", "¬v¬õ" }, new List<string>{ "¦¶¬õ" } } },
        { "¶Àºñ", new List<List<string>> { new List<string>{ "«C", "¶À", "¶À" }, new List<string>{ "ºñ", "¶À" }, new List<string>{ "¶Àºñ" } } },
        { "«CÂÅ", new List<List<string>> { new List<string>{ "«C", "«C", "¬v¬õ" }, new List<string>{ "«C", "ÂÅ" }, new List<string>{ "«CÂÅ" } } },
        { "¾í", new List<List<string>> { new List<string>{ "¬v¬õ", "¶À", "¶À" }, new List<string>{ "¬õ", "¶À" }, new List<string>{ "¾í" } } },
        { "ÂÅºñ", new List<List<string>> { new List<string>{ "¶À", "«C", "«C" }, new List<string>{ "ºñ", "«C" }, new List<string>{ "ÂÅºñ" } } },
        { "¶Â", new List<List<string>> { new List<string>{ "¬v¬õ", "«C", "¶À" }, new List<string>{ "¬õ", "«C" }, new List<string> { "¶À", "ÂÅ" }, new List<string> { "¬v¬õ", "ºñ" }, new List<string>{ "¶Â" } } },
        { "¥Õ", new List<List<string>> { new List<string>{ "¥Õ" } } },
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
