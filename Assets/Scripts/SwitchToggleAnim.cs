using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Toggle))]
public class SwitchToggleAnim : MonoBehaviour
{
    [Header("パーツ参照")]
    public RectTransform handleRect; // 動く丸（Handle）
    public Image backgroundImage;    // 背景（Background）

    [Header("設定")]
    public Color backgroundActiveColor = new Color(0.2f, 0.84f, 0.3f); // ONの色（緑など）
    public Color backgroundDefaultColor = new Color(0.8f, 0.8f, 0.8f); // OFFの色（グレー）
    
    public float handleMoveDuration = 0.2f; // アニメーション時間

    private Toggle toggle;
    private float handlePositionX; // 丸の移動距離（自動計算）
    private Coroutine currentCoroutine;

    void Awake()
    {
        toggle = GetComponent<Toggle>();

        // 丸の移動幅を計算（背景の幅の半分 - 少し余白）
        // ※BackgroundとHandleが正しくセットアップされている前提です
        handlePositionX = backgroundImage.rectTransform.rect.width * 0.25f; 

        // 初期状態の表示を強制セット
        UpdateVisual(toggle.isOn, true);

        // トグルの値が変わったらアニメーションを開始
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    void OnToggleValueChanged(bool isOn)
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(AnimateSwitch(isOn));
    }

    // アニメーション処理
    IEnumerator AnimateSwitch(bool isOn)
    {
        float timer = 0f;

        Vector2 startPos = handleRect.anchoredPosition;
        // ONなら右(+x)、OFFなら左(-x)
        Vector2 endPos = new Vector2(isOn ? handlePositionX : -handlePositionX, 0);

        Color startColor = backgroundImage.color;
        Color endColor = isOn ? backgroundActiveColor : backgroundDefaultColor;

        while (timer < handleMoveDuration)
        {
            timer += Time.deltaTime;
            float t = timer / handleMoveDuration;
            
            // 滑らかな動き（EaseOut）
            t = Mathf.Sin(t * Mathf.PI * 0.5f);

            handleRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            backgroundImage.color = Color.Lerp(startColor, endColor, t);

            yield return null;
        }

        // 最終値をきっちりセット
        handleRect.anchoredPosition = endPos;
        backgroundImage.color = endColor;
    }

    // 瞬時に状態を反映させる（初期化用）
    void UpdateVisual(bool isOn, bool instant)
    {
        Vector2 targetPos = new Vector2(isOn ? handlePositionX : -handlePositionX, 0);
        Color targetColor = isOn ? backgroundActiveColor : backgroundDefaultColor;

        if (instant)
        {
            if (handleRect) handleRect.anchoredPosition = targetPos;
            if (backgroundImage) backgroundImage.color = targetColor;
        }
    }
}