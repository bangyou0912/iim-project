using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using static HandCardSelect;
using TMPro;
using Photon.Pun.Demo.PunBasics;
using System.Drawing;
//using static System.Net.Mime.MediaTypeNames;

public class GameSceneManager : MonoBehaviourPunCallbacks
{
    public static GameSceneManager Instance;
    private bool isWhiteCardExchangeInProgress = false;
    public Dictionary<int, int> discardCounts = new Dictionary<int, int>(); // 棄牌次數
    public HashSet<int> eliminatedPlayers = new HashSet<int>(); // 出局玩家

    [Header("卡牌設定")]
    public GameObject publicCardPrefab;
    public RectTransform publiccardContainer;
    public Texture2D[] cards;
    public Image yellow_retangular;
    public HandCardGenerator handCardGenerator;
    private List<Texture2D> publicCardPool = new List<Texture2D>();
    private int publicCardIndex = 0;  // 控制從 publicCardPool 取第幾張

    [Header("互動視窗")]
    public GameObject confirmPanel;
    public Button confirmButton;
    public Button cancelButton;

    public GameObject failPanel;
    public Button confirmfailButton;
    public Button cancelfailButton;

    private HandCardSelect pendingDiscardCard = null;
    public GameObject discardConfirmPanel;
    public Button discardYesButton;
    public Button discardNoButton;

    public GameObject whiteCardHintText;
    public GameObject exchangeCardText;
    private List<int> whiteCardIndicesToDestroyAfterTransfer = new List<int>();

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
    private bool hasSynced = false;

    private Dictionary<int, int> playerGems = new Dictionary<int, int>();

