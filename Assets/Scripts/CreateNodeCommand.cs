// CreateNodeCommand.cs
using UnityEngine;

public class CreateNodeCommand : ICommand
{
    private GraphManager graphManager;
    private Vector3 position;
    private Node createdNode; // 作成したノードを記憶

    public CreateNodeCommand(GraphManager manager, Vector3 pos)
    {
        graphManager = manager;
        position = pos;
    }

    public void Execute()
    {
        // 初めて実行される時 (Redoではない時)
        if (createdNode == null)
        {
            GameObject newNodeObj = Object.Instantiate(graphManager.nodePrefab, position, Quaternion.identity);
            createdNode = newNodeObj.GetComponent<Node>();
            
            // CAモードなら、即座にRestingマテリアルを適用
            if (graphManager.currentSimMode == SimulationMode.CellularAutomaton)
            {
                graphManager.caManager.UpdateSingleNodeColor(createdNode);
            }
        }
        else // Redoの時
        {
            createdNode.gameObject.SetActive(true);
        }
        graphManager.Public_AddNode(createdNode);
    }

    public void Undo()
    {
        graphManager.Public_RemoveNode(createdNode);
        createdNode.gameObject.SetActive(false);
    }
}