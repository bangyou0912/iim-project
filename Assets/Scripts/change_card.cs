using UnityEngine;
using UnityEngine.UI;


public class change_card : MonoBehaviour
{
    public Texture2D[] cards;
    public RawImage rawImages;
    private bool isEnlarged = false;
    public int count = 0;
     public float scale = 1/2;
    public void chouca()
    {
        rawImages = rawImages.GetComponent<RawImage>();
        int result = Random.Range(0, 10);
        rawImages.texture = cards[result];

    }
   
    public void big()
    {
        if(count == 0)
        {
            transform.localScale += new Vector3(scale, scale, 0f);
            isEnlarged = true;
            count +=1;
        }
        else
        {
            small();
        }
    }
    public void small()
    {
        transform.localScale -= new Vector3(scale, scale, 0f);
        isEnlarged = false;
        count -= 1;
    }
    private void Update()
    {
        if (isEnlarged && Input.GetKeyDown(KeyCode.Return))
        {
            chouca();
            small();
        }
    }

}