    [Header("寶石獎勵面板")]
    public GameObject gemRewardPanel_1;
    public GameObject gemRewardPanel_2;
    public GameObject gemRewardPanel_3;
    public TMP_Text gemText;


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
            InitializePublicCardPool();
            StartCoroutine(GeneratePublicCards(1f));
            TurnManager.Instance.StartGame();
        }

        foreach (var player in PhotonNetwork.PlayerList)
        {
            playerGems[player.ActorNumber] = 3;
            discardCounts[player.ActorNumber] = 0; 
            UpdateGemUI();
        }
    }

    private void InitializePublicCardPool()
    {
        publicCardPool.Clear();

        string[] secondaryColorNames = { "紅", "綠", "藍" };
        string[] tertiaryColorNames = { "紫", "橙", "青藍", "黃綠", "朱紅", "藍綠" };

        foreach (var name in secondaryColorNames)
            AddToPoolByName(name,1);
        foreach (var name in tertiaryColorNames)
            AddToPoolByName(name,1);

        AddToPoolByName("白",8);
        AddToPoolByName("黑",8);
        

        // 洗牌
        for (int i = 0; i < publicCardPool.Count; i++)
        {
            var temp = publicCardPool[i];
            int randIndex = Random.Range(i, publicCardPool.Count);
            publicCardPool[i] = publicCardPool[randIndex];
            publicCardPool[randIndex] = temp;
        }

        string[] cardNames = publicCardPool.ConvertAll(tex => tex.name).ToArray();
        photonView.RPC("RPC_SyncPublicCardPool", RpcTarget.Others, cardNames);
        //Debug.Log($"主機已呼叫 RPC_SyncPublicCardPool，傳送 {cardNames.Length} 張卡。");
    }

    private void AddToPoolByName(string name, int count)
    {
        Texture2D tex = System.Array.Find(cards, t => t.name == name);
        if (tex == null) return;

        for (int i = 0; i < count; i++)
            publicCardPool.Add(tex);
    }

    [PunRPC]
    public void RPC_SyncPublicCardPool(string[] colorNames)
    {
        publicCardPool.Clear();
        foreach (string name in colorNames)
        {
            var tex = System.Array.Find(cards, t => t.name == name);
            if (tex != null)
                publicCardPool.Add(tex);
        }
        Debug.Log($"已同步 publicCardPool，共 {publicCardPool.Count} 張卡。");
        Debug.Log("publicCardPool 內容：" + string.Join(", ", publicCardPool.ConvertAll(t => t.name)));
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
        string[] selectedNames = new string[cardCount];
        for (int i = 0; i < cardCount; i++)
        {
            if (publicCardIndex < publicCardPool.Count)
            {
                selectedNames[i] = publicCardPool[publicCardIndex].name;
                UpdatePublicCardIndex(publicCardIndex + 1);
            }
        }
        UpdatePublicCardIndex(publicCardIndex - 1); //調整第一次生成公牌後的牌庫索引
        //Debug.Log("初始公牌生成結束：牌庫"+publicCardIndex);
        photonView.RPC("RPC_GeneratePublicCards_ByNames", RpcTarget.All, selectedNames);

    }
    [PunRPC]
    public void RPC_GeneratePublicCards_ByNames(string[] colorNames)
    {
        StartCoroutine(GeneratePublicCardsFromNames(colorNames));
    }
    public IEnumerator GeneratePublicCardsFromNames(string[] colorNames)
    {
        yield return new WaitForSeconds(0.1f);

        float cardWidth = 240f;
        float spacing = 40f;
        float totalWidth = colorNames.Length * cardWidth + (colorNames.Length - 1) * spacing;
        float startX = -totalWidth / 2 + cardWidth / 2;
        publicCards.Clear();

        for (int i = 0; i < colorNames.Length; i++)
        {
            Texture2D tex = System.Array.Find(cards, t => t.name == colorNames[i]);
            if (tex == null) continue;

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
        if (PhotonNetwork.IsMasterClient)
            StartCoroutine(DelayCheckWhiteCard());
    }
    private void UpdatePublicCardIndex(int newIndex) //更新牌庫目前取到第幾張
    {
        publicCardIndex = newIndex;
        photonView.RPC("RPC_SyncPublicCardIndex", RpcTarget.Others, newIndex);
        //Debug.Log($"MasterClient 更新並同步 publicCardIndex: {newIndex}");
    }

    [PunRPC]
    private void RPC_SyncPublicCardIndex(int newIndex)
    {
        publicCardIndex = newIndex;
        //Debug.Log($"同步 publicCardIndex: {publicCardIndex}");
    }

    private IEnumerator DelayCheckWhiteCard()                       //白色卡功能
    {
        yield return new WaitForSeconds(2.5f);

        // 等待直到交換結束，避免中斷流程
        while (isWhiteCardExchangeInProgress)
        {
            Debug.Log("白卡交換仍在進行中，等待結束");
            yield return new WaitForSeconds(0.5f);
        }

        CheckAndTriggerWhiteCardTransfer();
    }

    public void CheckAndTriggerWhiteCardTransfer()                 
    {
        if (!PhotonNetwork.IsMasterClient) return;

        List<int> whiteCardIndices = new List<int>();
        whiteCardIndicesToDestroyAfterTransfer.Clear(); // 確保清空舊資料

        for (int i = 0; i < publicCards.Count; i++)
        {
            if (publicCards[i] != null && publicCards[i].cardColorName == "白")
            {
                whiteCardIndices.Add(i);

                // 當牌庫已經用完，暫存等待銷毀
                if (publicCardIndex >= publicCardPool.Count)
                {
                    whiteCardIndicesToDestroyAfterTransfer.Add(i);
                }
            }
        }

        if (whiteCardIndicesToDestroyAfterTransfer.Count > 0 && publicCardIndex >= publicCardPool.Count)
        {
            Debug.LogWarning("牌庫用盡，只執行白卡銷毀，不再交換");

            foreach (int index in whiteCardIndicesToDestroyAfterTransfer)
            {
                photonView.RPC("RPC_DestroyPublicCard", RpcTarget.All, index);
                Debug.LogWarning($"已銷毀白卡 Index {index}");
            }

            whiteCardIndicesToDestroyAfterTransfer.Clear();
            return;
        }

        if (whiteCardIndices.Count > 0)
        {
            Debug.Log($"有 {whiteCardIndices.Count} 張白色卡，觸發交換與刷新");
            PhotonView.Get(TurnManager.Instance)?.RPC("RPC_PauseTurnTimer", RpcTarget.All);
            isWhiteCardExchangeInProgress = true;
            photonView.RPC("RPC_ShowWhiteCardHintText", RpcTarget.All);
            StartCoroutine(TriggerTransferAndRefreshAfterDelay(whiteCardIndices, 3f));
        }
    }
    private IEnumerator TriggerTransferAndRefreshAfterDelay(List<int> indices, float delay)
    {
        yield return new WaitForSeconds(delay);
        whiteCardHintText.SetActive(false);
        photonView.RPC("RPC_HideWhiteCardHintText", RpcTarget.All);
        photonView.RPC("RPC_TriggerCardTransfer", RpcTarget.All);

        if (!PhotonNetwork.IsMasterClient) yield break;

        foreach (int i in indices)
        {
            if (publicCardIndex < publicCardPool.Count)
            {
                int poolIndexToUse = publicCardIndex;
                UpdatePublicCardIndex(poolIndexToUse+1);
                photonView.RPC("RPC_RefreshPublicCard", RpcTarget.All, i, poolIndexToUse+1);
            }
            else
            {
                Debug.LogWarning("已無可用的公牌卡，刷新中止");
            }

        }
    }

    private Dictionary<int, string> pendingTransfers = new Dictionary<int, string>();
    [PunRPC]
    public void RPC_TriggerCardTransfer()
    {
        StartCoroutine(EnsureHandCardAndSubmit());
        
    }

    private IEnumerator EnsureHandCardAndSubmit()
    {
        float timer = 0f;
        while (handCardGenerator.cardContainer.childCount == 0 && timer < 2f)
        {
            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;
        }

        HandCardSelect[] myHandCards = handCardGenerator.cardContainer.GetComponentsInChildren<HandCardSelect>();
        if (myHandCards.Length == 0)
        {
            Debug.LogWarning($"玩家 {PhotonNetwork.LocalPlayer.ActorNumber} 沒有卡可提交，跳過");
            exchangeCardText.SetActive(true);
            exchangeCardText.GetComponent<TextMeshProUGUI>().text = "跳過本次交換";
            StartCoroutine(ShowExchangeCardTextSequence());
            photonView.RPC("RPC_SkipTransfer", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
            yield break;
        }

        yield return StartCoroutine(AnimateCardSelectionCoroutine(myHandCards));
    }
    [PunRPC]
    public void RPC_ShowWhiteCardHintText()
    {
        whiteCardHintText.SetActive(true);
    }

    [PunRPC]
    public void RPC_HideWhiteCardHintText()
    {
        whiteCardHintText.SetActive(false);
    }

    private IEnumerator ShowExchangeCardTextSequence()
    {
        exchangeCardText.SetActive(true); 
        yield return new WaitForSeconds(2f);
        exchangeCardText.GetComponent<TextMeshProUGUI>().text = " ";
        exchangeCardText.SetActive(false);
    }
    [PunRPC]
    public void RPC_ShowExchangeCancelledMessage()
    {
        StartCoroutine(ShowExchangeCardTextSequence());
    }
    private IEnumerator AnimateCardSelectionCoroutine(HandCardSelect[] cards) //選牌特效
    {
        int totalSteps = cards.Length * 2 + Random.Range(0, cards.Length);
        int currentIndex = 0;

        for (int i = 0; i < totalSteps; i++)
        {
            foreach (var c in cards) c.SetHighlight(false);
            cards[currentIndex].SetHighlight(true);
            yield return new WaitForSeconds(0.08f + i * 0.01f);
            currentIndex = (currentIndex + 1) % cards.Length;
        }

        foreach (var c in cards) c.SetHighlight(false);
        HandCardSelect chosen = cards[currentIndex % cards.Length];

        // 閃爍效果
        float blinkDuration = 2f;
        float blinkTimer = 0f;
        bool isOn = true;

        while (blinkTimer < blinkDuration)
        {
            chosen.SetHighlight(isOn);
            isOn = !isOn;
            yield return new WaitForSeconds(0.2f);
            blinkTimer += 0.2f;
        }

        if (chosen != null)
        { 
            chosen.SetHighlight(false);
            string color = chosen.cardColorName;
            int actor = PhotonNetwork.LocalPlayer.ActorNumber;
            exchangeCardText.SetActive(true);
            exchangeCardText.GetComponent<TextMeshProUGUI>().text = $"您即將交換的手牌是：{color}";
            StartCoroutine(ShowExchangeCardTextSequence());

            StartCoroutine(DelayRearrange());            

            Debug.Log($"玩家 {actor} 最終選擇要傳出的卡是：{color}");
            photonView.RPC("RPC_SubmitCardForTransfer", RpcTarget.MasterClient, actor, color);
        }
        else
        {
            Debug.LogWarning("選擇的卡片已不存在，無法提交。");
        }
    }

    public Texture2D GetHandCardTextureByName(string colorName) //從手牌堆中尋找對應顏色
    {
        foreach (var tex in handCardGenerator.primaryColors)
            if (tex.name == colorName) return tex;

        foreach (var tex in handCardGenerator.secondaryColors)
            if (tex.name == colorName) return tex;

        return null;
    }

    [PunRPC]
    public void RPC_SubmitCardForTransfer(int actor, string color)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        pendingTransfers[actor] = color;
        TryResolveTransfer();
        PhotonView.Get(TurnManager.Instance)?.RPC("RPC_ResumeTurnTimer", RpcTarget.All);
    }

    [PunRPC]
    public void RPC_ReceiveCardFromOther(int receiverActor, string colorName)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber != receiverActor) return;
        //Debug.Log($"玩家 {receiverActor} 準備接收一張卡：{colorName}");
        StartCoroutine(DelayReceiveCard(colorName));
    }

    private IEnumerator DelayReceiveCard(string colorName)
    {
        yield return new WaitForSeconds(1f);

        Texture2D tex = GetHandCardTextureByName(colorName);
        if (tex == null)
        {
            Debug.LogWarning($"無法找到顏色：{colorName}，玩家 {PhotonNetwork.LocalPlayer.ActorNumber} 沒收到卡");
            yield break;
        }

        GameObject newCard = Instantiate(handCardGenerator.handCardPrefab, handCardGenerator.cardContainer);
        RawImage raw = newCard.GetComponent<RawImage>();
        raw.texture = tex;

        HandCardSelect hcs = newCard.GetComponent<HandCardSelect>();
        hcs.cardColorName = tex.name;
        hcs.SetMode(HandCardMode.Normal);
        hcs.InitPosition();
        exchangeCardText.SetActive(true);
        exchangeCardText.GetComponent<TextMeshProUGUI>().text = $"收到手牌：{colorName}";
        StartCoroutine(ShowExchangeCardTextSequence());
        Debug.Log($"玩家 {PhotonNetwork.LocalPlayer.ActorNumber} 成功接收到卡：{colorName}");

        StartCoroutine(DelayRearrange());
        SyncMyHandCardsToSystem();
    }

    [PunRPC]
    public void RPC_SkipTransfer(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (!pendingTransfers.ContainsKey(actorNumber))
        {
            pendingTransfers[actorNumber] = null;
        }

        TryResolveTransfer();
    }

    private void TryResolveTransfer()
    {
        // 尚未所有玩家提交
        if (pendingTransfers.Count < PhotonNetwork.PlayerList.Length)
            return;

        // 收集有提交卡的玩家
        List<int> activeActors = new List<int>();
        foreach (var kvp in pendingTransfers)
        {
            if (!string.IsNullOrEmpty(kvp.Value))
            {
                activeActors.Add(kvp.Key);
            }
        }

        if (activeActors.Count < 2)
        {
            exchangeCardText.SetActive(true);
            exchangeCardText.GetComponent<TextMeshProUGUI>().text = "交換人數不足,取消交換";
            photonView.RPC("RPC_ShowExchangeCancelledMessage", RpcTarget.All);

            Debug.Log("提交卡片的人數不足 (<2)，交換取消。");
            isWhiteCardExchangeInProgress = false;
            pendingTransfers.Clear();
            if (PhotonNetwork.IsMasterClient)
                StartCoroutine(DelayCheckWhiteCard());
            return;
        }

        activeActors.Sort(); // 保持順序一致

        for (int i = 0; i < activeActors.Count; i++)
        {
            int fromActor = activeActors[i];
            int toActor = activeActors[(i + 1) % activeActors.Count];

            string colorToSend = pendingTransfers[fromActor];
            photonView.RPC("RPC_DestroyCardAndReceive", RpcTarget.All, fromActor, toActor, colorToSend);
            Debug.Log($"轉移：從 {fromActor} 的 {colorToSend} 給 {toActor}");
        }

        pendingTransfers.Clear();
        isWhiteCardExchangeInProgress = false;
        if (PhotonNetwork.IsMasterClient)
        {
            if (whiteCardIndicesToDestroyAfterTransfer.Count > 0)
            {
                foreach (int index in whiteCardIndicesToDestroyAfterTransfer)
                {
                    photonView.RPC("RPC_DestroyPublicCard", RpcTarget.All, index);
                    Debug.Log($"已銷毀牌庫用盡時的白卡：Index {index}");
                }
                whiteCardIndicesToDestroyAfterTransfer.Clear();

                StartCoroutine(DelayCheckWhiteCardAfterDelay(1.5f));
            }
            else
            {
                StartCoroutine(DelayCheckWhiteCard());
            }
        }

    }
    private IEnumerator DelayCheckWhiteCardAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCoroutine(DelayCheckWhiteCard());
    }

    [PunRPC]
    public void RPC_DestroyCardAndReceive(int fromActor, int toActor, string color)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == fromActor)
        {
            // 找手牌中指定顏色卡牌並銷毀
            HandCardSelect cardToDestroy = FindCardByColor(color);
            if (cardToDestroy != null)
            {
                Destroy(cardToDestroy.gameObject);
            }
        }
        if (PhotonNetwork.LocalPlayer.ActorNumber == toActor)
        {
            StartCoroutine(DelayReceiveCard(color));
        }
        SyncMyHandCardsToSystem();
    }

                                                            //白色卡功能結束
    private HandCardSelect FindCardByColor(string color)
    {
        HandCardSelect[] handCards = handCardGenerator.cardContainer.GetComponentsInChildren<HandCardSelect>();
        foreach (var card in handCards)
        {
            if (card.cardColorName == color)
                return card;
        }
        return null;
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
            Debug.Log("不是你的回合，不能調和公牌！");
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

    public void GiveupCard()//棄牌
    {
        ClosefailPanel();
        EnableDiscardSelection();
        DiceManager.Instance.DeselectResultDiceVisual();
    }
    public void EnableDiscardSelection()
    {
        Debug.Log("請選擇要棄掉的手牌");

        foreach (var card in Object.FindObjectsByType<HandCardSelect>(FindObjectsSortMode.None))
        {
            //Debug.Log("設定卡牌為 Discard 模式: " + card.cardColorName);
            card.SetMode(HandCardMode.DiscardSelection, OnHandCardChosenToDiscard);
        }
    }
    public void OnHandCardChosenToDiscard(HandCardSelect card)
    {
        pendingDiscardCard = card;
        ShowDiscardConfirmPanel();
        Debug.Log("確認是否棄牌");
    }
    public void ShowDiscardConfirmPanel()
    {
        discardConfirmPanel.SetActive(true);
        discardYesButton.onClick.RemoveAllListeners();
        discardNoButton.onClick.RemoveAllListeners();

        discardYesButton.onClick.AddListener(ConfirmDiscard);
        discardNoButton.onClick.AddListener(CancelDiscard);
    }

    public void HideDiscardConfirmPanel()
    {
        discardConfirmPanel.SetActive(false);
    }
    public void ConfirmDiscard()
    {
        if (pendingDiscardCard != null)
        {
            Destroy(pendingDiscardCard.gameObject);
            RearrangeHandCards();
        }

        // 新增棄牌次數
        int actor = PhotonNetwork.LocalPlayer.ActorNumber;
        discardCounts[actor]++;
        Debug.Log($"玩家 {actor} 棄牌第 {discardCounts[actor]} 次");

        // 若已達3次，標記為出局
        if (discardCounts[actor] >= 3)
        {
            eliminatedPlayers.Add(actor);
            Debug.Log($"玩家 {actor} 因為棄牌三次出局");

            // 若是自己，顯示提示（可加 UI）
            if (PhotonNetwork.LocalPlayer.ActorNumber == actor)
            {
                exchangeCardText.SetActive(true);
                exchangeCardText.GetComponent<TextMeshProUGUI>().text = "您已出局！";
                StartCoroutine(ShowExchangeCardTextSequence());
            }
        }

        // 發新牌 + 同步
        Texture2D[] primaryColors = handCardGenerator.primaryColors;
        Texture2D randomPrimary = primaryColors[Random.Range(0, primaryColors.Length)];
        GameObject newCard = Instantiate(handCardGenerator.handCardPrefab, handCardGenerator.cardContainer);
        RawImage raw = newCard.GetComponent<RawImage>();
        raw.texture = randomPrimary;

        HandCardSelect hcs = newCard.GetComponent<HandCardSelect>();
        hcs.cardColorName = randomPrimary.name;
        hcs.SetMode(HandCardMode.Normal);
        hcs.InitPosition();

        StartCoroutine(DelayRearrange());
        SyncMyHandCardsToSystem();

        pendingDiscardCard = null;
        HideDiscardConfirmPanel();
        ResetHandCardMode();
        DiceManager.Instance.ResetDiceUI();

        DelayCheckIfAllPlayersNoHandCards();
    }


    public void CancelDiscard()
    {
        pendingDiscardCard = null;
        HideDiscardConfirmPanel();
        EnableDiscardSelection(); 
        ResetHandCardMode();
    }

    private void ResetHandCardMode()
    {
        foreach (var card in Object.FindObjectsByType<HandCardSelect>(FindObjectsSortMode.None))
        {
            card.SetMode(HandCardMode.Normal);
        }
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
        //if (!selectedDiceColors.Contains(color))
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
        int gemReward = 0;

        if (success)
        {
            int cardIndex = publicCards.IndexOf(selectedPublicCard);
            if (cardIndex == -1) return;

                if (publicCardIndex < publicCardPool.Count-1)
                {
                    int poolIndexToUse = publicCardIndex+1;
                    UpdatePublicCardIndex(poolIndexToUse);
                    photonView.RPC("RPC_RefreshPublicCard", RpcTarget.All, cardIndex, poolIndexToUse);
                // Debug.LogWarning("OnConfirmHarmonize：從牌庫中換牌" + publicCardIndex);
                }
                else
                {
                    Debug.LogWarning("公牌牌庫已用完，無法刷新新的卡牌");
                    photonView.RPC("RPC_DestroyPublicCard", RpcTarget.All, cardIndex);
                }
            


            // 先複製要銷毀的卡片
            List<GameObject> cardsToDestroy = new List<GameObject>();
            foreach (var card in selectedHandCards)
            {
                if (usedHand.Contains(card.cardColorName) && card != null && card.gameObject != null)
                {
                    cardsToDestroy.Add(card.gameObject);
                }
            }

            // 清空選取資料
            selectedHandCards.Clear();
            selectedHandColors.Clear();
            selectedDiceColors.Clear();
            selectedPublicCard = null;

            DiceManager.Instance.ResetDiceUI();

            // 銷毀卡牌
            foreach (var obj in cardsToDestroy)
            {
                if (obj != null)
                    Destroy(obj);
            }

            // 延遲重新排列與同步
            StartCoroutine(DelayRearrange());
            StartCoroutine(DelaySyncAfterDestroy());

            CloseConfirmPanel();

            // 寶石獎勵邏輯
            if (usedHand.Count > 0 && usedDice.Count == 0)
                gemReward = 3;
            else if (usedHand.Count > 0 && usedDice.Count > 0)
                gemReward = 2;
            else if (usedHand.Count == 0 && usedDice.Count > 0)
                gemReward = 1;

            int actor = PhotonNetwork.LocalPlayer.ActorNumber;
            playerGems[actor] += gemReward;

            UpdateGemUI();
            photonView.RPC("RPC_UpdateGem", RpcTarget.All, actor, playerGems[actor]);
            ShowGemRewardPanel(gemReward);
            DelayCheckIfAllPlayersNoHandCards();
        }
        else
        {
            Debug.Log("調和失敗");
            CloseConfirmPanel();
            ShowfailPanel();
        }
    }

    [PunRPC]
    public void RPC_RefreshPublicCard(int cardIndex, int poolIndex)
    {
        if (cardIndex < 0 || cardIndex >= publicCards.Count) return;
        if (poolIndex < 0 || poolIndex >= publicCardPool.Count) return;

        Texture2D tex = publicCardPool[poolIndex];

        if (tex != null)
        {
            publicCards[cardIndex].SetCard(tex);
            if (!isWhiteCardExchangeInProgress && PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(DelayCheckWhiteCard());
            }

        }
    }

    [PunRPC]
    public void RPC_DestroyPublicCard(int cardIndex)//牌庫用完時若調色成功就銷毀公牌
    {
        if (cardIndex < 0 || cardIndex >= publicCards.Count) return;

        var card = publicCards[cardIndex];
        if (card != null && card.gameObject != null)
            Destroy(card.gameObject);

        publicCards[cardIndex] = null;
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
        { "紅", new List<List<string>> { new List<string>{ "洋紅", "黃" } } },
        { "綠", new List<List<string>> { new List<string>{ "青", "黃" } } },
        { "藍", new List<List<string>> { new List<string>{ "洋紅", "青" } } },
        { "紫", new List<List<string>> { new List<string>{ "洋紅", "洋紅", "青" }, new List<string>{ "藍", "洋紅" } } },
        { "朱紅", new List<List<string>> { new List<string>{ "洋紅", "洋紅", "黃" }, new List<string>{ "紅", "洋紅" } } },
        { "黃綠", new List<List<string>> { new List<string>{ "青", "黃", "黃" }, new List<string>{ "綠", "黃" } } },
        { "青藍", new List<List<string>> { new List<string>{ "青", "青", "洋紅" }, new List<string>{ "青", "藍" } } },
        { "橙", new List<List<string>> { new List<string>{ "洋紅", "黃", "黃" }, new List<string>{ "紅", "黃" } } },
        { "藍綠", new List<List<string>> { new List<string>{ "黃", "青", "青" }, new List<string>{ "綠", "青" } } },
        { "黑", new List<List<string>> { new List<string>{ "洋紅", "青", "黃" }, new List<string>{ "紅", "青" }, new List<string> { "黃", "藍" }, new List<string> { "洋紅", "綠" }, new List<string> { "紅", "藍" }, new List<string> { "紅", "綠" }, new List<string> { "藍", "綠" } } },
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
        List<HandCardSelect> cards = new List<HandCardSelect>();
        for (int i = 0; i < handCardGenerator.cardContainer.childCount; i++)
        {
            HandCardSelect hcs = handCardGenerator.cardContainer.GetChild(i).GetComponent<HandCardSelect>();
            if (hcs != null) cards.Add(hcs);
        }

        cards.Sort((a, b) => a.cardColorName.CompareTo(b.cardColorName));

        int count = cards.Count;
        if (count == 0) return;

        float cardWidth = cards[0].GetComponent<RectTransform>().sizeDelta.x;
        float containerWidth = handCardGenerator.cardContainer.rect.width;

        float maxSpacing = cardWidth + 20f;  // 正常間距
        float spacing = maxSpacing;

        // 如果卡牌總寬超過容器，壓縮 spacing
        float totalWidth = (count - 1) * spacing;
        if (totalWidth > containerWidth)
        {
            spacing = (containerWidth - 10f) / (count - 1); // 10f 是左右邊距
        }

        float startX = -containerWidth / 2f + cardWidth / 2f; // 從最左邊開始（置中錨點）

        for (int i = 0; i < count; i++)
        {
            float x = startX + i * spacing;
            float y = 0f;

            RectTransform rt = cards[i].GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);
            rt.localRotation = Quaternion.identity;

            cards[i].InitPosition();
        }
    }


    public bool TryConsumeGemForDice(bool isPrimaryColorDice)
    {
        int actor = PhotonNetwork.LocalPlayer.ActorNumber;
        int cost = isPrimaryColorDice ? 1 : 2;

        if (!playerGems.ContainsKey(actor)) return false;
        if (playerGems[actor] < cost)
        {
            Debug.Log("寶石不足！");
            return false;
        }

        playerGems[actor] -= cost;
        UpdateGemUI(); // 顯示更新
        photonView.RPC("RPC_UpdateGem", RpcTarget.All, actor, playerGems[actor]);
        return true;
    }

    public void UpdateGemUI()
    {
        int actor = PhotonNetwork.LocalPlayer.ActorNumber;
        if (playerGems.ContainsKey(actor))
            gemText.text = $"{playerGems[actor]}";
    }

    [PunRPC]
    public void RPC_UpdateGem(int actorNumber, int newGemAmount)
    {
        playerGems[actorNumber] = newGemAmount;

        if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            Debug.Log($"[RPC_UpdateGem] 玩家 {actorNumber} 寶石同步為 {newGemAmount}");
            UpdateGemUI();  // 僅更新自己 UI
        }
    }

    public void ShowGemRewardPanel(int rewardAmount)
    {
        GameObject panelToShow = null;

        switch (rewardAmount)
        {
            case 1: panelToShow = gemRewardPanel_1; break;
            case 2: panelToShow = gemRewardPanel_2; break;
            case 3: panelToShow = gemRewardPanel_3; break;
        }

        if (panelToShow != null)
        {
            StartCoroutine(ShowAndHidePanel(panelToShow, 1f));
        }
    }

    private IEnumerator ShowAndHidePanel(GameObject panel, float duration)
    {
        panel.SetActive(true);
        yield return new WaitForSeconds(duration);
        panel.SetActive(false);
    }

    private IEnumerator DelayRearrange()
    {
        yield return null; // 等待一個 frame，讓 Destroy 完成
        RearrangeHandCards();
    }

    public void DelayCheckIfAllPlayersNoHandCards(float delay = 1.5f)
    {
        //if (PhotonNetwork.IsMasterClient)
            StartCoroutine(DelayCheckCoroutine(delay));
    }

    private IEnumerator DelayCheckCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        CheckIfAllPlayersNoHandCards();
    }

    public void CheckIfAllPlayersNoHandCards()
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            int actor = player.ActorNumber;

            // 只要有一個人沒出局且有手牌，就繼續
            if (!eliminatedPlayers.Contains(actor) && PlayerHasHandCard(actor))
            {
                Debug.Log($"玩家 {actor} 仍有手牌，繼續遊戲");
                return;
            }
        }

        Debug.Log("所有玩家已出局或無手牌，遊戲結束！");
        EndGame();
    }

    // 2. 結算寶石與切換場景
    public void EndGame()
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            int gem = playerGems.ContainsKey(player.ActorNumber) ? playerGems[player.ActorNumber] : 0;
            player.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
        {
            { "finalGem", gem }
        });
        }

        StartCoroutine(LoadEndSceneWithDelay(1f));
    }

    IEnumerator LoadEndSceneWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        PhotonNetwork.LoadLevel("EndScene");
    }

    public bool PlayerHasHandCard(int actorNumber)
    {
        return playerHands.ContainsKey(actorNumber) && playerHands[actorNumber].Count > 0;
    }
}
