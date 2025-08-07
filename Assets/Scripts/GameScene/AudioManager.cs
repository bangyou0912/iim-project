using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public AudioSource bgmSource;        // 背景音樂的 AudioSource
    public AudioClip bgmClip;            // 要播放的背景音樂

    void Awake()
    {
        // 確保只有一個 AudioManager 存在
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 切場景不會消失
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 如果沒設定 AudioSource，自動新增一個
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
        }

        PlayBGM(); // 開始播放
    }

    public void PlayBGM()
    {
        if (bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.Play();
        }
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }
}
