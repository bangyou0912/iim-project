using System.Collections.Generic;
using UnityEngine;

public class PlayerState : MonoBehaviour
{
    public List<string> handCards;
    public int gems;
    public int discardCount;
    public bool isEliminated;
    public List<string> temporaryDiceColors;

    public PlayerState(List<string> initialCards)
    {
        handCards = new List<string>(initialCards);
        gems = 3;
        discardCount = 0;
        isEliminated = false;
        temporaryDiceColors = new List<string>();
    }

    public void CheckElimination()
    {
        if (discardCount >= 3)
        {
            isEliminated = true;
        }
    }

    public void AddCard(string card)
    {
        handCards.Add(card);
    }

    public void RemoveCard(string card)
    {
        handCards.Remove(card);
    }

    public void AddGems(int amount)
    {
        gems += amount;
    }

    public bool SpendGems(int amount)
    {
        if (gems >= amount)
        {
            gems -= amount;
            return true;
        }
        return false;
    }
}
