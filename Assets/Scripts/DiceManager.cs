
using UnityEngine;
using UnityEngine.UI;

public class DiceManager : MonoBehaviour
{
    public GameObject darkBackground;
    public GameObject diceChoicePanel;
    public GameObject mainDiceButton;
    public Image diceAnimationImage;

    public DiceRoller diceRoller; // 擲骰動畫的 script

    public void Start()
    {
        diceRoller.onRollComplete = OnDiceRollFinished;
    }

    public void OnMainDiceClicked()
    {
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
        mainDiceButton.SetActive(false);
        diceChoicePanel.SetActive(false);
        diceAnimationImage.gameObject.SetActive(true);

        diceRoller.RollDice(primary); // 播放對應骰子動畫

    }
    void OnDiceRollFinished()
    {
        // 恢復主骰子
        mainDiceButton.SetActive(true);
    }

}
