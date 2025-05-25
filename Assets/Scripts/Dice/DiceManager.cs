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
        if (IsPrim)
        {
            StartDiceRoll(primary: true);
        }
        else
        {
            StartDiceRoll(primary: false);
        }

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
        // 根據目前骰子的數量決定位置
        r = diceRoller.resultIndex;
        resultDiceContainer.GetComponent<Image>().gameObject.SetActive(true);
        resultText.SetActive(true);
        // 實例化結果骰子
        GameObject resultDice = Instantiate(resultDicePrefab, resultDiceContainer);

        //根據目前是三原色還是二次色選圖
        Sprite resultSprite = diceRoller.isPrimary
            ? primaryResultSprites[r]
            : secondaryResultSprites[r];

        // 根據骰子的結果圖設定顯示圖片
        resultDice.GetComponent<Image>().sprite = resultSprite;
        mainDiceButton.SetActive(true);
    }

}
