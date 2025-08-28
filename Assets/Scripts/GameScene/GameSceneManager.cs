using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using static HandCardSelect;
using TMPro;
using System.IO;
using System;
using System.Text;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif


public class GameSceneManager : MonoBehaviourPunCallbacks
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void DownloadCSV(string filename, string content);
#endif
    // 單例 & 狀態管理
    public static GameSceneManager Instance;
    private Coroutine currentExchangeCoroutine;
    public bool isWhiteCardExchangeInProgress = false;
    private bool hasOpenedChooseColorPanel = false;
    private bool firstFinishAwarded = false;
    private bool hasSynced = false;
    private int gemSpentTotal = 0;
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

    //玩家資料
    private Dictionary<int, int> playerGems = new Dictionary<int, int>();
    public Dictionary<int, int> discardCounts = new Dictionary<int, int>(); // 棄牌次數
    public HashSet<int> eliminatedPlayers = new HashSet<int>(); // 出局玩家
    private Dictionary<int, List<string>> playerHands = new Dictionary<int, List<string>>();
    private Dictionary<int, string> pendingTransfers = new Dictionary<int, string>();
    
    //手牌與選取資料（邏輯用）
    private List<string> selectedHandColors = new List<string>();
    private List<HandCardSelect> selectedHandCards = new List<HandCardSelect>();
    private List<string> selectedDiceColors = new List<string>();
    private PublicCardSelect selectedPublicCard = null;
    private HandCardSelect pendingDiscardCard = null;

    // 卡牌設定
    [Header("卡牌設定")]
    public GameObject publicCardPrefab;
    public RectTransform publiccardContainer;
    public Texture2D[] cards;
    public Texture2D blackCardTexture; // 黑色卡
    public Image yellow_retangular;
    public HandCardGenerator handCardGenerator;
    private List<Texture2D> publicCardPool = new List<Texture2D>();
    private int publicCardIndex = 0;  // 控制從 publicCardPool 取第幾張
    private List<PublicCardSelect> publicCards = new List<PublicCardSelect>();

    // 寶石與獎勵
    [Header("寶石獎勵面板")]
    public TMP_Text gemText;
    public GameObject gemRewardPanel_1;
    public GameObject gemRewardPanel_2;
    public GameObject gemRewardPanel_3;

    // 互動視窗
    [Header("互動視窗")]
    public GameObject confirmPanel;
    public Button confirmButton;
    public Button cancelButton;

    public GameObject failPanel;
    public Button confirmfailButton;
    public Button cancelfailButton;

    public GameObject discardConfirmPanel;
    public Button discardYesButton;
    public Button discardNoButton;

    public GameObject discardHintPanel;

    public GameObject whiteCardHintTextPanel;
    public GameObject exchangeCardTextPanel;
    private List<int> whiteCardIndicesToDestroyAfterTransfer = new List<int>();

    // 敵方 UI
    [Header("敵方 UI")]
    [SerializeField] private GameObject cardBackPrefab;
    [SerializeField] private Transform enemyZone_Top;
    [SerializeField] private Transform enemyZone_Left;
    [SerializeField] private Transform enemyZone_Right;

    // 即時提示（可選功能）
    [Header("可選功能設定")]
    public bool enableRealtimeTips = false;
    public GameObject colorCirclePrefab;//動畫圓形prefab
    public Transform canvasTransform;

    [Header("提示文字 UI")]
    public GameObject TipPanel;
    public TMP_Text hoverTipText;

    // 顏色選擇 UI
    [Header("顏色選擇面板")]
    public GameObject chooseColorPanel;
    public Button chooseMagentaButton;
    public Button chooseYellowButton;
    public Button chooseCyanButton;

    // 其他 UI 控制
    public GameObject darkBackground;

    [Header("出局相關 UI")]
    public GameObject eliminationOverlay; // 黑幕 UI

    /*
    -------------------------------------------------------------------------------------------------------------------------------------
                                                    Unity 生命週期
    -------------------------------------------------------------------------------------------------------------------------------------
    */
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        yellow_retangular.gameObject.SetActive(false);
        confirmPanel.SetActive(false);
        confirmButton.onClick.AddListener(() => StartCoroutine(OnConfirmHarmonizeCoroutine()));
        cancelButton.onClick.AddListener(CloseConfirmPanel);
        failPanel.SetActive(false);
        confirmfailButton.onClick.AddListener(GiveupCard);
        cancelfailButton.onClick.AddListener(ClosefailPanel);

        handCardGenerator.StartGeneratingCards();
        eliminatedPlayers.Clear();
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
            playerTurnDurations[player.ActorNumber] = new List<float>();
            playerTurnHintClicks[player.ActorNumber] = new List<int>();
            currentTurnHintClicks[player.ActorNumber] = 0;
            playerTurnGemSpent[player.ActorNumber] = new List<int>();
            playerTurnDiscard[player.ActorNumber] = new List<int>();
            playerTurnFail[player.ActorNumber] = new List<int>();
            playerTurnGemGained[player.ActorNumber] = new List<int>();
            currentTurnGemGained[player.ActorNumber] = 0;
            currentTurnGemSpent[player.ActorNumber] = 0;
            currentTurnDiscardCnt[player.ActorNumber] = 0;
            currentTurnFailCnt[player.ActorNumber] = 0;

            failCounts[player.ActorNumber] = 0;
            UpdateGemUI();
        }

        if (AudioManager.Instance == null)
        {
            Instantiate(Resources.Load<GameObject>("AudioManager"));
        }
    }

    /*
    -------------------------------------------------------------------------------------------------------------------------------------
                                                   公牌池初始化與生成邏輯
    -------------------------------------------------------------------------------------------------------------------------------------
    */
    private void InitializePublicCardPool()
    {
        publicCardPool.Clear();

        string[] secondaryColorNames = { "紅", "綠", "藍" };
        string[] tertiaryColorNames = { "紫", "橙", "青綠", "黃綠", "朱紅", "藍綠","青藍" };

        foreach (var name in secondaryColorNames)
            AddToPoolByName(name, 3);
        foreach (var name in tertiaryColorNames)
            AddToPoolByName(name, 3);

        AddToPoolByName("白", 4);
        AddToPoolByName("黑", 4);

        // 洗牌
        for (int i = 0; i < publicCardPool.Count; i++)
        {
            var temp = publicCardPool[i];
            int randIndex = UnityEngine.Random.Range(i, publicCardPool.Count);
            publicCardPool[i] = publicCardPool[randIndex];
            publicCardPool[randIndex] = temp;
        }

        // 廣播整個牌池給所有人
        string[] cardNames = publicCardPool.ConvertAll(tex => tex.name).ToArray();
        photonView.RPC("RPC_SyncPublicCardPool", RpcTarget.Others, cardNames);

        // 只有主持人負責決定初始牌庫索引
        if (PhotonNetwork.IsMasterClient)
        {
            publicCardIndex = 0;
        }
    }
    private void AddToPoolByName(string name, int count)
    {
        Texture2D tex = System.Array.Find(cards, t => t.name == name);
        if (tex == null) return;

        for (int i = 0; i < count; i++)
            publicCardPool.Add(tex);
    }

    public IEnumerator GeneratePublicCards(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 非主持人直接結束，不做任何事
        if (!PhotonNetwork.IsMasterClient)
            yield break;

        int cardCount = 4;
        string[] selectedNames = new string[cardCount];

        for (int i = 0; i < cardCount; i++)
        {
            if (publicCardIndex < publicCardPool.Count)
            {
                selectedNames[i] = publicCardPool[publicCardIndex].name;
                publicCardIndex++;
            }
        }
        publicCardIndex = publicCardIndex - 1;
        // 廣播公牌顏色給所有人
        photonView.RPC("RPC_GeneratePublicCards_ByNames", RpcTarget.All, selectedNames);

        // 同步牌庫索引給所有人
        photonView.RPC("RPC_SyncPublicCardIndex", RpcTarget.All, publicCardIndex);
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
    private void UpdatePublicCardIndex(int newIndex)//更新牌庫目前取到第幾張
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("非主持人不能更新 publicCardIndex！");
            return;
        }

        publicCardIndex = newIndex;
        photonView.RPC("RPC_SyncPublicCardIndex", RpcTarget.All, newIndex);
        Debug.Log($"MasterClient 更新並同步 publicCardIndex: {newIndex}");
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
    [PunRPC]
    public void RPC_SyncPublicCardIndex(int newIndex)
    {
        publicCardIndex = newIndex;
        Debug.Log($"已同步 publicCardIndex: {newIndex}");
    }
    [PunRPC]
    public void RPC_GeneratePublicCards_ByNames(string[] colorNames)
    {
        StartCoroutine(GeneratePublicCardsFromNames(colorNames));
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
        if (PhotonNetwork.IsMasterClient)
        {
            UpdatePublicCardIndex(poolIndex);
        }
    }

    [PunRPC]
    public void RPC_DestroyPublicCard(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= publicCards.Count) return;

        if (cardIndex + 1 >= publicCardPool.Count)
        {
            Debug.Log("牌庫已用完，改為顯示黑色卡");

            // 顯示黑色卡代替
            Texture2D blackTex = blackCardTexture;

            if (publicCards[cardIndex] != null)
                publicCards[cardIndex].SetCard(blackTex);

            return;
        }
        var card = publicCards[cardIndex];
        if (card != null && card.gameObject != null)
            Destroy(card.gameObject);

        publicCards[cardIndex] = null;
    }

    /*
    -------------------------------------------------------------------------------------------------------------------------------------
                                                   白卡處理與交換
    -------------------------------------------------------------------------------------------------------------------------------------
    */

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
        whiteCardHintTextPanel.SetActive(false);
        photonView.RPC("RPC_HideWhiteCardHintText", RpcTarget.All);
        photonView.RPC("RPC_TriggerCardTransfer", RpcTarget.All);

        if (!PhotonNetwork.IsMasterClient) yield break;

        foreach (int i in indices)
        {
            if (publicCardIndex < publicCardPool.Count)
            {
                int poolIndexToUse = publicCardIndex;
                //UpdatePublicCardIndex(poolIndexToUse+1);
                photonView.RPC("RPC_RefreshPublicCard", RpcTarget.All, i, poolIndexToUse+1);
            }
            else
            {
                Debug.LogWarning("已無可用的公牌卡，刷新中止");
            }

        }
    }

    [PunRPC]
    public void RPC_ShowWhiteCardHintText()
    {
        whiteCardHintTextPanel.SetActive(true);
    }

    [PunRPC]
    public void RPC_HideWhiteCardHintText()
    {
        whiteCardHintTextPanel.SetActive(false);
    }

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
        if (myHandCards.Length == 0 || eliminatedPlayers.Contains(PhotonNetwork.LocalPlayer.ActorNumber))
        {
            Debug.LogWarning($"玩家 {PhotonNetwork.LocalPlayer.ActorNumber} 沒有卡可提交，跳過");
            exchangeCardTextPanel.SetActive(true);
            exchangeCardTextPanel.GetComponentInChildren<TextMeshProUGUI>().text = "跳過本次交換";
            StartCoroutine(ShowExchangeCardTextSequence());
            photonView.RPC("RPC_SkipTransfer", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
            yield break;
        }

        yield return StartCoroutine(AnimateCardSelectionCoroutine(myHandCards));
    }

    private IEnumerator AnimateCardSelectionCoroutine(HandCardSelect[] cards) //選牌特效
    {
        int totalSteps = cards.Length * 2 + UnityEngine.Random.Range(0, cards.Length);
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
            exchangeCardTextPanel.SetActive(true);
            exchangeCardTextPanel.GetComponentInChildren<TextMeshProUGUI>().text = $"您即將交換的手牌是：{color}";
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

    private IEnumerator ShowExchangeCardTextSequence()
    {
        exchangeCardTextPanel.SetActive(true); 
        yield return new WaitForSeconds(1.75f);
        exchangeCardTextPanel.GetComponentInChildren<TextMeshProUGUI>().text = " ";
        exchangeCardTextPanel.SetActive(false);
        currentExchangeCoroutine = null;
    }
    [PunRPC]
    public void RPC_ShowExchangeCancelledMessage()
    {
        if (currentExchangeCoroutine != null)
            StopCoroutine(currentExchangeCoroutine);
        exchangeCardTextPanel.SetActive(true);
        exchangeCardTextPanel.GetComponentInChildren<TextMeshProUGUI>().text = "交換人數不足,取消交換";
        currentExchangeCoroutine = StartCoroutine(ShowExchangeCardTextSequence());
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
        exchangeCardTextPanel.SetActive(true);
        exchangeCardTextPanel.GetComponentInChildren<TextMeshProUGUI>().text = $"收到手牌：{colorName}";
        StartCoroutine(ShowExchangeCardTextSequence());
        Debug.Log($"玩家 {PhotonNetwork.LocalPlayer.ActorNumber} 成功接收到卡：{colorName}");

        StartCoroutine(DelayRearrange());
        SyncMyHandCardsToSystem();
    }

    /*
    -------------------------------------------------------------------------------------------------------------------------------------
                                                    玩家同步相關
    -------------------------------------------------------------------------------------------------------------------------------------
    */

    public void TrySyncOnceAfterGenerate()
    {
        if (hasSynced) return;
        hasSynced = true;
        SyncMyHandCardsToSystem();
    }

    public void SyncMyHandCardsToSystem()
    {
        int actor = PhotonNetwork.LocalPlayer.ActorNumber;
        HandCardSelect[] cards = UnityEngine.Object.FindObjectsByType<HandCardSelect>(FindObjectsSortMode.None);
        List<string> colorList = new List<string>();
        foreach (var c in cards) colorList.Add(c.cardColorName);

        photonView.RPC("RPC_SyncHandCards", RpcTarget.All, actor, colorList.ToArray());
    }

    [PunRPC]
    public void RPC_SyncHandCards(int actorNumber, string[] colorArray)
    {
        playerHands[actorNumber] = new List<string>(colorArray);
        UpdateEnemyHandUI();
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

    /*
    -------------------------------------------------------------------------------------------------------------------------------------
                                                    調和判斷與執行
    -------------------------------------------------------------------------------------------------------------------------------------
    */

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

    IEnumerator OnConfirmHarmonizeCoroutine()
    {
        if (!TurnManager.IsMyTurn || selectedPublicCard == null)
        {
            Debug.Log("非回合或未選擇公牌");
            yield break;
        }

        string targetColor = selectedPublicCard.cardColorName;

        bool success = CanHarmonize(targetColor, selectedHandColors, selectedDiceColors, out var usedHand, out var usedDice);

        // 播放動畫（成功或失敗都播）
        if (enableRealtimeTips && colorMixingRules.ContainsKey(targetColor))
        {
            CloseConfirmPanel();
            List<string> recipe = colorMixingRules[targetColor][0];
            yield return StartCoroutine(PlayMixCoroutine(recipe, targetColor));
        }

        // 混色動畫播完後才執行結果邏輯
        if (success)
        {
            yield return StartCoroutine(HandleHarmonizeSuccess(targetColor, usedHand, usedDice));
        }
        else
        {
            int actor = PhotonNetwork.LocalPlayer.ActorNumber;
            photonView.RPC(nameof(RPC_AddCurrentTurnFail), RpcTarget.MasterClient, actor, 1);

            if (!failCounts.ContainsKey(actor)) failCounts[actor] = 0;
            failCounts[actor]++;
            CloseConfirmPanel();
            ShowfailPanel();
        }
    }

    [PunRPC]
    void RPC_AddCurrentTurnFail(int actorNumber, int delta)
    {
        if (!currentTurnFailCnt.ContainsKey(actorNumber))
            currentTurnFailCnt[actorNumber] = 0;
        currentTurnFailCnt[actorNumber] += Mathf.Max(0, delta);
    }

    private IEnumerator HandleHarmonizeSuccess(string targetColor, List<string> usedHand, List<string> usedDice)
    {
        int cardIndex = publicCards.IndexOf(selectedPublicCard);
        if (cardIndex == -1) yield break;

        if (publicCardIndex < publicCardPool.Count - 1)
        {
            int poolIndexToUse = publicCardIndex;
            photonView.RPC("RPC_RefreshPublicCard", RpcTarget.All, cardIndex, poolIndexToUse + 1);
        }
        else
        {
            photonView.RPC("RPC_DestroyPublicCard", RpcTarget.All, cardIndex);
        }

        List<GameObject> cardsToDestroy = new List<GameObject>();
        foreach (var card in selectedHandCards)
        {
            if (usedHand.Contains(card.cardColorName) && card != null && card.gameObject != null)
                cardsToDestroy.Add(card.gameObject);
        }

        selectedHandCards.Clear();
        selectedHandColors.Clear();
        selectedDiceColors.Clear();
        selectedPublicCard = null;

        if (usedDice.Exists(color => resultDice.currentlySelectedManual != null &&
                                     resultDice.currentlySelectedManual.cardColorName == color))
        {
            resultDice.currentlySelectedManual.SetSelected(false);
        }

        ResetGemSpent();
        DiceManager.Instance.ResetDiceUI();

        foreach (var obj in cardsToDestroy)
        {
            if (obj != null)
                Destroy(obj);
        }

        yield return StartCoroutine(DelayRearrange());
        yield return StartCoroutine(DelaySyncAfterDestroy());

        CloseConfirmPanel();

        int gemReward = 0;
        if (usedHand.Count > 0 && usedDice.Count == 0)
            gemReward = 5;
        else if (usedHand.Count > 0 && usedDice.Count > 0)
            gemReward = 4;
        else if (usedHand.Count == 0 && usedDice.Count > 0)
            gemReward = 3;

        int actor = PhotonNetwork.LocalPlayer.ActorNumber;
        playerGems[actor] += gemReward;
        UpdateGemUI();
        photonView.RPC("RPC_UpdateGem", RpcTarget.All, actor, playerGems[actor]);
        ShowGemRewardPanel(gemReward);

        photonView.RPC(nameof(RPC_AddCurrentTurnGemGained), RpcTarget.MasterClient, actor, gemReward);

        DelayCheckIfAllPlayersNoHandCards();
        TurnManager.Instance.CompleteMyTurn();
    }

    public string GetMixingTip(string colorName)
    {
        List<List<string>> recipes = colorMixingRules[colorName];
        var mainRecipe = recipes[0];
        string formatted = string.Join(" + ", mainRecipe);
        return $"調色原則:\t{formatted}";
    }

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
    private IEnumerator PlayMixCoroutine(List<string> componentNames, string resultName)
    {
        List<GameObject> circles = new List<GameObject>();
        Vector3[] startPositions;

        if (componentNames.Count == 2)
            startPositions = new Vector3[] { new Vector3(-200, 0, 0), new Vector3(200, 0, 0) };
        else
            startPositions = new Vector3[] { new Vector3(-200, -100, 0), new Vector3(200, -100, 0), new Vector3(0, 200, 0) };

        // 生成三原色圖片
        for (int i = 0; i < componentNames.Count; i++)
        {
            var circle = Instantiate(colorCirclePrefab, canvasTransform);
            var image = circle.GetComponent<Image>();

            Sprite sprite = Resources.Load<Sprite>($"colorCircle/{componentNames[i]}");
            if (sprite != null) image.sprite = sprite;

            Color c = Color.white;
            c.a = 0.8f; // 初始透明度
            image.color = c;

            var rt = circle.GetComponent<RectTransform>();
            rt.anchoredPosition = startPositions[i];
            rt.localScale = Vector3.one;
            circles.Add(circle);
        }

        // 移動到中心
        float t = 0f;
        float moveDuration = 2f;
        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / moveDuration);
            for (int i = 0; i < circles.Count; i++)
                circles[i].GetComponent<RectTransform>().anchoredPosition = Vector3.Lerp(startPositions[i], Vector3.zero, lerp);
            yield return null;
        }

        // 生成結果圖片，與三張圖疊在一起，透明度 0
        var resultCircle = Instantiate(colorCirclePrefab, canvasTransform);
        RectTransform rtResult = resultCircle.GetComponent<RectTransform>();
        rtResult.anchoredPosition = Vector3.zero;
        rtResult.localScale = Vector3.one;

        Sprite resultSprite = Resources.Load<Sprite>($"colorCircle/{resultName}");
        var resultImage = resultCircle.GetComponent<Image>();
        if (resultSprite != null)
            resultImage.sprite = resultSprite;
        else
            resultImage.color = Color.white;

        // 初始結果圖片透明
        Color resultCol = resultImage.color;
        resultCol.a = 0f;
        resultImage.color = resultCol;

        // 同時淡出三張圖 & 淡入結果圖
        float blendDuration = 0.5f;
        float blendT = 0f;
        while (blendT < blendDuration)
        {
            blendT += Time.deltaTime;
            float alphaOut = Mathf.Lerp(0.8f, 0f, blendT / blendDuration);
            float alphaIn = Mathf.Lerp(0f, 1f, blendT / blendDuration);

            // 三張圖淡出
            foreach (var c in circles)
            {
                var img = c.GetComponent<Image>();
                Color col = img.color;
                col.a = alphaOut;
                img.color = col;
            }

            // 結果圖淡入
            Color colResult = resultImage.color;
            colResult.a = alphaIn;
            resultImage.color = colResult;

            yield return null;
        }

        // 移除三張圖，留下結果圖
        foreach (var c in circles)
            Destroy(c);

        yield return new WaitForSeconds(2f);
        Destroy(resultCircle);
    }

    /*
    public void HighlightMatchHandCards(string targetColor)
    {
        // 1. 收集手牌資料
        HandCardSelect[] handCards = handCardGenerator.cardContainer.GetComponentsInChildren<HandCardSelect>();
        List<string> handColors = new List<string>();
        Dictionary<string, List<HandCardSelect>> colorToHandCards = new Dictionary<string, List<HandCardSelect>>();

        foreach (var card in handCards)
        {
            handColors.Add(card.cardColorName);
            if (!colorToHandCards.ContainsKey(card.cardColorName))
                colorToHandCards[card.cardColorName] = new List<HandCardSelect>();
            colorToHandCards[card.cardColorName].Add(card);
        }

        // 2. 收集所有 resultDice（包含骰子結果與自選三原色）
        List<resultDice> allDiceCards = new List<resultDice>();

        // 加入擲骰結果區域中的骰子
        allDiceCards.AddRange(DiceManager.Instance.resultDiceContainer.GetComponentsInChildren<resultDice>());

        // 加入場上所有自選三原色（SingleManual 模式）
        resultDice[] allManualDice = GameObject.FindObjectsByType<resultDice>(FindObjectsSortMode.None);
        foreach (var dice in allManualDice)
        {
            if (dice.selectMode == resultDice.DiceSelectMode.SingleManual && !allDiceCards.Contains(dice))
                allDiceCards.Add(dice);
        }

        // 建立骰子顏色對映
        List<string> diceColors = new List<string>();
        Dictionary<string, List<resultDice>> colorToDiceCards = new Dictionary<string, List<resultDice>>();

        foreach (var dice in allDiceCards)
        {
            diceColors.Add(dice.cardColorName);
            if (!colorToDiceCards.ContainsKey(dice.cardColorName))
                colorToDiceCards[dice.cardColorName] = new List<resultDice>();
            colorToDiceCards[dice.cardColorName].Add(dice);
        }

        // 3. 嘗試找出第一組可以合成 targetColor 的配方
        if (!colorMixingRules.ContainsKey(targetColor)) return;

        foreach (var recipe in colorMixingRules[targetColor])
        {
            List<string> tempHand = new List<string>(handColors);
            List<string> tempDice = new List<string>(diceColors);
            List<HandCardSelect> matchedHand = new List<HandCardSelect>();
            List<resultDice> matchedDice = new List<resultDice>();
            bool matched = true;

            foreach (var color in recipe)
            {
                // 先嘗試從手牌中找
                if (tempHand.Contains(color) && colorToHandCards.ContainsKey(color))
                {
                    var card = colorToHandCards[color].Find(c => !matchedHand.Contains(c));
                    if (card != null)
                    {
                        matchedHand.Add(card);
                        tempHand.Remove(color);
                        continue;
                    }
                }

                // 再嘗試從骰子中找
                if (tempDice.Contains(color) && colorToDiceCards.ContainsKey(color))
                {
                    var dice = colorToDiceCards[color].Find(d => !matchedDice.Contains(d));
                    if (dice != null)
                    {
                        matchedDice.Add(dice);
                        tempDice.Remove(color);
                        continue;
                    }
                }

                // 找不到這個顏色
                matched = false;
                break;
            }

            if (matched)
            {
                // 套用 hover 效果
                foreach (var card in matchedHand)
                    card.SetHoverVisual(true);

                foreach (var dice in matchedDice)
                    dice.SetHoverVisual(true);

                return; // 成功找到一組 recipe 就結束
            }
        }
    }
    */

    public void ClearAllHandCardHover()
    {
        HandCardSelect[] handCards = handCardGenerator.cardContainer.GetComponentsInChildren<HandCardSelect>();
        foreach (var card in handCards)
        {
            if (!card.isCardSelected)
                card.SetHoverVisual(false);
        }

        resultDice[] allDice = GameObject.FindObjectsByType<resultDice>(FindObjectsSortMode.None);
        foreach (var dice in allDice)
        {
            if (!dice.isCardSelected)
                dice.SetHoverVisual(false);
        }
    }

    /*
    -------------------------------------------------------------------------------------------------------------------------------------
                                                    棄牌與懲罰
    -------------------------------------------------------------------------------------------------------------------------------------
    */

    public void GiveupCard()//棄牌
    {
        ClosefailPanel();
        EnableDiscardSelection();
        //提示需選擇一張卡牌
        if (discardHintPanel != null)
            discardHintPanel.SetActive(true);

        exchangeCardTextPanel.SetActive(true);
        exchangeCardTextPanel.GetComponentInChildren<TextMeshProUGUI>().text = "請選擇一張手牌丟棄";
        StartCoroutine(ShowExchangeCardTextSequence());
        DiceManager.Instance.DeselectResultDiceVisual();
        selectedHandColors.Clear();
    }

    public void EnableDiscardSelection()
    {
        Debug.Log("請選擇要棄掉的手牌");
        foreach (var card in UnityEngine.Object.FindObjectsByType<HandCardSelect>(FindObjectsSortMode.None))
        {
            //Debug.Log("設定卡牌為 Discard 模式: " + card.cardColorName);
            card.SetMode(HandCardMode.DiscardSelection, OnHandCardChosenToDiscard);
        }
    }

    public void OnHandCardChosenToDiscard(HandCardSelect card)
    {
        pendingDiscardCard = card;
        if (discardHintPanel != null)
            discardHintPanel.SetActive(false);
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
        int actor = PhotonNetwork.LocalPlayer.ActorNumber;
        
        if (pendingDiscardCard != null)
        {
            Destroy(pendingDiscardCard.gameObject);
            RearrangeHandCards();
        }

        // 新增棄牌次數
        discardCounts[actor]++;
        Debug.Log($"玩家 {actor} 棄牌第 {discardCounts[actor]} 次");
        photonView.RPC(nameof(RPC_AddCurrentTurnDiscard), RpcTarget.MasterClient, actor, 1);
        // 若已達3次，標記為出局
        if (discardCounts[actor] >= 3)
        {
            // 加入本地列表
            if (!eliminatedPlayers.Contains(actor))
                EliminatePlayer(actor);

            // LocalPlayer 先顯示自己的出局提示
            if (actor == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                StopCoroutine(ShowExchangeCardTextSequence());
                exchangeCardTextPanel.SetActive(true);
                exchangeCardTextPanel.GetComponentInChildren<TextMeshProUGUI>().text = "您已出局！";
                StartCoroutine(ShowExchangeCardTextSequence());
            }
            StartCoroutine(DelayNotifyOthersEliminated(actor, 2f));
        }

        // 發新牌 + 同步
        Texture2D[] primaryColors = handCardGenerator.primaryColors;
        Texture2D randomPrimary = primaryColors[UnityEngine.Random.Range(0, primaryColors.Length)];
        GameObject newCard = Instantiate(handCardGenerator.handCardPrefab, handCardGenerator.cardContainer);
        RawImage raw = newCard.GetComponent<RawImage>();
        raw.texture = randomPrimary;

        HandCardSelect hcs = newCard.GetComponent<HandCardSelect>();
        hcs.cardColorName = randomPrimary.name;
        hcs.SetMode(HandCardMode.Normal);
        hcs.InitPosition();

        StartCoroutine(DelayRearrange());
        SyncMyHandCardsToSystem();
        //獲得三原色提示
        exchangeCardTextPanel.SetActive(true);
        exchangeCardTextPanel.GetComponentInChildren<TextMeshProUGUI>().text = $"獲得三原色卡：{randomPrimary.name}";
        StartCoroutine(ShowExchangeCardTextSequence());

        pendingDiscardCard = null;
        HideDiscardConfirmPanel();
        ResetHandCardMode();
        DiceManager.Instance.ResetDiceUI();
        ResetGemSpent();
        DelayCheckIfAllPlayersNoHandCards();
        TurnManager.Instance.CompleteMyTurn();
    }

    [PunRPC]
    void RPC_AddCurrentTurnDiscard(int actorNumber, int delta)
    {
        if (!currentTurnDiscardCnt.ContainsKey(actorNumber))
            currentTurnDiscardCnt[actorNumber] = 0;
        currentTurnDiscardCnt[actorNumber] += Mathf.Max(0, delta);
    }

    private IEnumerator DelayNotifyOthersEliminated(int actorNumber, float delay)
    {
        yield return new WaitForSeconds(delay);
        photonView.RPC("RPC_NotifyPlayerEliminated", RpcTarget.All, actorNumber);
    }
    [PunRPC]
    void RPC_NotifyPlayerEliminated(int actorNumber)
    {
        StopCoroutine(ShowExchangeCardTextSequence());

        string displayText = "";

        if (PhotonNetwork.LocalPlayer.ActorNumber == actorNumber)
        {
            displayText = "您已出局！";

            if (eliminationOverlay != null)
                eliminationOverlay.SetActive(true);
        }
        else
        {
            var player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            string name = player != null ? player.NickName : actorNumber.ToString();
            displayText = $"玩家 {name} 因為棄牌三次出局";
        }

        exchangeCardTextPanel.SetActive(true);
        exchangeCardTextPanel.GetComponentInChildren<TextMeshProUGUI>().text = displayText;
        StartCoroutine(ShowExchangeCardTextSequence());

        Debug.Log(displayText);
    }
    public void EliminatePlayer(int actorNumber)
    {
        if (!eliminatedPlayers.Contains(actorNumber))
        {
            eliminatedPlayers.Add(actorNumber);

            // 同步給所有其他玩家
            photonView.RPC("RPC_EliminatePlayer", RpcTarget.Others, actorNumber);

            Debug.Log($"[EliminatePlayer] 玩家 {actorNumber} 出局，已同步到其他玩家。");
            //PrintEliminatedPlayers();
        }
    }

    [PunRPC]
    void RPC_EliminatePlayer(int actorNumber)
    {
        if (!eliminatedPlayers.Contains(actorNumber))
        {
            eliminatedPlayers.Add(actorNumber);
            Debug.Log($"[RPC_EliminatePlayer] 收到同步 → 玩家 {actorNumber} 出局。");
            //PrintEliminatedPlayers();
        }
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
        foreach (var card in UnityEngine.Object.FindObjectsByType<HandCardSelect>(FindObjectsSortMode.None))
        {
            card.SetMode(HandCardMode.Normal);
        }
    }

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

    /*
    -------------------------------------------------------------------------------------------------------------------------------------
                                                        UI控制
    -------------------------------------------------------------------------------------------------------------------------------------
    */

    public void ShowConfirmPanel() => confirmPanel.SetActive(true);
    public void CloseConfirmPanel() => confirmPanel.SetActive(false);
    public void ShowfailPanel() => failPanel.SetActive(true);
    public void ClosefailPanel() => failPanel.SetActive(false);

    public void ShowGemRewardPanel(int rewardAmount)
    {
        GameObject panelToShow = null;

        switch (rewardAmount)
        {
            case 3: panelToShow = gemRewardPanel_1; break;
            case 4: panelToShow = gemRewardPanel_2; break;
            case 5: panelToShow = gemRewardPanel_3; break;
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

    public void UpdateGemUI()
    {
        int actor = PhotonNetwork.LocalPlayer.ActorNumber;
        if (playerGems.ContainsKey(actor))
            gemText.text = $"{playerGems[actor]}";
    }

    /*
    -------------------------------------------------------------------------------------------------------------------------------------
                                                        手牌操作與更新
    -------------------------------------------------------------------------------------------------------------------------------------
    */

    public Texture2D GetHandCardTextureByName(string colorName) //從手牌堆中尋找對應顏色
    {
        foreach (var tex in handCardGenerator.primaryColors)
            if (tex.name == colorName) return tex;

        foreach (var tex in handCardGenerator.secondaryColors)
            if (tex.name == colorName) return tex;

        return null;
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

        float maxSpacing = cardWidth;  // 正常間距
        float spacing = maxSpacing;

        // 如果卡牌總寬超過容器，壓縮 spacing
        float totalWidth = (count - 1) * spacing;
        if (totalWidth > containerWidth)
        {
            spacing = (containerWidth - 10f) / (count - 1);
        }

        float startX = -containerWidth / 2f + cardWidth / 2f - 40f;

        for (int i = 0; i < count; i++)
        {
            float x = startX + i * spacing;
            float y = -80f;

            RectTransform rt = cards[i].GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);
            rt.localRotation = Quaternion.identity;

            cards[i].InitPosition();
        }
    }

    private IEnumerator DelayRearrange()
    {
        yield return null; // 等待一個 frame，讓 Destroy 完成
        RearrangeHandCards();
    }

    private IEnumerator DelaySyncAfterDestroy()
    {
        yield return new WaitForEndOfFrame();
        SyncMyHandCardsToSystem();
    }

    /*
    -------------------------------------------------------------------------------------------------------------------------------------
                                                        寶石與骰子操作
    -------------------------------------------------------------------------------------------------------------------------------------
    */

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
        gemSpentTotal += cost;

        UpdateGemUI(); // 顯示更新
        photonView.RPC("RPC_UpdateGem", RpcTarget.All, actor, playerGems[actor]);

        photonView.RPC(nameof(RPC_AddCurrentTurnGemSpent), RpcTarget.MasterClient, actor, cost);

        if (!hasOpenedChooseColorPanel && gemSpentTotal >= 3)
        {
            hasOpenedChooseColorPanel = true;
            HideDiceButtons();
            ShowChooseColorPanel(); //自選三原色

        }
        return true;
    }

    [PunRPC]
    void RPC_AddCurrentTurnGemSpent(int actorNumber, int delta)
    {
        if (!currentTurnGemSpent.ContainsKey(actorNumber))
            currentTurnGemSpent[actorNumber] = 0;
        currentTurnGemSpent[actorNumber] += Mathf.Max(0, delta);
    }

    void ShowChooseColorPanel()
    {
        chooseColorPanel.SetActive(true);
    }

    public void HideDiceButtons()
    {
        //if (DiceManager.Instance != null)
        DiceManager.Instance.HideDiceButtons();
    }

    public void ResetGemSpent()
    {
        gemSpentTotal = 0;
        hasOpenedChooseColorPanel = false;

        //if (chooseColorPanel != null)
        chooseColorPanel.SetActive(false);
        resultDice.currentlySelectedManual = null;
        if (DiceManager.Instance != null)
        {
            DiceManager.Instance.mainDiceButton.SetActive(true);
            DiceManager.Instance.diceChoicePanel.SetActive(false);
        }
    }

    public bool CanStillRollDice()
    {
        return gemSpentTotal < 3;
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

    /*
    -------------------------------------------------------------------------------------------------------------------------------------
                                                        結算與場景切換
    -------------------------------------------------------------------------------------------------------------------------------------
    */

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
        bool anyHasCards = false;

        foreach (var player in PhotonNetwork.PlayerList)
        {
            int actor = player.ActorNumber;

            //跳過出局玩家
            if (eliminatedPlayers.Contains(actor))
                continue;

            bool hasCards = PlayerHasHandCard(actor);

            if (hasCards == false  && firstFinishAwarded == false)
            {
                 AwardFirstFinishBonus(actor); 
            }

            // 檢查是否還有人有手牌
            if (hasCards)
            {
                anyHasCards = true;
            }
        }

        if (anyHasCards)
        {
            Debug.Log("仍有玩家有手牌，繼續遊戲");
            return;
        }

        Debug.Log("所有未出局玩家皆無手牌，遊戲結束！");

        if (PhotonNetwork.IsMasterClient)
        {
            EndGame();
        }
        else
        {
            photonView.RPC("RPC_RequestEndGame", RpcTarget.MasterClient);
        }
    }

    //每個玩家手牌清空時呼叫
    /*private void CheckPlayerHandEmpty(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_PlayerHandEmpty", RpcTarget.MasterClient, actorNumber);
        }
        else
        {
            RPC_PlayerHandEmpty(actorNumber);
        }
    }

    [PunRPC]
    private void RPC_PlayerHandEmpty(int actorNumber)
    {
        //第一個結束的玩家的獎勵
        if (!firstFinishAwarded)
        {
            firstFinishAwarded = true;
            AwardFirstFinishBonus(actorNumber);
        }
    }
    */
    private void AwardFirstFinishBonus(int actorNumber)
    {
        photonView.RPC("RPC_AwardFirstFinishBonus", RpcTarget.All, actorNumber);
    }

    [PunRPC]
    private void RPC_AwardFirstFinishBonus(int actorNumber)
    {
        if (firstFinishAwarded == true) return;
        firstFinishAwarded = true;
        int currentGem = playerGems.ContainsKey(actorNumber) ? playerGems[actorNumber] : 0;
        playerGems[actorNumber] = currentGem + 3;

        Debug.Log($"玩家 {actorNumber} 第一個完成，獲得額外3顆寶石");

        var player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        if (player != null)
        {
            player.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
        {
            { "gem", playerGems[actorNumber] }
        });
        }

        //UpdateGemUI();
        photonView.RPC(nameof(RPC_AddCurrentTurnGemGained), RpcTarget.MasterClient, actorNumber, 3);
        photonView.RPC("RPC_UpdateGem", RpcTarget.All, actorNumber, playerGems[actorNumber]);

    }

    public bool PlayerHasHandCard(int actorNumber)
    {
        return playerHands.ContainsKey(actorNumber) && playerHands[actorNumber].Count > 0;
    }

    public void EndGame()
    {

        PhotonView.Get(GameSceneManager.Instance).RPC(nameof(RPC_BeginShutdown), RpcTarget.All);
        foreach (var player in PhotonNetwork.PlayerList)
        {
            int actor = player.ActorNumber;
            int gem = playerGems.ContainsKey(actor) ? playerGems[actor] : 0;


            int totalFail = 0;
            if (playerTurnFail != null && playerTurnFail.TryGetValue(actor, out var failList) && failList != null)
            {
                for (int i = 0; i < failList.Count; i++) totalFail += failList[i];
            }
            else if (failCounts != null && failCounts.TryGetValue(actor, out var fc))
            {
                totalFail = fc; 
            }

            playerGems[actor] = gem;
            player.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
        {
            { "finalGem", gem },
            { "totalFail", totalFail }
        });
        }

        StartCoroutine(ExportAfterDelay(0.2f));
        StartCoroutine(LoadEndSceneWithDelay(1f));
    }


    [PunRPC]
    public void RPC_RequestEndGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[RPC] 接收到結束遊戲請求，MasterClient 執行 EndGame");
            EndGame();
        }
    }

    IEnumerator LoadEndSceneWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        PhotonNetwork.LoadLevel("EndScene");
    }

    /*
    -------------------------------------------------------------------------------------------------------------------------------------
                                                        Photon 事件處理
    -------------------------------------------------------------------------------------------------------------------------------------
    */

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("已斷線，原因: " + cause);

        switch (cause)
        {
            case DisconnectCause.ExceptionOnConnect:
            case DisconnectCause.Exception:
            case DisconnectCause.DisconnectByServerLogic:
            case DisconnectCause.ClientTimeout:
            case DisconnectCause.ServerTimeout:
                Debug.Log("試圖自動重連並重新進入房間...");
                PhotonNetwork.ReconnectAndRejoin();  // 嘗試回到原本房間
                break;

            default:
                Debug.LogWarning("不支援自動重連的斷線原因: " + cause);
                break;
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("已重新連線到 Master Server");
        // 可以顯示 UI 提示：等待房間回復中
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"重新進入房間失敗：{message} (Code {returnCode})");
        // 可以導回主選單，或重新選擇房間
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("成功重新加入房間！");
        // 可依需求恢復場景狀態
    }

    /*
    -------------------------------------------------------------------------------------------------------------------------------------
                                                    玩家遊玩資料紀錄
    -------------------------------------------------------------------------------------------------------------------------------------
    */

    public void OnHintButtonClicked()//點擊提示次數
    {
        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        photonView.RPC(nameof(RPC_RecordHintClick), RpcTarget.All, actorNumber);
    }

    [PunRPC]
    void RPC_RecordHintClick(int actorNumber)
    {
        var player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        if (player == null) return;

        int clickCount = 0;
        if (player.CustomProperties.ContainsKey("hintClickCount"))
            clickCount = (int)player.CustomProperties["hintClickCount"];

        clickCount++;
        player.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
        {
            { "hintClickCount", clickCount }
        });

        if (PhotonNetwork.IsMasterClient)
        {
            if (!currentTurnHintClicks.ContainsKey(actorNumber))
                currentTurnHintClicks[actorNumber] = 0;
            currentTurnHintClicks[actorNumber]++;
        }
        Debug.Log($"玩家 {actorNumber} 點擊提示，總次數: {clickCount}");
    }
    private Dictionary<int, List<float>> playerTurnDurations = new Dictionary<int, List<float>>();

    // 新增：每位玩家每回合提示次數（每回合一個整數）
    private Dictionary<int, List<int>> playerTurnHintClicks = new Dictionary<int, List<int>>();

    // 新增：目前這一回合已點提示次數（回合結束會歸零）
    private Dictionary<int, int> currentTurnHintClicks = new Dictionary<int, int>();

    private Dictionary<int, List<int>> playerTurnGemSpent = new Dictionary<int, List<int>>();   // 每回合花費寶石
    private Dictionary<int, List<int>> playerTurnDiscard = new Dictionary<int, List<int>>();   // 每回合是否棄牌(0/1)
    private Dictionary<int, List<int>> playerTurnFail = new Dictionary<int, List<int>>();   // 每回合失敗次數
    private Dictionary<int, int> currentTurnGemSpent = new Dictionary<int, int>();  // 當回合累積寶石消耗
    private Dictionary<int, int> currentTurnDiscardCnt = new Dictionary<int, int>();  // 當回合棄牌次數
    private Dictionary<int, int> currentTurnFailCnt = new Dictionary<int, int>();  // 當回合失敗次數
    private Dictionary<int, List<int>> playerTurnGemGained = new Dictionary<int, List<int>>();// 每回合寶石獎勳（得到的寶石數量）
    private Dictionary<int, int> currentTurnGemGained = new Dictionary<int, int>();// 當回合累積的「本回合得到的寶石數量」
    public Dictionary<int, int> failCounts = new Dictionary<int, int>();

    [PunRPC]
    void RPC_AddCurrentTurnGemGained(int actorNumber, int delta)
    {
        if (!currentTurnGemGained.ContainsKey(actorNumber))
            currentTurnGemGained[actorNumber] = 0;
        currentTurnGemGained[actorNumber] += Mathf.Max(0, delta);
    }
    public void RecordMyTurnDuration(float seconds)
    {
        int actor = PhotonNetwork.LocalPlayer.ActorNumber;
        // 送到 MasterClient 做統一記錄，避免多端各自存不同步
        PhotonView.Get(GameSceneManager.Instance).RPC(
        nameof(RPC_RecordTurnDuration), RpcTarget.MasterClient, actor, seconds);
    }

    [PunRPC]
    void RPC_RecordTurnDuration(int actorNumber, float seconds)
    {
        seconds = Mathf.Clamp(seconds, 0f, 10_000f);

        // 1) 用時
        if (!playerTurnDurations.ContainsKey(actorNumber))
            playerTurnDurations[actorNumber] = new List<float>();
        playerTurnDurations[actorNumber].Add(seconds);

        // 2) 當回合提示次數 → 歸檔 + 清零
        if (!playerTurnHintClicks.ContainsKey(actorNumber))
            playerTurnHintClicks[actorNumber] = new List<int>();
        int hintThisTurn = currentTurnHintClicks.TryGetValue(actorNumber, out var h) ? h : 0;
        playerTurnHintClicks[actorNumber].Add(hintThisTurn);
        currentTurnHintClicks[actorNumber] = 0;

        // 3) 當回合寶石消耗 → 歸檔 + 清零
        if (!playerTurnGemSpent.ContainsKey(actorNumber))
            playerTurnGemSpent[actorNumber] = new List<int>();
        int gemThisTurn = currentTurnGemSpent.TryGetValue(actorNumber, out var g) ? g : 0;
        playerTurnGemSpent[actorNumber].Add(gemThisTurn);
        currentTurnGemSpent[actorNumber] = 0;

        // 4) 當回合是否棄牌/次數 → 歸檔 + 清零
        if (!playerTurnDiscard.ContainsKey(actorNumber))
            playerTurnDiscard[actorNumber] = new List<int>();
        int discardThisTurn = currentTurnDiscardCnt.TryGetValue(actorNumber, out var d) ? d : 0;
        playerTurnDiscard[actorNumber].Add(discardThisTurn);
        currentTurnDiscardCnt[actorNumber] = 0;

        // 5) 當回合失敗次數 → 歸檔 + 清零
        if (!playerTurnFail.ContainsKey(actorNumber))
            playerTurnFail[actorNumber] = new List<int>();
        int failThisTurn = currentTurnFailCnt.TryGetValue(actorNumber, out var f) ? f : 0;
        playerTurnFail[actorNumber].Add(failThisTurn);
        currentTurnFailCnt[actorNumber] = 0;

        if (!playerTurnGemGained.ContainsKey(actorNumber))
            playerTurnGemGained[actorNumber] = new List<int>();
        int gainedThisTurn = currentTurnGemGained.TryGetValue(actorNumber, out var gg) ? gg : 0;
        playerTurnGemGained[actorNumber].Add(gainedThisTurn);
        currentTurnGemGained[actorNumber] = 0;

        Debug.Log($"[TurnEnd] Actor {actorNumber} 回合#{playerTurnDurations[actorNumber].Count}: " +
                  $"Time={seconds:F2}s, Hint={hintThisTurn}, Gem={gemThisTurn}, Discard={discardThisTurn}, Fail={failThisTurn}");
    }
    public void ExportTurnDurationsCsv()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        string timestamp = DateTime.Now.ToString("MMdd_HHmm");
        string fileName = $"{timestamp}_回合數據.csv";

        var sb = new StringBuilder();
        sb.AppendLine("玩家名稱,玩家編號,回合,回合用時(秒),查看提示次數,花費寶石,棄牌次數,失敗次數,寶石獎勵");

        foreach (var p in PhotonNetwork.PlayerList)
        {
            int actor = p.ActorNumber;
            string name = p.NickName;

            playerTurnDurations.TryGetValue(actor, out var times);
            playerTurnHintClicks.TryGetValue(actor, out var hints);
            playerTurnGemSpent.TryGetValue(actor, out var gemsSpent);
            playerTurnDiscard.TryGetValue(actor, out var discards);
            playerTurnFail.TryGetValue(actor, out var fails);
            playerTurnGemGained.TryGetValue(actor, out var gemsGained);

            if (times == null) continue;
            int n = times.Count;

            for (int i = 0; i < n; i++)
            {
                float sec = times[i];
                int usedHint = (hints != null && i < hints.Count) ? hints[i] : 0;
                int gemSpent = (gemsSpent != null && i < gemsSpent.Count) ? gemsSpent[i] : 0;
                int didDiscard = (discards != null && i < discards.Count) ? discards[i] : 0;
                int failCount = (fails != null && i < fails.Count) ? fails[i] : 0;
                int gemReward = (gemsGained != null && i < gemsGained.Count) ? gemsGained[i] : 0;

                sb.AppendLine($"{name},{actor},{i + 1},{sec:F2},{usedHint},{gemSpent},{didDiscard},{failCount},{gemReward}");
            }
        }

        // ====== 先上傳到 Google Sheets ======
        try
        {
            // 輕度正規化換行；Apps Script 的 Utilities.parseCsv 兩者都可，但這裡統一為 \n
            string csvForSheet = sb.ToString().Replace("\r\n", "\n");

            // 分頁名稱：時間戳 + （可選）房名
            string baseTabName = $"{timestamp}_回合數據";
            if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null && !string.IsNullOrEmpty(PhotonNetwork.CurrentRoom.Name))
                baseTabName += $"_{PhotonNetwork.CurrentRoom.Name}";

            if (GoogleSheetUploader.Instance != null)
                GoogleSheetUploader.Instance.UploadCsv(baseTabName, csvForSheet);
            else
                Debug.LogWarning("[ExportTurnDurationsCsv] 找不到 GoogleSheetUploader.Instance，略過上傳。");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ExportTurnDurationsCsv] 上傳 Google Sheet 失敗：{ex}");
        }

        // ====== 照舊：匯出檔案 ======
