using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class WaveAgent : MonoBehaviour
{
    public float speed = 5.0f;
    public LayerMask agentLayer;

    public Node currentNode;
    public Node targetNode;
    public Edge currentEdge;

    private bool isMoving = true;

    public void Initialize(Node startNode, Edge firstEdge)
    {
        transform.position = startNode.transform.position;
        currentNode = startNode;
        currentEdge = firstEdge;
        targetNode = (firstEdge.startNode == startNode) ? firstEdge.endNode : firstEdge.startNode;
        isMoving = true;
        this.name = $"Agent_{this.GetInstanceID()}";
    }

    void Update()
    {
        if (!isMoving || targetNode == null) return;
        transform.position = Vector3.MoveTowards(transform.position, targetNode.transform.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetNode.transform.position) < 0.01f)
        {
            isMoving = false;
            currentNode = targetNode;
            StartCoroutine(HandleNodeArrivalCoroutine());
        }
    }

    private IEnumerator HandleNodeArrivalCoroutine()
    {
        // 他のAgentが到着するのを1フレーム待機
        yield return null;

        // ノード周辺の停止中Agentを全て検出
        List<WaveAgent> agentsAtNode = new List<WaveAgent>();
        Collider[] allCollidersFound = Physics.OverlapSphere(currentNode.transform.position, 0.3f, agentLayer);
        foreach (var col in allCollidersFound)
        {
            WaveAgent foundAgent = col.GetComponent<WaveAgent>();
            if (foundAgent != null && !foundAgent.isMoving)
            {
                agentsAtNode.Add(foundAgent);
            }
        }

        // Agentが1体以下なら、合流ではなく通常の分裂処理
        if (agentsAtNode.Count <= 1)
        {
            HandleSoloArrival();
            yield break;
        }

        // 複数のAgentがいる場合、代表者(Master)を決定
        WaveAgent masterAgent = agentsAtNode.OrderBy(a => a.GetInstanceID()).FirstOrDefault();

        // 自分が代表者でなければ、処理を代表者に任せる
        if (masterAgent != this)
        {
            yield break;
        }

        // --- 以下、代表者(Master)のみが実行する処理 ---

        // 全Agentの進入路を収集
        HashSet<Edge> incomingEdges = new HashSet<Edge>();
        foreach (var agent in agentsAtNode)
        {
            incomingEdges.Add(agent.currentEdge);
        }

        // 逃げ道を特定
        List<Edge> allOutgoingEdges = currentNode.edges.Values.ToList();
        List<Edge> escapeRoutes = allOutgoingEdges.Where(edge => !incomingEdges.Contains(edge)).ToList();
        
        // ルールに基づいて分岐または消滅
        if (escapeRoutes.Count == 0)
        {
            // 逃げ道なし -> 全員消滅
            foreach (var agent in agentsAtNode)
            {
                if (agent != null) Destroy(agent.gameObject);
            }
        }
        else
        {
            // 逃げ道あり -> 統合して、全ての逃げ道へ分裂
            masterAgent.Initialize(currentNode, escapeRoutes[0]);

            for (int i = 1; i < escapeRoutes.Count; i++)
            {
                GameObject newAgentObj = Instantiate(this.gameObject, currentNode.transform.position, Quaternion.identity);
                WaveAgent newAgent = newAgentObj.GetComponent<WaveAgent>();
                newAgent.Initialize(currentNode, escapeRoutes[i]);
            }

            foreach (var agent in agentsAtNode)
            {
                if (agent != masterAgent && agent != null)
                {
                    Destroy(agent.gameObject);
                }
            }
        }
    }
    
    // Agentが1体だけでノードに到着したときの通常の分裂処理
    private void HandleSoloArrival()
    {
        List<Edge> nextEdges = new List<Edge>();
        foreach (var neighbor in currentNode.neighbors)
        {
            Edge edge = currentNode.edges[neighbor];
            if (edge != currentEdge)
            {
                nextEdges.Add(edge);
            }
        }
        
        if (nextEdges.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        this.Initialize(currentNode, nextEdges[0]);

        for (int i = 1; i < nextEdges.Count; i++)
        {
            GameObject newAgentObj = Instantiate(this.gameObject, transform.position, Quaternion.identity);
            WaveAgent newAgent = newAgentObj.GetComponent<WaveAgent>();
            newAgent.Initialize(currentNode, nextEdges[i]);
        }
    }

    // 辺上での正面衝突
    void OnTriggerEnter(Collider other)
    {
        WaveAgent otherAgent = other.GetComponent<WaveAgent>();
        if (otherAgent == null || otherAgent == this) return;
        if (this.GetInstanceID() > otherAgent.GetInstanceID()) return;

        if (this.currentEdge == otherAgent.currentEdge && this.targetNode == otherAgent.currentNode && this.currentNode == otherAgent.targetNode)
        {
            Destroy(this.gameObject);
            Destroy(otherAgent.gameObject);
        }
    }
}