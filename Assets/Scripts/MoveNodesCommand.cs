// MoveNodesCommand.cs
using UnityEngine;
using System.Collections.Generic;

public class MoveNodesCommand : ICommand
{
    private List<Node> targetNodes;
    private List<Vector3> initialPositions;
    private List<Vector3> finalPositions;
    private GraphManager graphManager;

    // コンストラクタ：移動するノード、移動前の位置、移動後の位置を受け取る
    public MoveNodesCommand(List<Node> nodes, List<Vector3> initialPos, List<Vector3> finalPos, GraphManager manager)
    {
        targetNodes = nodes;
        initialPositions = initialPos;
        finalPositions = finalPos;
        graphManager = manager;
    }

    public void Execute()
    {
        // ノードを指定された「移動後」の位置に動かす
        for (int i = 0; i < targetNodes.Count; i++)
        {
            targetNodes[i].transform.position = finalPositions[i];
            foreach (var edge in targetNodes[i].edges.Values)
            {
                edge.SetPositions();
            }
        }
    }

    public void Undo()
    {
        // ノードを保存しておいた「移動前」の位置に戻す
        for (int i = 0; i < targetNodes.Count; i++)
        {
            targetNodes[i].transform.position = initialPositions[i];
            foreach (var edge in targetNodes[i].edges.Values)
            {
                edge.SetPositions();
            }
        }
    }
}