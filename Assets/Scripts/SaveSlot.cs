using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class SaveSlot : MonoBehaviour
{
    // --- 既存の変数 ---
    [Header("UI切り替え用")]
    public GameObject contentRoot;
    public GameObject emptyRoot;

    [Header("データ表示用パーツ")]
    public RawImage thumbnailImage;
    public Text dateText;
    public Text fileNameText; // ここに名前を表示します
    public Text infoText;

    [Header("内部データ")]
    private int slotIndex;
    private string savePath;
    private SaveLoadManager manager;
    private bool hasFile = false;

    public void Setup(int index, SaveLoadManager managerRef)
    {
        this.slotIndex = index;
        this.manager = managerRef;
        this.savePath = Path.Combine(Application.persistentDataPath, $"graphData_slot{index}.json");
        hasFile = File.Exists(savePath);

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (hasFile)
        {
            if(contentRoot) contentRoot.SetActive(true);
            if(emptyRoot) emptyRoot.SetActive(false);

            // サムネイル
            string thumbPath = Path.Combine(Application.persistentDataPath, $"thumbnail_slot{slotIndex}.png");
            if (File.Exists(thumbPath))
            {
                byte[] bytes = File.ReadAllBytes(thumbPath);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(bytes);
                if(thumbnailImage) thumbnailImage.texture = texture;
            }

            // 日付
            System.DateTime lastWrite = File.GetLastWriteTime(savePath);
            if(dateText) dateText.text = lastWrite.ToString("yyyy/MM/dd\nHH:mm");

            // ★変更点: Manager経由でカスタム名を取得して表示
            if(fileNameText) fileNameText.text = manager.GetSlotName(slotIndex);
            
            if (infoText)
            {
                try
                {
                    // JSONを読み込んでデータを解析
                    string json = File.ReadAllText(savePath);
                    
                    SerializableGraphData data = JsonUtility.FromJson<SerializableGraphData>(json);

                    if (data != null)
                    {
                        int nodeCount = (data.nodes != null) ? data.nodes.Count : 0;
                        int edgeCount = (data.edges != null) ? data.edges.Count : 0;

                        infoText.text = $"Nodes: {nodeCount}, Edges: {edgeCount}";
                    }
                    else
                    {
                        infoText.text = "Data Error";
                    }
                }
                catch
                {
                    // 読み込みエラー時は安全なテキストを表示
                    infoText.text = "Load Failed"; 
                }
            }
        }
        else
        {
            if(contentRoot) contentRoot.SetActive(false);
            if(emptyRoot) emptyRoot.SetActive(true);
        }
    }

    // ▼▼▼ クリック時の処理を変更 ▼▼▼
    public void OnClickSlot()
    {
        // いきなりLoadGameせず、ウィンドウを開く
        manager.OnSlotClicked(slotIndex);
    }
}