#if UNITY_WEBGL && !UNITY_EDITOR
    string bom = "\uFEFF";
    string csvContent = bom + sb.ToString().Replace("\n", "\r\n"); // WebGL 下載用 CRLF
    DownloadCSV(fileName, csvContent);
#else
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string path = Path.Combine(desktopPath, fileName);
        // 桌面檔案保留 UTF-8 BOM（Windows Excel 友善）
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        Debug.Log($"回合數據已匯出到桌面：{path}");
#endif
    }


    public void ExportPlayerSummaryCsv()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        string timestamp = DateTime.Now.ToString("MMdd_HHmm");
        string fileName = $"{timestamp}_結算數據.csv";

        var sb = new StringBuilder();
        sb.AppendLine("玩家名稱,玩家編號,查看提示總次數,最終寶石數,總失敗次數");

        foreach (var player in PhotonNetwork.PlayerList)
        {
            string name = player.NickName;
            int actorNumber = player.ActorNumber;

            // 總提示次數
            int totalHintClicks = 0;
            if (player.CustomProperties.ContainsKey("hintClickCount"))
                totalHintClicks = (int)player.CustomProperties["hintClickCount"];

            // 最終寶石數量
            int finalGem = 0;
            if (player.CustomProperties.ContainsKey("finalGem"))
                finalGem = (int)player.CustomProperties["finalGem"];
            //else if (playerGems.TryGetValue(actorNumber, out var localGem))
                //finalGem = localGem;

            // 總失敗次數（優先使用回合累加，其次 failCounts，再其次 customProperties）
            int totalFail = 0;
            if (playerTurnFail != null && playerTurnFail.TryGetValue(actorNumber, out var failList) && failList != null)
            {
                for (int i = 0; i < failList.Count; i++) totalFail += failList[i];
            }
            else if (failCounts != null && failCounts.TryGetValue(actorNumber, out var fc))
            {
                totalFail = fc;
            }
            else if (player.CustomProperties.ContainsKey("totalFail"))
            {
                totalFail = (int)player.CustomProperties["totalFail"];
            }

            sb.AppendLine($"{name},{actorNumber},{totalHintClicks},{finalGem},{totalFail}");
        }

        // ====== 先上傳到 Google Sheets ======
        try
        {
            string csvForSheet = sb.ToString().Replace("\r\n", "\n");

            string baseTabName = $"{timestamp}_結算數據";
            if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null && !string.IsNullOrEmpty(PhotonNetwork.CurrentRoom.Name))
                baseTabName += $"_{PhotonNetwork.CurrentRoom.Name}";

            if (GoogleSheetUploader.Instance != null)
                GoogleSheetUploader.Instance.UploadCsv(baseTabName, csvForSheet);
            else
                Debug.LogWarning("[ExportPlayerSummaryCsv] 找不到 GoogleSheetUploader.Instance，略過上傳。");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ExportPlayerSummaryCsv] 上傳 Google Sheet 失敗：{ex}");
        }

        // ====== 照舊：匯出檔案 ======
#if UNITY_WEBGL && !UNITY_EDITOR
    string bom = "\uFEFF";
    string csvContent = bom + sb.ToString().Replace("\n", "\r\n"); // WebGL 下載用 CRLF
    DownloadCSV(fileName, csvContent);
#else
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string path = Path.Combine(desktopPath, fileName);
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        Debug.Log($"結算數據已匯出到桌面：{path}");
#endif
    }
    private IEnumerator ExportAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ExportTurnDurationsCsv();
        ExportPlayerSummaryCsv();
    }



    /*
    -------------------------------------------------------------------------------------------------------------------------------------
                                                        額外功能
    -------------------------------------------------------------------------------------------------------------------------------------
    */

    public void SetRealtimeTipsEnabled(bool isOn)
    {
        enableRealtimeTips = isOn;
        //if (!isOn && TipPanel != null)
            //TipPanel.SetActive(false);
    }

    private bool isShuttingDown = false;
    public bool IsShuttingDown => isShuttingDown;

    [PunRPC]
    void RPC_BeginShutdown()
    {
        isShuttingDown = true;
    }
}
