using UnityEngine;
using UnityEngine.UI;


public class changecard : MonoBehaviour
{
    public Texture2D[] cards;
    public RawImage[] rawImages;

    void Start()
    {
        chouca();
    }

    public void chouca()
    {
        for (int i = 0; i < 4; i++)
        {
            rawImages[i] = rawImages[i].GetComponent<RawImage>();
            int result = Random.Range(0, 10);
            rawImages[i].texture = cards[result];
        }

    }
}

