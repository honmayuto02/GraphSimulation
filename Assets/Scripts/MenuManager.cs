// MenuManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO; // ▼▼▼ この行を「追加」します ▼▼▼

public class MenuManager : MonoBehaviour
{
    // 「New Graph」ボタンに登録するメソッド
    public void OnNewGraphButtonClicked()
    {
        GameData.slotToLoad = -1;
        GameData.graphToPreserve = null;
        GameData.presetToLoad = null;
        SceneManager.LoadScene("EditorScene");
    }

    // ▼▼▼ 既存のOnLoadButtonClickedメソッドを、この内容に「置き換え」ます ▼▼▼
    // 「Load Graph」ボタンに登録するメソッド
    public void OnLoadButtonClicked()
    {
        // SaveLoadSceneに移動する前に、古い一時ファイル（スロット-1）を削除する。
        // これにより、「EditorSceneから保存に来たのではない」状態を明確にする。
        string tempDataPath = Path.Combine(Application.persistentDataPath, "graphData_slot-1.json");
        string tempThumbnailPath = Path.Combine(Application.persistentDataPath, "thumbnail_slot-1.png");

        if (File.Exists(tempDataPath))
        {
            File.Delete(tempDataPath);
        }
        if (File.Exists(tempThumbnailPath))
        {
            File.Delete(tempThumbnailPath);
        }
        
        // 一時ファイルをクリーンアップした後で、SaveLoadSceneをロードする
        SceneManager.LoadScene("SaveLoadScene");
    }
}