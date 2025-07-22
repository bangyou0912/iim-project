

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DiceRoller : MonoBehaviour
{
    public Sprite[] primaryDiceSprites;     // 三原色骰圖
    public Sprite[] secondaryDiceSprites;   // 二次色骰圖

    private Sprite[] _currentSprites; // 記住這次是哪組骰子
    public Sprite[] currentSprites => _currentSprites;
    public int resultIndex;          // 外部也能存取結果
    public bool isPrimary { get; private set; } //確認骰的是三原色還是二次色

    public Image diceImage;       // 顯示動畫的 Image（DiceAnimationImage）
    public RectTransform diceRect;
    public GameObject darkBackground;

    public System.Action onRollComplete;

    public Vector2 centerPosition = Vector2.zero;
    public Vector2 enlargedSize = new Vector2(500, 500);
    public float moveTime = 0.1f;
    public float rollDuration = 2f;
    public float initialRollSpeed = 0.05f; // 一開始超快
    public float finalRollSpeed = 0.09f; // 最後變慢
    public bool isRolling = false; 

    public AudioClip rollSound;       // 骰子滾動音效
    public AudioClip landSound;       // 骰子落地音效
    private AudioSource audioSource;

    private Vector2 originalPosition;
    private Vector2 originalSize;
    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
    }
    public void RollDice(bool isPrimary)
    {
        // 根據選擇 選擇骰子圖案   
        this.isPrimary = isPrimary;//讓 DiceManager 知道現在是三原色or二次色骰子  
        _currentSprites = isPrimary ? primaryDiceSprites : secondaryDiceSprites;
        StartCoroutine(PlayDiceAnimation(currentSprites));
        diceImage.gameObject.SetActive(true); // 顯示骰子圖像  
    }

    
    public IEnumerator PlayDiceAnimation(Sprite[] sprites)
    {

        // 將動畫拉到最上層
        diceImage.transform.SetAsLastSibling();
        darkBackground.SetActive(true);
        
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
            // 旋轉骰子
            
            int rand = Random.Range(0, sprites.Length);
            diceImage.sprite = sprites[rand];            
            diceImage.transform.Rotate(new Vector3(0, 0, 30)); // 每次轉30度

            // 播滾動聲
            if (!audioSource.isPlaying)
            {
                audioSource.PlayOneShot(rollSound);
            }

            timer += currentSpeed;

            currentSpeed = Mathf.Lerp(initialRollSpeed, finalRollSpeed, timer / rollDuration);
            yield return new WaitForSeconds(currentSpeed);
            
        }

        // 定格結果
               
        diceImage.transform.rotation = Quaternion.identity;
        resultIndex = Random.Range(0, sprites.Length);
        diceImage.sprite = sprites[resultIndex];
        Debug.Log("骰子結果: " + (diceImage.sprite.name));
        audioSource.PlayOneShot(landSound); 

        diceImage.transform.SetAsLastSibling();
        yield return new WaitForSeconds(2f);
        isRolling = false;
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
