/*using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DiceRollerButton : MonoBehaviour
{
    public Button diceButton;         // 這就是你的骰子按鈕
    public Sprite[] diceSprites;      // 骰子的 6 張圖片
    public AudioClip rollSound;       // 骰子滾動音效
    public AudioClip landSound;       // 骰子落地音效
    public float rollDuration = 2.0f; // 滾動總時間
    public float initialRollSpeed = 0.05f; // 一開始超快
    public float finalRollSpeed = 0.3f;    // 最後變慢

    private AudioSource audioSource;
    private Image diceImage;          // Button 上的 Image
    private bool isRolling = false;   // 防止連續點擊

    void Start()
    {
        diceImage = diceButton.GetComponent<Image>();
        audioSource = gameObject.AddComponent<AudioSource>();

        // 綁定按鈕點擊事件
        diceButton.onClick.AddListener(RollDice);
    }

    public void RollDice()
    {
        if (!isRolling)
        {
            StartCoroutine(RollAnimation());
        }
    }

    private IEnumerator RollAnimation()
    {
        isRolling = true;

        float timer = 0f;
        float currentSpeed = initialRollSpeed;

        while (timer < rollDuration)
        {
            int randomIndex = Random.Range(0, diceSprites.Length);
            diceImage.sprite = diceSprites[randomIndex];

            // 旋轉骰子
            diceImage.transform.Rotate(new Vector3(0, 0, 30)); // 每次轉30度

            // 播滾動聲
            if (!audioSource.isPlaying)
            {
                audioSource.PlayOneShot(rollSound);
            }

            timer += currentSpeed;

            // 慢慢加速 (變慢)
            currentSpeed = Mathf.Lerp(initialRollSpeed, finalRollSpeed, timer / rollDuration);

            yield return new WaitForSeconds(currentSpeed);
        }

        // 最後確定一個點數
        int finalIndex = Random.Range(0, diceSprites.Length);
        diceImage.sprite = diceSprites[finalIndex];

        // 回正角度
        diceImage.transform.rotation = Quaternion.identity;

        // 播落地聲
        audioSource.PlayOneShot(landSound);

        isRolling = false;
    }
}
*/

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DiceRoller : MonoBehaviour
{
    public Sprite[] primaryDiceSprites;     // 三原色骰圖
    public Sprite[] secondaryDiceSprites;   // 二次色骰圖

    public Image diceImage;                 // 顯示動畫的 Image（DiceAnimationImage）
    public RectTransform diceRect;
    public GameObject darkBackground;
    public System.Action onRollComplete;

    public Vector2 centerPosition = Vector2.zero;
    public Vector2 enlargedSize = new Vector2(500, 500);
    public float moveTime = 0.2f;
    public float rollDuration = 2f;

    public float initialRollSpeed = 0.05f; // 一開始超快
    public float finalRollSpeed = 0.09f;    // 最後變慢

    public AudioClip rollSound;       // 骰子滾動音效
    public AudioClip landSound;
    private AudioSource audioSource;// 骰子落地音效

    private Vector2 originalPosition;
    private Vector2 originalSize;

    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
    }
    public void RollDice(bool isPrimary)
    {

        // 根據選擇 選擇骰子圖案
        Sprite[] selectedSprites = isPrimary ? primaryDiceSprites : secondaryDiceSprites;

        // 開始播放動畫
        StartCoroutine(PlayDiceAnimation(selectedSprites));
        // 顯示骰子圖像
        diceImage.gameObject.SetActive(true);
        
    }

    IEnumerator PlayDiceAnimation(Sprite[] sprites)
    {
        // 將動畫拉到最上層
        diceImage.transform.SetAsLastSibling();
        darkBackground.SetActive(true);
        diceImage.gameObject.SetActive(true);

        // 儲存原位置
        originalPosition = diceRect.anchoredPosition;
        originalSize = diceRect.sizeDelta;

        // 移動到中央並放大
        float t = 0;
        while (t < moveTime)
        {
            t += Time.deltaTime;
            float p = t / moveTime;
            diceRect.anchoredPosition = Vector2.Lerp(originalPosition, centerPosition, p);
            diceRect.sizeDelta = Vector2.Lerp(originalSize, enlargedSize, p);
            yield return null;
        }

        // 滾動動畫
        float currentSpeed = initialRollSpeed;
        float timer = 0f;
        while (timer < rollDuration)
        {
            audioSource.PlayOneShot(rollSound);
            int rand = Random.Range(0, sprites.Length);
            diceImage.sprite = sprites[rand];

            timer += currentSpeed;

            currentSpeed = Mathf.Lerp(initialRollSpeed, finalRollSpeed, timer / rollDuration);
            yield return new WaitForSeconds(currentSpeed);
        }

        // 定格結果

        int result = Random.Range(0, sprites.Length);
        diceImage.sprite = sprites[result];
        Debug.Log("骰子結果: " + (result + 1));

        audioSource.PlayOneShot(landSound);

        yield return new WaitForSeconds(2f);


        // 關閉黑色背景
        darkBackground.SetActive(false);

        diceRect.anchoredPosition = originalPosition;
        diceRect.sizeDelta = originalSize;
        diceImage.gameObject.SetActive(false);

        if (onRollComplete != null)
        {
            onRollComplete.Invoke();
        }

    }
}
