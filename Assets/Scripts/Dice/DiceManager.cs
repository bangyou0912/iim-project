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

    [Header("Roller Logic")]
    public DiceRoller diceRoller;                   // 擲骰動畫的 Script

    [Header("Roll Limits")]
    private int rollCount = 0;
    private const int maxRolls = 3;

    [Header("Result Sprites")]
    public Sprite[] primaryResultSprites;     // 單面三原色圖
    public Sprite[] secondaryResultSprites;   // 單面二次色圖


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
        StartDiceRoll(primary: true);
    }

    public void OnSecondaryDiceClicked()
    {
        StartDiceRoll(primary: false);
    }

    void StartDiceRoll(bool primary)
    {
        // 隱藏選擇面板，顯示動畫骰子圖
        diceChoicePanel.SetActive(false);
        diceAnimationImage.gameObject.SetActive(true);
        diceRoller.RollDice(primary);
    }


    void OnDiceRollFinished()
    {
        rollCount += 1;
        // 根據目前骰子的數量決定位置
        r = diceRoller.resultIndex;
        resultDiceContainer.GetComponent<Image>().gameObject.SetActive(true);
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
