// DeleteSelectionCommand.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DeleteSelectionCommand : ICommand
{
    private GraphManager graphManager;
    private List<Node> nodesToDelete;
    private List<Edge> edgesToDelete; // 実際に削除する全ての辺のリスト
    
    // コンストラクタ: 削除対象のノードと辺を受け取る
    public DeleteSelectionCommand(GraphManager manager, List<Node> nodes, List<Edge> edges)
    {
        graphManager = manager;
        nodesToDelete = new List<Node>(nodes); // コピーを作成
        
        // 削除対象の辺を決定する
        // (1) 選択されたノードに接続する全ての辺
        // (2) 選択された辺
        HashSet<Edge> edgeSet = new HashSet<Edge>(edges); // 重複を避けるためSetを使う
        foreach (Node node in nodesToDelete)
        {
            foreach (Edge edge in node.edges.Values)
            {
                edgeSet.Add(edge);
            }
        }
        edgesToDelete = edgeSet.ToList();
    }

    public void Execute()
    {
        // 辺を先に削除（無効化）する
        foreach (Edge edge in edgesToDelete)
        {
            graphManager.Public_RemoveEdge(edge);
            edge.gameObject.SetActive(false);
        }
        // 次にノードを削除（無効化）する
        foreach (Node node in nodesToDelete)
        {
            graphManager.Public_RemoveNode(node);
            node.gameObject.SetActive(false);
        }
    }

    public void Undo()
    {
        // ノードを先に復活（有効化）する
        foreach (Node node in nodesToDelete)
        {
            graphManager.Public_AddNode(node);
            node.gameObject.SetActive(true);
        }
        // 次に辺を復活（有効化）する
        foreach (Edge edge in edgesToDelete)
        {
            graphManager.Public_AddEdge(edge);
            edge.gameObject.SetActive(true);
        }
    }
}