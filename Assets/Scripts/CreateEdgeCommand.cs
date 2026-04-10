// CreateEdgeCommand.cs
using UnityEngine;

public class CreateEdgeCommand : ICommand
{
    private GraphManager graphManager;
    private Node startNode;
    private Node endNode;
    private Edge createdEdge; // 作成した辺を記憶

    public CreateEdgeCommand(GraphManager manager, Node start, Node end)
    {
        graphManager = manager;
        startNode = start;
        endNode = end;
    }

    public void Execute()
    {
        // 既存の辺がないかチェック
        if (startNode.edges.ContainsKey(endNode))
        {
            createdEdge = null; // 既に存在するので何もしない
            return;
        }
        
        if (createdEdge == null) // 初めて実行される時
        {
            GameObject edgeObj = Object.Instantiate(graphManager.edgePrefab);
            createdEdge = edgeObj.GetComponent<Edge>();
            createdEdge.startNode = startNode;
            createdEdge.endNode = endNode;
            createdEdge.SetPositions();
        }
        else // Redoの時
        {
            createdEdge.gameObject.SetActive(true);
        }
        graphManager.Public_AddEdge(createdEdge);
    }

    public void Undo()
    {
        if (createdEdge == null) return; // 何も作成されなかった場合はUndoもしない
        
        graphManager.Public_RemoveEdge(createdEdge);
        createdEdge.gameObject.SetActive(false);
    }
}