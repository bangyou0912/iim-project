using UnityEngine;
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
