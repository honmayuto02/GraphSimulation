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
        // ウィンドウを初期化（閉じる）
        if (optionsWindow != null) 
            optionsWindow.SetActive(false);

        // 保存されたデータを復元する処理
        
        // GameDataから設定を読み込む
        bool savedGridState = GameData.isGridVisible;

        // グリッドオブジェクトの表示を合わせる
        if (gridObject != null)
        {
            gridObject.SetActive(savedGridState);
        }

        // トグルの見た目(ON/OFF)を合わせる
        if (gridToggle != null)
        {
            gridToggle.isOn = savedGridState; 
            
            // イベント登録（Startの最後に行う）
            gridToggle.onValueChanged.AddListener(SetGridVisibility);
        }
    }

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
        //  オブジェクトの表示切り替え
        if (gridObject != null)
        {
            gridObject.SetActive(isVisible);
        }

        // 設定を GameData に保存（更新）する
        GameData.isGridVisible = isVisible;
    }
}