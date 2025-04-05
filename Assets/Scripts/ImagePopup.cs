using UnityEngine;
using UnityEngine.UI;

public class ImagePopup : MonoBehaviour
{
    public GameObject popupPanel;
    public Image popupImage;
    public Sprite imageToShow;

    public void ShowImage()
    {
        popupImage.sprite = imageToShow;
        popupPanel.SetActive(true);
    }

    public void HideImage()
    {
        popupPanel.SetActive(false);
    }
}
