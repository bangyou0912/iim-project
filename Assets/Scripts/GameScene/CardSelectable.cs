using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardSelectable : MonoBehaviour, IPointerClickHandler
{
    public enum CardType { Hand, Dice, Public }

    [Header("設定")]
    public string cardColorName; // ← 這個是顏色的名稱（修正名稱）
    public CardType cardtype;
    public Image highlighted;
    public bool isCardSelected = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        isCardSelected = !isCardSelected;
        if (highlighted != null)
            highlighted.gameObject.SetActive(isCardSelected);

        switch (cardtype)
        {
            case CardType.Hand:
            case CardType.Dice:
                GameSceneManager.Instance.OnCardSelected(this);
                break;
            case CardType.Public:
                GameSceneManager.Instance.OnPublicCardClicked(this);
                break;
        }
    }

    public void ResetSelection()
    {
        isCardSelected = false;
        if (highlighted != null)
            highlighted.gameObject.SetActive(false);
    }
}
