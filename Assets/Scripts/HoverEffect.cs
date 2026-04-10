using UnityEngine;
using UnityEngine.EventSystems; // マウスイベントを使うために必要
using System.Collections;

public class HoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("設定")]
    public float scaleSize = 1.1f;  // マウスが乗った時の大きさ（1.1倍）
    public float duration = 0.1f;   // 変化にかかる時間（秒）

    private Vector3 originalScale;
    private Coroutine currentCoroutine;

    void Start()
    {
        // 最初の大きさを記憶しておく
        originalScale = transform.localScale;
    }

    // マウスが乗ったとき
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 今動いているアニメーションがあれば止める
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        
        // 拡大するアニメーションを開始
        currentCoroutine = StartCoroutine(AnimateScale(originalScale * scaleSize));
    }

    // マウスが離れたとき
    public void OnPointerExit(PointerEventData eventData)
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        
        // 元のサイズに戻るアニメーションを開始
        currentCoroutine = StartCoroutine(AnimateScale(originalScale));
    }

    // サイズをなめらかに変える処理（コルーチン）
    private IEnumerator AnimateScale(Vector3 targetScale)
    {
        float timer = 0f;
        Vector3 startScale = transform.localScale;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            // Lerpを使って徐々にサイズを変える
            transform.localScale = Vector3.Lerp(startScale, targetScale, timer / duration);
            yield return null; // 1フレーム待機
        }
        
        // 最後にきっちり目標サイズにする
        transform.localScale = targetScale;
    }
}