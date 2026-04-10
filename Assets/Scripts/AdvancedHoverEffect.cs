using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class AdvancedHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("ターゲット設定")]
    public RectTransform iconTransform; // アイコン全体の親 (拡大用)
    public Image iconBgImage;           // アイコンの背景 (円/四角)
    
    // ▼▼▼ 追加: 画像アイコンとテキストアイコン、どちらも対応できるようにする ▼▼▼
    public TMP_Text iconText;           // テキストの場合 (+記号など)
    public Image iconImage;             // 画像の場合 (フォルダアイコンなど)

    public Outline buttonOutline;
    public Image glowShadowImage;       // 影用の画像 (GlowShadow)

    [Header("ホバー時のスタイル")]
    public float iconScaleMultiplier = 1.15f;
    public Color iconBgHoverColor;
    
    // ▼▼▼ 修正: テキストと画像、それぞれのホバー色設定 ▼▼▼
    public Color iconContentHoverColor = Color.white; // 中身(文字/画像)のホバー色
    
    public Color outlineHoverColor;
    public Color shadowNormalColor = new Color(0, 0, 0, 0.1f);
    public Color shadowHoverColor;

    [Header("アニメーション速度")]
    public float duration = 0.2f;

    // 内部変数
    private Vector3 defaultIconScale;
    private Color defaultIconBgColor;
    private Color defaultIconTextColor;
    private Color defaultIconImageColor; // 画像の初期色用
    private Color defaultOutlineColor;
    
    private Coroutine currentCoroutine;

    void Start()
    {
        // 初期状態の保存
        if (iconTransform) defaultIconScale = iconTransform.localScale;
        if (iconBgImage) defaultIconBgColor = iconBgImage.color;
        
        if (iconText) defaultIconTextColor = iconText.color;
        if (iconImage) defaultIconImageColor = iconImage.color; // 画像の色保存
        
        if (buttonOutline) defaultOutlineColor = buttonOutline.effectColor;
        if (glowShadowImage) glowShadowImage.color = shadowNormalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(AnimateToHoverState(true));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(AnimateToHoverState(false));
    }

    private IEnumerator AnimateToHoverState(bool isHovering)
    {
        float timer = 0f;

        // --- スタート値 ---
        Vector3 startScale = iconTransform ? iconTransform.localScale : Vector3.one;
        Color startBgColor = iconBgImage ? iconBgImage.color : Color.white;
        Color startTextColor = iconText ? iconText.color : Color.clear;
        Color startImageColor = iconImage ? iconImage.color : Color.clear; // 画像の現在色
        Color startOutlineColor = buttonOutline ? buttonOutline.effectColor : Color.clear;
        Color startShadowColor = glowShadowImage ? glowShadowImage.color : Color.clear;

        // --- ゴール値 ---
        Vector3 targetScale = isHovering ? defaultIconScale * iconScaleMultiplier : defaultIconScale;
        Color targetBgColor = isHovering ? iconBgHoverColor : defaultIconBgColor;
        Color targetContentColor = isHovering ? iconContentHoverColor : defaultIconTextColor; // テキスト用
        Color targetImageColor = isHovering ? iconContentHoverColor : defaultIconImageColor;  // 画像用
        Color targetOutlineColor = isHovering ? outlineHoverColor : defaultOutlineColor;
        Color targetShadowColor = isHovering ? shadowHoverColor : shadowNormalColor;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            
            // 各プロパティの変化
            if (iconTransform) iconTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            if (iconBgImage) iconBgImage.color = Color.Lerp(startBgColor, targetBgColor, t);
            
            // テキストがあれば色を変える
            if (iconText) iconText.color = Color.Lerp(startTextColor, targetContentColor, t);
            
            // ▼▼▼ 画像があれば色を変える ▼▼▼
            if (iconImage) iconImage.color = Color.Lerp(startImageColor, targetImageColor, t);
            
            if (buttonOutline) buttonOutline.effectColor = Color.Lerp(startOutlineColor, targetOutlineColor, t);
            if (glowShadowImage) glowShadowImage.color = Color.Lerp(startShadowColor, targetShadowColor, t);

            yield return null;
        }

        // 最終値セット
        if (iconTransform) iconTransform.localScale = targetScale;
        if (iconBgImage) iconBgImage.color = targetBgColor;
        if (iconText) iconText.color = targetContentColor;
        if (iconImage) iconImage.color = targetImageColor; // 画像もセット
        if (buttonOutline) buttonOutline.effectColor = targetOutlineColor;
        if (glowShadowImage) glowShadowImage.color = targetShadowColor;
    }
}