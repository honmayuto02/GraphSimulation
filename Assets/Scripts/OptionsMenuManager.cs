using UnityEngine;
using UnityEngine.UI;

public class OptionsMenuManager : MonoBehaviour
{
    [Header("UI参照")]
    public GameObject optionsWindow;
    public Toggle gridToggle;          // グリッドのトグル

    [Header("制御対象")]
    public GameObject gridObject;      // グリッド本体

    void Start()
    {
        // 1. ウィンドウを初期化（閉じる）
        if (optionsWindow != null) 
            optionsWindow.SetActive(false);

        // 2. ▼▼▼ 保存されたデータを復元する処理 ▼▼▼
        
        // GameDataから設定を読み込む
        bool savedGridState = GameData.isGridVisible;

        // グリッドオブジェクトの表示を合わせる
        if (gridObject != null)
        {
            gridObject.SetActive(savedGridState);
        }

        // トグルの見た目(ON/OFF)を合わせる
        // ※これを変えると onValueChanged イベントが発火して、下の SetGridVisibility も自動で呼ばれます
        if (gridToggle != null)
        {
            // アニメーション付きトグルの場合、即座に反映させるため
            // WithoutNotifyではなく、普通にisOnを変えてイベントを飛ばしてOKです
            gridToggle.isOn = savedGridState; 
            
            // イベント登録（Startの最後に行う）
            gridToggle.onValueChanged.AddListener(SetGridVisibility);
        }
    }

    // --- 以下、既存の関数 ---

    public void OpenOptions()
    {
        if (optionsWindow != null) optionsWindow.SetActive(true);
    }

    public void CloseOptions()
    {
        if (optionsWindow != null) optionsWindow.SetActive(false);
    }

    // トグルが押された時に呼ばれる関数
    public void SetGridVisibility(bool isVisible)
    {
        // 1. オブジェクトの表示切り替え
        if (gridObject != null)
        {
            gridObject.SetActive(isVisible);
        }

        // 2. ▼▼▼ 設定を GameData に保存（更新）する ▼▼▼
        GameData.isGridVisible = isVisible;
    }
}