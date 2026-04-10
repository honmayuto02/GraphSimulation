using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Root")]
    public GameObject mainCanvas;

    [Header("Log Settings")]
    public Text logText; // Inspectorで新しいTextオブジェクトを設定
    public float displayDuration = 3.0f; // メッセージが表示されている時間（秒）
    public float fadeDuration = 1.0f;    // メッセージが消えるときのアニメーション時間（秒）

    private Coroutine currentLogCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // もし'H'キーが押された瞬間なら
        if (Input.GetKeyDown(KeyCode.H))
        {
            // mainCanvasが設定されていれば、表示・非表示を切り替える
            if (mainCanvas != null)
            {
                // SetActiveに、現在の状態の逆(!mainCanvas.activeSelf)を渡すことでトグルする
                mainCanvas.SetActive(!mainCanvas.activeSelf);
            }
        }
    }

    // ログメッセージをUIに表示するための公開メソッド
    public void Log(string message)
    {
        // 既に前のメッセージのフェード処理が動いていたら、それを停止
        if (currentLogCoroutine != null)
        {
            StopCoroutine(currentLogCoroutine);
        }
        
        // 新しいメッセージを表示し、フェード処理を開始
        currentLogCoroutine = StartCoroutine(ShowAndFadeMessage(message));
    }

    private IEnumerator ShowAndFadeMessage(string message)
    {
        // 1. メッセージを設定し、完全に不透明にする
        logText.text = message;
        logText.color = new Color(logText.color.r, logText.color.g, logText.color.b, 1f);

        // 2. 設定された秒数だけ待機
        yield return new WaitForSeconds(displayDuration);

        // 3. 徐々に透明にする（フェードアウト）
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            logText.color = new Color(logText.color.r, logText.color.g, logText.color.b, alpha);
            yield return null;
        }
    }
}