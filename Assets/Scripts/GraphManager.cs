using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

public enum SimulationMode
{
    WaveAgent,
    CellularAutomaton
}

public class GraphManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject nodePrefab;
    public GameObject edgePrefab;
    public GameObject waveAgentPrefab;
    public GameObject indicatorArrowPrefab;

    [Header("Editor Settings")]
    public Material highlightMaterial;
    public float gridSnapSize = 1.0f;

    [Header("Thumbnail Settings")]
    public Camera thumbnailCamera;

    [Header("UI Settings")]
    public Text pauseResumeButtonText;
    public Text modeButtonText;

    [Header("Camera Settings")]
    public float minZoom = 0.1f;  // Orthographicカメラの最小サイズ（最もズームイン）
    public float maxZoom = 30f; // Orthographicカメラの最大サイズ（最もズームアウト）

    [Header("Simulation Managers")]
    public CellularAutomatonManager caManager;
    public SimulationMode currentSimMode = SimulationMode.WaveAgent;

    public enum EditorMode
    {
        NodePlacement, NodeMovement, EdgeConnection, NodeDeletion,
        SimulationSelect, // WaveAgent用
        CA_Setup,       // CellularAutomaton用
        Simulation
    }
    private EditorMode currentMode = EditorMode.NodePlacement;

    private enum CASetupMode { None, SettingExcited, SettingRefractory, SettingResting }
    private CASetupMode currentCASetupMode = CASetupMode.None;

    // --- 内部変数 ---
    private Camera mainCamera;
    private float zoomSpeed = 4.0f;
    private float panSpeed = 5.0f;
    private Vector3 panOrigin;
    private Node selectedNode;
    private Node firstSelectedNodeForEdge;
    private bool isDraggingNode = false;
    private Node currentlyHighlightedNode;
    private List<Node> allNodes = new List<Node>();
    private List<Edge> allEdges = new List<Edge>();
    public static GraphManager Instance;
    private struct InitialCondition { public Node startNode; public Edge initialEdge; }
    private List<InitialCondition> initialConditions = new List<InitialCondition>();
    private List<GameObject> indicatorObjects = new List<GameObject>();
    private enum SetupState { SelectingNode, SelectingEdge }
    private SetupState currentSetupState;
    private Node selectedNodeForSetup;
    private bool isSimulationPaused = false;

    [Header("Multi Selection")]
    private List<Node> selectedNodes = new List<Node>(); // 複数選択されたノードを保持するリスト
    private bool isSelecting = false;        // 範囲選択中かどうかを示すフラグ
    private Vector3 selectionStartPosition;  // 範囲選択の開始位置（ワールド座標）

    private bool isDraggingSelection = false; // 選択範囲をドラッグ中かどうかのフラグ
    private Vector3 lastMousePosition;       // 複数移動時のマウス座標の差分を計算するため

    private List<Vector3> initialDragPositions; // ドラッグ開始時のノード位置
    private List<Node> nodesToDrag;             // ドラッグ対象のノード

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        mainCamera = Camera.main;

        // カメラの位置(XY移動とZ軸)を復元
        if (GameData.cameraPosition.HasValue)
        {
            mainCamera.transform.position = GameData.cameraPosition.Value;
            GameData.cameraPosition = null; // 適用後にクリア
        }
        
        // Orthographicカメラのズームサイズを復元
        if (mainCamera.orthographic && GameData.cameraOrthoSize.HasValue)
        {
            mainCamera.orthographicSize = GameData.cameraOrthoSize.Value;
            GameData.cameraOrthoSize = null; // 適用後にクリア
        }

        // シーン間で維持されたグラフデータがあれば、最優先でロードする
        if (GameData.graphToPreserve != null)
        {
            LoadGraphFromData(GameData.graphToPreserve);
            GameData.graphToPreserve = null;
        }
        // なければ、通常のセーブ/ロードのデータをロードする
        else if (GameData.slotToLoad >= 0)
        {
            LoadGraph(GameData.slotToLoad);
        }
        GameData.slotToLoad = -1;

        if (caManager != null)
        {
            caManager.Initialize(allNodes);
        }
        if (modeButtonText != null)
        {
            modeButtonText.text = (currentSimMode == SimulationMode.WaveAgent) ? "CAモードへ" : "通常モードへ";
        }
    }

    private void Update()
    {
        HandleCameraControls();
        if (currentMode == EditorMode.Simulation || EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // 3. Backspaceキーが押されたら、選択中のノードをすべて削除
        if (Input.GetKeyDown(KeyCode.Backspace) && selectedNodes.Count > 0)
        {
            // 以前: DeleteNodeをループで直接実行
            // 新規: DeleteSelectionCommandに選択ノードのリストを渡す
            ICommand command = new DeleteSelectionCommand(this, new List<Node>(selectedNodes), new List<Edge>());
            UndoManager.Instance.RegisterCommand(command);
            
            ClearSelection(); // ハイライト解除
        }

        switch (currentMode)
        {
            case EditorMode.NodePlacement: HandleNodePlacement(); break;
            case EditorMode.NodeMovement: HandleNodeMovement(); break;
            case EditorMode.EdgeConnection: HandleEdgeConnection(); break;
            case EditorMode.NodeDeletion: HandleNodeDeletion(); break;
            case EditorMode.SimulationSelect: HandleSimulationSetup(); break;
            case EditorMode.CA_Setup: Handle_CA_Setup(); break;
        }

        if (isDraggingSelection)
        {
            Vector3 currentMousePosition = GetMouseWorldPosition(false);
            Vector3 mouseDelta = currentMousePosition - lastMousePosition;
    
            // 選択されているすべてのノードを、マウスの移動量と同じだけ動かす
            foreach (Node node in selectedNodes)
            {
                node.transform.position += mouseDelta;
                // ノードに接続されている辺の位置も更新
                foreach (Edge edge in node.edges.Values)
                {
                    edge.SetPositions();
                }
            }
            lastMousePosition = currentMousePosition;
        }
    }

    #region Public UI Methods
    public void SetMode(int modeIndex)
    {
        currentMode = (EditorMode)modeIndex;
        if (currentMode != EditorMode.CA_Setup)
        {
            currentCASetupMode = CASetupMode.None;
        }
        firstSelectedNodeForEdge = null;
        isDraggingNode = false;
        UnhighlightNode();
        ClearSelection();
        UIManager.Instance.Log(currentMode.ToString() + "モードに変更しました。");
    }

    public void OnSaveMenuButtonClicked()
    {
        SaveGraph(-1);

        // SaveLoadSceneに移動する際も、カメラ状態を保存する
        GameData.cameraPosition = mainCamera.transform.position;
        if (mainCamera.orthographic)
        {
            GameData.cameraOrthoSize = mainCamera.orthographicSize;
        }
        
        SceneManager.LoadScene("SaveLoadScene");
    }

    public void GoToMenuScene()
    {
        SceneManager.LoadScene("MenuScene");
    }

    public void ToggleSimulationMode()
    {
        GameData.graphToPreserve = CaptureCurrentGraph();

        // シーンを切り替える直前に、現在のカメラ状態を保存
        GameData.cameraPosition = mainCamera.transform.position;
        if (mainCamera.orthographic)
        {
            GameData.cameraOrthoSize = mainCamera.orthographicSize;
        }

        if (currentSimMode == SimulationMode.WaveAgent)
        {
            SceneManager.LoadScene("CAScene");
        }
        else
        {
            SceneManager.LoadScene("EditorScene");
        }
    }

    public void EnterCASetup_Excited()
    {
        SetMode((int)EditorMode.CA_Setup);
        currentCASetupMode = CASetupMode.SettingExcited;
        UIManager.Instance.Log("興奮状態のセルを選択してください。");
    }

    public void EnterCASetup_Refractory()
    {
        SetMode((int)EditorMode.CA_Setup);
        currentCASetupMode = CASetupMode.SettingRefractory;
        UIManager.Instance.Log("不応状態のセルを選択してください。");
    }

        public void EnterCASetup_Resting()
    {
        SetMode((int)EditorMode.CA_Setup);
        currentCASetupMode = CASetupMode.SettingResting;
        UIManager.Instance.Log("静止状態のセルを選択してください。");
    }

    public void EnterSimulationSetup()
    {
        SetMode((int)EditorMode.SimulationSelect);
        initialConditions.Clear();
        ResetSetupSelection();
        UIManager.Instance.Log("開始するノードを選択してください。");
    }

    public void StartConfiguredSimulation()
    {
        if (currentSimMode == SimulationMode.CellularAutomaton)
        {
            if (caManager != null)
            {
                SetMode((int)EditorMode.Simulation);
                caManager.StartSimulation();
            }
            return;
        }

        if (initialConditions.Count == 0)
        {
            UIManager.Instance.Log("注意：初期条件がありません。");
            return;
        }
        int agentCount = initialConditions.Count;
        foreach (var condition in initialConditions)
        {
            GameObject agentObj = Instantiate(waveAgentPrefab, condition.startNode.transform.position, Quaternion.identity);
            agentObj.GetComponent<WaveAgent>().Initialize(condition.startNode, condition.initialEdge);
        }
        initialConditions.Clear();
        ClearAllIndicators();
        isSimulationPaused = false;
        if (pauseResumeButtonText != null) pauseResumeButtonText.text = "停止";
        SetMode((int)EditorMode.Simulation);
        UIManager.Instance.Log("シミュレーションを開始しました。");
    }

    public void StopSimulation()
    {
        if (currentSimMode == SimulationMode.CellularAutomaton)
        {
            if (caManager != null)
            {
                caManager.StopSimulation();
            }
            // ▼▼▼ このif文ブロックを追加 ▼▼▼
            // ボタンのテキストを初期状態の「Pause」に戻す
            if (pauseResumeButtonText != null)
            {
                pauseResumeButtonText.text = "停止";
            }
            SetMode((int)EditorMode.NodePlacement);
            return;
        }
        else // WaveAgentモードの場合
        {
            WaveAgent[] agents = FindObjectsOfType<WaveAgent>();
            foreach (WaveAgent agent in agents)
            {
                Destroy(agent.gameObject);
            }
            isSimulationPaused = false;
            if (pauseResumeButtonText != null)
            {
                pauseResumeButtonText.text = "停止"; // WaveAgent側にも同様の処理
            }
            UIManager.Instance.Log("シミュレーションを停止しました。");
            SetMode((int)EditorMode.NodePlacement);
        }
    }
    
    public void ClearInitialConditions()
    {
        if (currentSimMode == SimulationMode.CellularAutomaton)
        {
            if (caManager != null)
            {
                caManager.ResetInitialConditions();
            }
        }
        else
        {
            initialConditions.Clear();
            ClearAllIndicators();
            ResetSetupSelection();
            UIManager.Instance.Log("初期条件をリセットしました。");
        }
    }
    
    public void TogglePauseSimulation()
    {
        // 現在のモードに応じて処理を振り分ける
        if (currentSimMode == SimulationMode.CellularAutomaton)
        {
            // CAモードの場合
            if (caManager != null)
            {
                // 1. CAマネージャーに一時停止/再開を命令する
                caManager.TogglePause();
    
                // 2. CAマネージャーから現在の状態を取得し、ボタンのテキストを更新する
                if (caManager.IsPaused())
                {
                    if (pauseResumeButtonText != null) pauseResumeButtonText.text = "再開";
                }
                else
                {
                    if (pauseResumeButtonText != null) pauseResumeButtonText.text = "停止";
                }
            }
        }
        else // WaveAgentモードの場合
        {
            isSimulationPaused = !isSimulationPaused;
            WaveAgent[] agents = FindObjectsOfType<WaveAgent>();
            foreach (WaveAgent agent in agents)
            {
                agent.enabled = !isSimulationPaused;
            }
    
            if (isSimulationPaused)
            {
                if (pauseResumeButtonText != null) pauseResumeButtonText.text = "再開";
                UIManager.Instance.Log("シミュレーションを一時停止しました。");
            }
            else
            {
                if (pauseResumeButtonText != null) pauseResumeButtonText.text = "停止";
                UIManager.Instance.Log("シミュレーションを再開しました。");
            }
        }
    }
    #endregion

    #region Graph Editing & Setup Methods
    private void HandleNodePlacement()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 potentialPosition = GetMouseWorldPosition(true); // グリッドにスナップ
            bool isOccupied = allNodes.Any(node => Vector3.Distance(node.transform.position, potentialPosition) < 0.1f);
    
            if (!isOccupied)
            {
                // ▼▼▼ 変更点 ▼▼▼
                // 以前: Instantiateを直接実行
                // 新規: CreateNodeCommandを作成してUndoManagerに登録
                ICommand command = new CreateNodeCommand(this, potentialPosition);
                UndoManager.Instance.RegisterCommand(command);
            }
            else
            {
                UIManager.Instance.Log("この位置には既にノードが存在します。");
            }
        }
    }


    private void HandleNodeMovement()
    {
        // --- 1. マウスの左ボタンが押された瞬間の処理 ---
        if (Input.GetMouseButtonDown(0))
        {
            Node hitNode = GetNodeAtMousePosition();
    
            if (hitNode != null) // --- Case 1: ノードをクリックした場合 ---
            {
                if (selectedNodes.Contains(hitNode)) // 1a. 「選択済み」のノードをクリック
                {
                    // 複数ドラッグ移動を開始
                    isDraggingSelection = true;
                    lastMousePosition = GetMouseWorldPosition(false);
                    nodesToDrag = new List<Node>(selectedNodes); // 移動対象を確定
                    initialDragPositions = new List<Vector3>();
                    foreach (Node node in nodesToDrag)
                    {
                        initialDragPositions.Add(node.transform.position); // 移動前の位置を記録
                    }
                }
                else // 1b. 「未選択」のノードをクリック
                {
                    // これから単一ノードをドラッグする
                    ClearSelection(); // 他の選択を解除
                    
                    // 従来の単一ノードドラッグロジック
                    selectedNode = hitNode;
                    isDraggingNode = true; // 単一ドラッグフラグを立てる
                    HighlightNode(selectedNode);
                    nodesToDrag = new List<Node> { selectedNode }; // 移動対象を確定
                    initialDragPositions = new List<Vector3> { selectedNode.transform.position }; // 移動前の位置を記録
                }
            }
            else // --- Case 2: 何もない場所をクリックした場合 ---
            {
                // 範囲選択を開始
                isSelecting = true;
                selectionStartPosition = GetMouseWorldPosition(false);
                ClearSelection(); 
            }
        }
    
        // --- 2. マウスの左ボタンが離された瞬間の処理 ---
        if (Input.GetMouseButtonUp(0))
        {
            // 2a. 範囲選択の終了処理
            if (isSelecting)
            {
                SelectNodesInRect(selectionStartPosition, GetMouseWorldPosition(false));
                isSelecting = false;
            }
    
            if (isDraggingSelection || isDraggingNode)
            {
                List<Vector3> finalPositions = new List<Vector3>();
                foreach (Node node in nodesToDrag)
                {
                    Vector3 snappedPos = SnapToGrid(node.transform.position);
                    node.transform.position = snappedPos; // 最後にグリッドにスナップ
                    finalPositions.Add(snappedPos); // 移動後の位置を記録
                    foreach (Edge edge in node.edges.Values) edge.SetPositions();
                }
    
                // MoveNodesCommandを作成してUndoManagerに登録
                ICommand moveCommand = new MoveNodesCommand(nodesToDrag, initialDragPositions, finalPositions, this);
                UndoManager.Instance.RegisterCommand(moveCommand);
            }

            // 共通の終了処理
            isSelecting = false;
            isDraggingSelection = false;
            if (isDraggingNode)
            {
                isDraggingNode = false;
                UnhighlightNode();
                selectedNode = null;
            }
        }
    
        // --- 3. ドラッグ中の処理 (フレームごと) ---
        
        // 3a. 複数ノードのドラッグ中
        if (isDraggingSelection)
        {
            Vector3 currentMousePosition = GetMouseWorldPosition(false);
            Vector3 mouseDelta = currentMousePosition - lastMousePosition;
            foreach (Node node in selectedNodes)
            {
                node.transform.position += mouseDelta;
                foreach (Edge edge in node.edges.Values)
                {
                    edge.SetPositions();
                }
            }
            lastMousePosition = currentMousePosition;
        }
        // 3b. 単一ノードのドラッグ中
        else if (isDraggingNode && selectedNode != null)
        {
            // 単一ドラッグはグリッドにスナップしながら動かす (元のロジック)
            selectedNode.transform.position = GetMouseWorldPosition(true); // Snap to grid
            foreach (Edge edge in selectedNode.edges.Values)
            {
                edge.SetPositions();
            }
        }
    }

    private void HandleEdgeConnection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Node hitNode = GetNodeAtMousePosition();
            if (hitNode != null)
            {
                if (firstSelectedNodeForEdge == null)
                {
                    firstSelectedNodeForEdge = hitNode;
                    HighlightNode(hitNode);
                }
                else if (firstSelectedNodeForEdge != hitNode)
                {
                    // ▼▼▼ 変更点 ▼▼▼
                    // 以前: CreateEdge(start, end) を直接実行
                    // 新規: CreateEdgeCommandを作成してUndoManagerに登録
                    ICommand command = new CreateEdgeCommand(this, firstSelectedNodeForEdge, hitNode);
                    UndoManager.Instance.RegisterCommand(command);
                    
                    UnhighlightNode();
                    firstSelectedNodeForEdge = null;
                }
            }
            else
            {
                UnhighlightNode();
                firstSelectedNodeForEdge = null;
            }
        }
    }

    private void HandleNodeDeletion()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Node hitNode = hit.collider.GetComponent<Node>();
                if (hitNode != null)
                {
                    // ▼▼▼ 変更点 ▼▼▼
                    // 以前: DeleteNode(hitNode) を直接実行
                    // 新規: DeleteSelectionCommandを作成してUndoManagerに登録
                    ICommand command = new DeleteSelectionCommand(this, new List<Node> { hitNode }, new List<Edge>());
                    UndoManager.Instance.RegisterCommand(command);
                    return;
                }
    
                Edge hitEdge = hit.collider.GetComponent<Edge>();
                if (hitEdge != null)
                {
                    // ▼▼▼ 変更点 ▼▼▼
                    // 以前: DeleteEdge(hitEdge) を直接実行
                    // 新規: DeleteSelectionCommandを作成してUndoManagerに登録
                    ICommand command = new DeleteSelectionCommand(this, new List<Node>(), new List<Edge> { hitEdge });
                    UndoManager.Instance.RegisterCommand(command);
                    return;
                }
            }
        }
    }

    private void HandleSimulationSetup()
    {
        if (currentSetupState == SetupState.SelectingNode)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Node hitNode = GetNodeAtMousePosition();
                if (hitNode != null && hitNode.neighbors.Count > 0)
                {
                    selectedNodeForSetup = hitNode;
                    HighlightNode(selectedNodeForSetup);
                    foreach (Edge edge in selectedNodeForSetup.edges.Values)
                    {
                        edge.Highlight(highlightMaterial, 0.2f);
                    }
                    currentSetupState = SetupState.SelectingEdge;
                }
            }
        }
        else if (currentSetupState == SetupState.SelectingEdge)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    Edge hitEdge = hit.collider.GetComponent<Edge>();
                    if (hitEdge != null && selectedNodeForSetup.edges.ContainsValue(hitEdge))
                    {
                        initialConditions.Add(new InitialCondition { startNode = selectedNodeForSetup, initialEdge = hitEdge });
                        Debug.Log($"初期条件を追加: {initialConditions.Count}個目");
                        UIManager.Instance.Log($"初期条件を追加しました。({initialConditions.Count}個目)");

                        Node targetNode = (hitEdge.startNode == selectedNodeForSetup) ? hitEdge.endNode : hitEdge.startNode;
                        Vector3 direction = (targetNode.transform.position - selectedNodeForSetup.transform.position).normalized;
                        GameObject arrow = Instantiate(indicatorArrowPrefab);
                        indicatorObjects.Add(arrow);
                        float nodeRadius = 0.5f;
                        arrow.transform.position = selectedNodeForSetup.transform.position + direction * nodeRadius;
                        arrow.transform.rotation = Quaternion.LookRotation(direction);

                        ResetSetupSelection();
                        return;
                    }
                }
                ResetSetupSelection();
            }
        }
    }

    private void ResetSetupSelection()
    {
        UnhighlightNode();
        foreach (Edge edge in allEdges)
        {
            edge.Unhighlight();
        }
        selectedNodeForSetup = null;
        currentSetupState = SetupState.SelectingNode;
    }

    private void ClearAllIndicators()
    {
        foreach (GameObject indicator in indicatorObjects)
        {
            Destroy(indicator);
        }
        indicatorObjects.Clear();
    }
    
    private void Handle_CA_Setup()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Node hitNode = GetNodeAtMousePosition();
            if (hitNode != null && caManager != null)
            {
                if (currentCASetupMode == CASetupMode.SettingExcited)
                {
                    hitNode.SetState(NodeState.Excited);
                }
                else if (currentCASetupMode == CASetupMode.SettingRefractory)
                {
                    hitNode.SetState(NodeState.Refractory);
                }
                else if (currentCASetupMode == CASetupMode.SettingResting)
                {
                    hitNode.SetState(NodeState.Resting);
                }
                caManager.UpdateSingleNodeColor(hitNode);
            }
        }
    }
    #endregion

    #region Save, Load, and Helper Methods
    private SerializableGraphData CaptureCurrentGraph()
    {
        SerializableGraphData graphData = new SerializableGraphData();
        Dictionary<Node, int> nodeToIndexMap = new Dictionary<Node, int>();
        for (int i = 0; i < allNodes.Count; i++)
        {
            Node node = allNodes[i];
            graphData.nodes.Add(new SerializableNodeData { id = i, position = node.transform.position });
            nodeToIndexMap.Add(node, i);
        }
        foreach (Edge edge in allEdges)
        {
            graphData.edges.Add(new SerializableEdgeData { startNodeId = nodeToIndexMap[edge.startNode], endNodeId = nodeToIndexMap[edge.endNode] });
        }
        return graphData;
    }

    private void LoadGraphFromData(SerializableGraphData graphData)
    {
        ClearGraph();
        Dictionary<int, Node> indexToNodeMap = new Dictionary<int, Node>();
        foreach (var nodeData in graphData.nodes)
        {
            GameObject newNodeObj = Instantiate(nodePrefab, nodeData.position, Quaternion.identity);
            Node newNode = newNodeObj.GetComponent<Node>();
            allNodes.Add(newNode);
            indexToNodeMap.Add(nodeData.id, newNode);
        }
        foreach (var edgeData in graphData.edges)
        {
            Node startNode = indexToNodeMap[edgeData.startNodeId];
            Node endNode = indexToNodeMap[edgeData.endNodeId];
            CreateEdge(startNode, endNode);
        }
        Debug.Log("一時データからグラフを復元しました。");
    }

    private void SaveGraph(int slotIndex)
    {
        SerializableGraphData graphData = new SerializableGraphData();
        Dictionary<Node, int> nodeToIndexMap = new Dictionary<Node, int>();

        for (int i = 0; i < allNodes.Count; i++)
        {
            Node node = allNodes[i];
            graphData.nodes.Add(new SerializableNodeData { id = i, position = node.transform.position });
            nodeToIndexMap.Add(node, i);
        }
        foreach (Edge edge in allEdges)
        {
            graphData.edges.Add(new SerializableEdgeData
            {
                startNodeId = nodeToIndexMap[edge.startNode],
                endNodeId = nodeToIndexMap[edge.endNode]
            });
        }

        string json = JsonUtility.ToJson(graphData, true);
        string path = GetSavePath(slotIndex);
        File.WriteAllText(path, json);
        CaptureAndSaveThumbnail(slotIndex);

        if (slotIndex == -1)
        {
            Debug.Log($"グラフを一時ファイルとしてセーブしました: {path}");
            UIManager.Instance.Log($"グラフをセーブしました : {path}");
        }
        else
        {
            Debug.Log($"グラフをスロット {slotIndex + 1} にセーブしました: {path}");
            UIManager.Instance.Log($"グラフをスロット {slotIndex + 1}にセーブしました。 : {path}");
        }
    }

    private void LoadGraph(int slotIndex)
    {
        string path = GetSavePath(slotIndex);
        if (!File.Exists(path)) return;

        string json = File.ReadAllText(path);
        SerializableGraphData graphData = JsonUtility.FromJson<SerializableGraphData>(json);
        ClearGraph();

        Dictionary<int, Node> indexToNodeMap = new Dictionary<int, Node>();
        foreach (var nodeData in graphData.nodes)
        {
            GameObject newNodeObj = Instantiate(nodePrefab, nodeData.position, Quaternion.identity);
            Node newNode = newNodeObj.GetComponent<Node>();
            allNodes.Add(newNode);
            indexToNodeMap.Add(nodeData.id, newNode);
        }
        foreach (var edgeData in graphData.edges)
        {
            Node startNode = indexToNodeMap[edgeData.startNodeId];
            Node endNode = indexToNodeMap[edgeData.endNodeId];
            CreateEdge(startNode, endNode);
        }
        Debug.Log($"スロット {slotIndex + 1} からグラフをロードしました。");
        UIManager.Instance.Log($"スロット{slotIndex + 1}からグラフをロードしました。");
    }

    private void CaptureAndSaveThumbnail(int slotIndex)
    {
        if (thumbnailCamera == null)
        {
            Debug.LogError("Thumbnail Cameraが設定されていません。");
            return;
        }
        if (allNodes.Count == 0) return;
    
        // 1. グラフの中心を計算 (これは必要)
        Bounds bounds = new Bounds(allNodes[0].transform.position, Vector3.zero);
        foreach (Node node in allNodes)
        {
            bounds.Encapsulate(node.transform.position);
        }
    
        thumbnailCamera.gameObject.SetActive(true);
    
        // ▼▼▼ 修正箇所 ▼▼▼
        // 2. mainCamera（ユーザーのカメラ）のズーム設定をthumbnailCameraにコピーする
        if (mainCamera.orthographic)
        {
            thumbnailCamera.orthographic = true;
            
            // mainCameraの現在のズームレベル(orthographicSize)をそのままコピー
            thumbnailCamera.orthographicSize = mainCamera.orthographicSize;
            
            // カメラの位置はグラフの中心に合わせ、Zは固定値（-10など）にする
            thumbnailCamera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
        }
        else // mainCameraがPerspectiveの場合
        {
            thumbnailCamera.orthographic = false;
            thumbnailCamera.fieldOfView = mainCamera.fieldOfView; // FOVもコピー
            
            // mainCameraのZ位置(ズームレベル)をコピーし、XYはグラフの中心に合わせる
            thumbnailCamera.transform.position = new Vector3(bounds.center.x, bounds.center.y, mainCamera.transform.position.z);
        }
        // ▲▲▲ 修正ここまで ▲▲▲
    
        // --- 既存の自動計算ロジックは上記に置き換えられたため不要 ---
        // float graphSize = Mathf.Max(bounds.size.x, bounds.size.y);
        // ...
        // thumbnailCamera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -finalDistance);
    
        // 3. レンダリング処理 (変更なし)
        RenderTexture rt = new RenderTexture(256, 256, 24);
        thumbnailCamera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(256, 256, TextureFormat.RGB24, false);
        thumbnailCamera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
        screenShot.Apply();
        thumbnailCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);
        thumbnailCamera.gameObject.SetActive(false);
    
        byte[] bytes = screenShot.EncodeToPNG();
        string path = GetThumbnailPath(slotIndex);
        File.WriteAllBytes(path, bytes);
    }

    private void ClearGraph()
    {
        foreach (Edge edge in allEdges) Destroy(edge.gameObject);
        allEdges.Clear();
        foreach (Node node in allNodes) Destroy(node.gameObject);
        allNodes.Clear();
    }

    private void CreateEdge(Node start, Node end)
    {
        if (start.edges.ContainsKey(end)) return;
        GameObject edgeObj = Instantiate(edgePrefab);
        Edge edge = edgeObj.GetComponent<Edge>();
        edge.startNode = start;
        edge.endNode = end;
        edge.SetPositions();
        start.neighbors.Add(end);
        start.edges.Add(end, edge);
        end.neighbors.Add(start);
        end.edges.Add(start, edge);
        allEdges.Add(edge);
    }

    private string GetSavePath(int slotIndex) => Path.Combine(Application.persistentDataPath, $"graphData_slot{slotIndex}.json");
    private string GetThumbnailPath(int slotIndex) => Path.Combine(Application.persistentDataPath, $"thumbnail_slot{slotIndex}.png");

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10.0f;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.x = Mathf.Round(worldPos.x / gridSnapSize) * gridSnapSize;
        worldPos.y = Mathf.Round(worldPos.y / gridSnapSize) * gridSnapSize;
        worldPos.z = 0;
        return worldPos;
    }

    private Node GetNodeAtMousePosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit)) return hit.collider.GetComponent<Node>();
        return null;
    }

    private void HighlightNode(Node nodeToHighlight)
    {
        UnhighlightNode();
        currentlyHighlightedNode = nodeToHighlight;
        currentlyHighlightedNode.Highlight(highlightMaterial);
    }

    private void UnhighlightNode()
    {
        if (currentlyHighlightedNode != null)
        {
            currentlyHighlightedNode.Unhighlight();
            currentlyHighlightedNode = null;
        }
    }

    // --- 【修正版】単発クリック用のズーム処理 ---

    // 1回のクリックで動く量（お好みで調整してください）
    public float clickZoomStep = 2.0f; 

    // +ボタンのOnClickに割り当てる
    public void ClickZoomIn()
    {
        ApplyZoomStep(1.0f); // ズームイン方向
    }

    // -ボタンのOnClickに割り当てる
    public void ClickZoomOut()
    {
        ApplyZoomStep(-1.0f); // ズームアウト方向
    }

    // 実際の処理を行う内部関数
    private void ApplyZoomStep(float direction)
    {
        // Ortho（2Dモード）の場合
        if (mainCamera.orthographic)
        {
            // サイズを減らす = ズームイン
            mainCamera.orthographicSize -= direction * clickZoomStep;
            mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize, minZoom, maxZoom);
        }
        // Perspective（3Dモード）の場合
        else
        {
            Vector3 pos = mainCamera.transform.position;
            // Z座標を増やす = ズームイン（カメラが対象に近づく）
            // ※カメラの向きや位置によっては符号（+/-）が逆になる場合があります
            pos.z += direction * clickZoomStep;
            mainCamera.transform.position = pos;
        }
    }

    private void HandleCameraControls()
    {
        // ▼▼▼ 追加: UIの上にマウスがある時は、ホイール入力を無視する ▼▼▼
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0.0f)
            {
                // カメラのProjectionがOrthographicかPerspectiveかで処理を分ける
                if (mainCamera.orthographic)
                {
                    // Orthographicの場合：Sizeを変更
                    mainCamera.orthographicSize -= scroll * zoomSpeed;
                    mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize, minZoom, maxZoom);
                }
                else
                {
                    // Perspectiveの場合：Z軸の位置を変更
                    Vector3 pos = mainCamera.transform.position;
                    pos.z += scroll * zoomSpeed;
                    mainCamera.transform.position = pos;
                }
            }
        }

        float zoomDelta = 0;
        if (Input.GetKey(KeyCode.Minus)) // =(イコール)キーでズームイン
        {
            zoomDelta = -1.0f;
        }
        if (Input.GetKey(KeyCode.Caret)) // ^(キャレット)キーでズームアウト
        {
            zoomDelta = 1.0f;
        }

        if (zoomDelta != 0)
        {
            if (mainCamera.orthographic)
            {
                mainCamera.orthographicSize += zoomDelta * zoomSpeed * Time.deltaTime;
            }
            else
            {
                Vector3 pos = mainCamera.transform.position;
                pos.z -= zoomDelta * zoomSpeed * Time.deltaTime;
                mainCamera.transform.position = pos;
            }
        }

        Vector3 panMove = Vector3.zero;

        // --- パン（画面移動）処理 ---
        // ホイールボタンが押された瞬間
        if (Input.GetMouseButtonDown(2))
        {
            panOrigin = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        }

        // ホイールボタンが押されている間
        if (Input.GetMouseButton(2))
        {
            Vector3 difference = panOrigin - mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mainCamera.transform.position += difference;
        }

        Vector3 keyboardMove = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) { keyboardMove.y += 1; }
        if (Input.GetKey(KeyCode.S)) { keyboardMove.y -= 1; }
        if (Input.GetKey(KeyCode.A)) { keyboardMove.x -= 1; }
        if (Input.GetKey(KeyCode.D)) { keyboardMove.x += 1; }

        // キーボード入力があれば、マウスのパンを上書き
        if (keyboardMove != Vector3.zero)
        {
            // ズームアウトしているほど速く動くように調整
            float panMultiplier = mainCamera.orthographic ? mainCamera.orthographicSize : Mathf.Abs(mainCamera.transform.position.z);
            panMove = keyboardMove.normalized * (panSpeed / 10f) * panMultiplier * Time.deltaTime;
        }
        
        mainCamera.transform.position += panMove;
    }

    public void Public_AddNode(Node node)
    {
        if (!allNodes.Contains(node))
        {
            allNodes.Add(node);
        }
    }

    // 【コマンド専用】ノードをリストから削除する（Destroyしない）
    public void Public_RemoveNode(Node node)
    {
        if (allNodes.Contains(node))
        {
            allNodes.Remove(node);
        }
    }

    // 【コマンド専用】辺をリストとノードに追加する
    public void Public_AddEdge(Edge edge)
    {
        if (!allEdges.Contains(edge))
        {
            allEdges.Add(edge);
            edge.startNode.neighbors.Add(edge.endNode);
            edge.startNode.edges.Add(edge.endNode, edge);
            edge.endNode.neighbors.Add(edge.startNode);
            edge.endNode.edges.Add(edge.startNode, edge);
        }
    }

    // 【コマンド専用】辺をリストとノードから削除する（Destroyしない）
    public void Public_RemoveEdge(Edge edge)
    {
        if (allEdges.Contains(edge))
        {
            allEdges.Remove(edge);
            edge.startNode.neighbors.Remove(edge.endNode);
            edge.startNode.edges.Remove(edge.endNode);
            edge.endNode.neighbors.Remove(edge.startNode);
            edge.endNode.edges.Remove(edge.startNode);
        }
    }

    #endregion

    // OnGUIはUpdateとは別に、GUI描画イベントのたびに呼ばれます
    private void OnGUI()
    {
        // 範囲選択中なら、矩形を描画する
        if (isSelecting)
        {
            // ワールド座標からスクリーン座標に変換
            var startScreenPos = mainCamera.WorldToScreenPoint(selectionStartPosition);
            var currentScreenPos = mainCamera.WorldToScreenPoint(GetMouseWorldPosition(false));
    
            // GUIで矩形を描画
            var rect = new Rect(startScreenPos.x, Screen.height - startScreenPos.y,
                                currentScreenPos.x - startScreenPos.x, - (currentScreenPos.y - startScreenPos.y));
            
            GUI.Box(rect, "");
        }
    }
    

    #region Multi Selection Helper Methods

    private Vector3 SnapToGrid(Vector3 position)
    {
        position.x = Mathf.Round(position.x / gridSnapSize) * gridSnapSize;
        position.y = Mathf.Round(position.y / gridSnapSize) * gridSnapSize;
        position.z = 0;
        return position;
    }
    
    // 既存のGetMouseWorldPositionメソッドを、新しいヘルパーを使うように修正
    private Vector3 GetMouseWorldPosition(bool snapToGrid = true)
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = mainCamera.nearClipPlane + 10;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
    
        if (snapToGrid)
        {
            worldPos = SnapToGrid(worldPos); // 既存のロジックをヘルパー呼び出しに置き換え
        }
        worldPos.z = 0;
        return worldPos;
    }
    
    // 選択をすべて解除する
    private void ClearSelection()
    {
        foreach (Node node in selectedNodes)
        {
            node.Unhighlight(); // ハイライトを解除
        }
        selectedNodes.Clear();
    }
    
    // 指定された矩形内のノードをすべて選択する
    private void SelectNodesInRect(Vector3 startPos, Vector3 endPos)
    {
        ClearSelection();
    
        Rect selectionRect = new Rect(
            Mathf.Min(startPos.x, endPos.x),
            Mathf.Min(startPos.y, endPos.y),
            Mathf.Abs(startPos.x - endPos.x),
            Mathf.Abs(startPos.y - endPos.y)
        );
    
        foreach (Node node in allNodes)
        {
            // ノードの位置が矩形内に含まれていれば、選択リストに追加
            if (selectionRect.Contains(node.transform.position))
            {
                selectedNodes.Add(node);
                node.Highlight(highlightMaterial); // 選択されたノードをハイライト
            }
        }
    }
    
    
    #endregion
}