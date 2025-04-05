using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class change_card : MonoBehaviour
{
    public Texture2D[] cards;
    public RawImage rawImages;
    private bool isEnlarged = false;
    public float scale = 1/2;
    public void chouca()
    {
        rawImages = rawImages.GetComponent<RawImage>();
        int result = Random.Range(0, 10);
        rawImages.texture = cards[result];
    }
   
    public void big()
    {
        if(isEnlarged == false)
        {
            transform.localScale += new Vector3(scale, scale, 0f);
            isEnlarged = true;
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

