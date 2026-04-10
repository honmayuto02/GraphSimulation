using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEngine.UI; // Text用
using TMPro; // InputField用 (もしInputが標準ならここは不要)

public class SaveLoadManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public GameObject saveSlotPrefab;
    public Transform gridContainer;
    private const int MAX_SLOTS = 30;

    [Header("Confirmation Window UI")]
    public GameObject confirmationPanel;
    
    // ▼▼▼ 新しく追加した参照変数はここから ▼▼▼
    public Text headerTitleText;      // ウィンドウ上部の "Slot X"
    public Text lastModifiedDateText; // "2025/12/20 14:00"
    public Text fileDetailsText;      // "Nodes: 24..." (もしあれば)
    // ▲▲▲ 追加ここまで ▲▲▲

    public RawImage confirmationThumbnailImage;

    public InputField nameInputField; // または InputField
    public Button loadButton;
    public Button saveButton;
    public Button renameButton;

    private int currentSelectedSlot = -1;

    void Start()
    {
        CheckAndCopyPresets();
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        PopulateGrid();
    }

    // --- 既存のプリセット処理（省略） ---
    private void CheckAndCopyPresets() { CopyPresetToSlot(0, "preset_grid.json", "preset_grid_thumbnail.png"); }
    private void CopyPresetToSlot(int slotIndex, string presetJsonName, string presetThumbName)
    {
        // (省略: 前回のコードと同じ)
        string destDataPath = GetSavePath(slotIndex);
        if (File.Exists(destDataPath)) return;
        /* ... コピー処理 ... */
    }
    // ------------------------------------

    public void PopulateGrid()
    {
        foreach (Transform child in gridContainer) Destroy(child.gameObject);
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            GameObject slotGO = Instantiate(saveSlotPrefab, gridContainer);
            SaveSlot slot = slotGO.GetComponent<SaveSlot>();
            slot.Setup(i, this);
        }
    }

public void OnSlotClicked(int slotIndex)
    {
        currentSelectedSlot = slotIndex;
        confirmationPanel.SetActive(true);

        // 1. テキスト情報の更新
        if (headerTitleText != null) headerTitleText.text = $"Slot {slotIndex + 1}";
        
        string savedName = PlayerPrefs.GetString($"SlotName_{slotIndex}", $"Slot {slotIndex + 1}");
        if (nameInputField != null) nameInputField.text = savedName;

        string path = GetSavePath(slotIndex);
        bool hasFile = File.Exists(path);

        if (hasFile)
        {
            if (lastModifiedDateText != null) lastModifiedDateText.text = File.GetLastWriteTime(path).ToString("yyyy/MM/dd HH:mm");
            if (fileDetailsText != null) fileDetailsText.text = "Saved Graph Data";

            if (fileDetailsText != null)
            {
                try
                {
                    // JSONテキストを読み込む
                    string json = File.ReadAllText(path);

                    SerializableGraphData data = JsonUtility.FromJson<SerializableGraphData>(json);

                    if (data != null)
                    {
                        // リストの要素数を数えて表示 (nullチェック付)
                        int nodeCount = (data.nodes != null) ? data.nodes.Count : 0;
                        int edgeCount = (data.edges != null) ? data.edges.Count : 0;

                        fileDetailsText.text = $"Nodes: {nodeCount}, Edges: {edgeCount}";
                    }
                    else
                    {
                        fileDetailsText.text = "Invalid Data";
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"データの読み込みに失敗: {e.Message}");
                    fileDetailsText.text = "Data Error";
                }
            }

            // ▼▼▼ 2. サムネイル画像の読み込み処理を追加 ▼▼▼
            if (confirmationThumbnailImage != null)
            {
                string thumbPath = GetThumbnailPath(slotIndex);
                if (File.Exists(thumbPath))
                {
                    // 画像バイトデータを読み込んでテクスチャにする
                    byte[] bytes = File.ReadAllBytes(thumbPath);
                    Texture2D texture = new Texture2D(2, 2);
                    texture.LoadImage(bytes);
                    
                    confirmationThumbnailImage.texture = texture;
                    confirmationThumbnailImage.color = Color.white; // 画像があるなら白（不透明）
                }
                else
                {
                    // データはあるけど画像がない場合
                    confirmationThumbnailImage.texture = null; 
                    confirmationThumbnailImage.color = new Color(0.9f, 0.9f, 0.9f, 1f); // グレーにしておく
                }
            }
        }
        else
        {
            // ファイル自体がない場合
            if (lastModifiedDateText != null) lastModifiedDateText.text = "--/--/-- --:--";
            if (fileDetailsText != null) fileDetailsText.text = "No Data";

            // 画像をクリア
            if (confirmationThumbnailImage != null)
            {
                confirmationThumbnailImage.texture = null;
                confirmationThumbnailImage.color = new Color(0.9f, 0.9f, 0.9f, 1f); // グレー
            }
        }

        // 3. ボタン制御
        if (loadButton) loadButton.interactable = hasFile;
        if (renameButton) renameButton.interactable = hasFile;
        if (saveButton) saveButton.interactable = true;
    }

    // --- 以下、ボタン機能（変更なし） ---

    public void OnLoadPressed()
    {
        if (currentSelectedSlot == -1) return;
        if (!File.Exists(GetSavePath(currentSelectedSlot))) return;

        GameData.slotToLoad = currentSelectedSlot;
        SceneManager.LoadScene("EditorScene");
    }

    public void OnSavePressed()
    {
        if (currentSelectedSlot == -1) return;
        string tempDataPath = GetSavePath(-1);
        string tempThumbPath = GetThumbnailPath(-1);

        if (File.Exists(tempDataPath))
        {
            File.Copy(tempDataPath, GetSavePath(currentSelectedSlot), true);
            if(File.Exists(tempThumbPath)) File.Copy(tempThumbPath, GetThumbnailPath(currentSelectedSlot), true);
        }
        
        SaveSlotName();
        PopulateGrid();
        ClosePanel();
    }

    public void OnRenamePressed()
    {
        if (currentSelectedSlot == -1) return;
        SaveSlotName();
        PopulateGrid();
    }

    private void SaveSlotName()
    {
        string newName = nameInputField.text;
        if (string.IsNullOrEmpty(newName)) newName = $"Slot {currentSelectedSlot + 1}";
        PlayerPrefs.SetString($"SlotName_{currentSelectedSlot}", newName);
        PlayerPrefs.Save();
    }

    public void ClosePanel()
    {
        confirmationPanel.SetActive(false);
    }

    public string GetSlotName(int slotIndex)
    {
        return PlayerPrefs.GetString($"SlotName_{slotIndex}", $"Slot {slotIndex + 1}");
    }

    #region Helper Methods
    private string GetSavePath(int slotIndex) => Path.Combine(Application.persistentDataPath, $"graphData_slot{slotIndex}.json");
    private string GetThumbnailPath(int slotIndex) => Path.Combine(Application.persistentDataPath, $"thumbnail_slot{slotIndex}.png");
    public void GoToMenuScene() => SceneManager.LoadScene("MenuScene");
    #endregion
}