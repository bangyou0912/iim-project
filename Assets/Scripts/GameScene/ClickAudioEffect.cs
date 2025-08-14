using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]

public class ClickAudioEffect : MonoBehaviour
{
    public AudioClip ClickAudio;
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayBubble(ClickAudio);
        });
    }
}
