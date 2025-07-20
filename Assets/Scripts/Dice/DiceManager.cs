using UnityEngine;
using UnityEngine.UI;

public class DiceManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject darkBackground;
    public GameObject diceChoicePanel;
    public GameObject mainDiceButton;
    public GameObject resultDicePrefab;             // 骰子結果的 prefab
    public RectTransform resultDiceContainer;       // 結果骰子容器
    public Image diceAnimationImage;
    public GameObject resultText;

    [Header("Roller Logic")]
    public DiceRoller diceRoller;                   // 擲骰動畫的 Script

    [Header("Roll Limits")]
    private int rollCount = 0;
    private const int maxRolls = 3;

    [Header("Result Sprites")]
    public Sprite[] primaryResultSprites;     // 單面三原色圖
    public Sprite[] secondaryResultSprites;   // 單面二次色圖

    public GameObject checkToRoll;//確認擲骰
    public GameObject rejectToRoll;//取消擲骰
    public GameObject checkToPrimaryDicePanel;
    public GameObject checkToSndDicePanel;
    public bool IsPrim ;
    public int r; // 用來存擲骰結果 index（可外部使用）

    public static DiceManager Instance;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        diceRoller.onRollComplete = OnDiceRollFinished;
    }
    public void OnMainDiceClicked()
    {
        if (rollCount >= maxRolls)
        {
            Debug.Log("已經達到最大擲骰次數！");
            return;
        }

        diceChoicePanel.SetActive(true);
        mainDiceButton.SetActive(false);
    }

    public void OnPrimaryDiceClicked()
    {
        checkToPrimaryDicePanel.SetActive(true);
        checkToRoll.SetActive(true);
        rejectToRoll.SetActive(true);
        IsPrim = true;
    }

    public void OnSecondaryDiceClicked()
    {
        checkToSndDicePanel.SetActive(true);
        checkToRoll.SetActive(true);
        rejectToRoll.SetActive(true);
        IsPrim = false;
    }

    public void OnCheckToRollClicked()
    {
        bool canRoll = GameSceneManager.Instance.TryConsumeGemForDice(IsPrim); // true=三原色, false=二次色
        if (!canRoll)
        {
            Debug.Log("寶石不足，無法擲骰！");
            return;
        }

        StartDiceRoll(primary: IsPrim);
    }
    public void OnRejectToRollClicked()
    {
        diceChoicePanel.SetActive(false);
        mainDiceButton.SetActive(true);
        checkToPrimaryDicePanel.SetActive(false);
        checkToSndDicePanel.SetActive(false);
        checkToRoll.SetActive(false);
        rejectToRoll.SetActive(false);
    }
    void StartDiceRoll(bool primary)
    {
        // 隱藏選擇面板，顯示動畫骰子圖
        OnRejectToRollClicked();
        diceAnimationImage.gameObject.SetActive(true);
        diceRoller.RollDice(primary);
    }


    void OnDiceRollFinished()
    {
        rollCount += 1;
        r = diceRoller.resultIndex;
        resultDiceContainer.GetComponent<Image>().gameObject.SetActive(true);
        resultText.SetActive(true);
        GameObject resultDice = Instantiate(resultDicePrefab, resultDiceContainer);

        Sprite resultSprite = diceRoller.isPrimary
            ? primaryResultSprites[r]
            : secondaryResultSprites[r];

        resultDice.GetComponent<Image>().sprite = resultSprite;
        mainDiceButton.SetActive(true);
    }
    public void ResetDiceUI()
    {
        
        foreach (Transform child in resultDiceContainer)
        {
            Destroy(child.gameObject);
        }
        resultDiceContainer.GetComponent<Image>().gameObject.SetActive(false);
        resultText.SetActive(false);
        mainDiceButton.SetActive(true);
        rollCount = 0;
        diceChoicePanel.SetActive(false);
        darkBackground.SetActive(false);
    }
    public void DeselectResultDiceVisual() //取消highLighted
    {
        foreach (Transform child in resultDiceContainer)
        {
            resultDice dice = child.GetComponent<resultDice>();
            if (dice != null && dice.isCardSelected)
            {
                dice.SetSelected(false);
            }
        }
    }
}
