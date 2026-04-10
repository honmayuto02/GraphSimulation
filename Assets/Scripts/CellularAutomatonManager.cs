using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CellularAutomatonManager : MonoBehaviour
{
    [Header("シミュレーション設定")]
    public float stepInterval = 2.0f;

    [Header("状態のマテリアル")]
    public Material restingMaterial;
    public Material excitedMaterial;
    public Material refractoryMaterial;

    private List<Node> allNodes;
    private bool isSimulating = false;
    private Dictionary<Node, NodeState> nextStates;

    private bool isPaused = false;

    public void TogglePause()
    {
        isPaused = !isPaused;
        if (isPaused)
        {
            if (UIManager.Instance != null) UIManager.Instance.Log("CAシミュレーションを停止しました。");
        }
        else
        {
            if (UIManager.Instance != null) UIManager.Instance.Log("CAシミュレーションを再開しました。");
        }
    }

    public bool IsPaused()
    {
        return isPaused;
    }

    private IEnumerator SimulationLoop()
    {
        while (isSimulating)
        {
            // ▼▼▼ このwhileループを「追加」します ▼▼▼
            // isPausedがtrueである間、この場で無限に待機し続ける
            while (isPaused)
            {
                yield return null; // 1フレーム待機
            }

            // --- 既存の処理 ---
            CalculateNextStates();
            ApplyNextStates();
            UpdateAllNodeColors();
            yield return new WaitForSeconds(stepInterval);
        }
    }

    public void Initialize(List<Node> nodes)
    {
        allNodes = nodes;
        nextStates = new Dictionary<Node, NodeState>();
        UpdateAllNodeColors();
    }

    public void StartSimulation()
    {
        if (isSimulating) return;
        isSimulating = true;
        StartCoroutine(SimulationLoop());
        if (UIManager.Instance != null) UIManager.Instance.Log("セルオートマトンを開始しました。");
    }

    public void StopSimulation()
    {
        isSimulating = false;
        isPaused = false;
        StopAllCoroutines();
        foreach (var node in allNodes)
        {
            node.CurrentState = NodeState.Resting;
        }
        UpdateAllNodeColors();
        if (UIManager.Instance != null) UIManager.Instance.Log("シミュレーションを終了しました。");
    }

    public void ResetInitialConditions()
    {
        if (isSimulating)
        {
            if (UIManager.Instance != null) UIManager.Instance.Log("シミュレーション中は初期条件をリセットできません。");
            return;
        }
        foreach (var node in allNodes)
        {
            node.CurrentState = NodeState.Resting;
        }
        UpdateAllNodeColors();
        if (UIManager.Instance != null) UIManager.Instance.Log("初期条件をリセットしました。");
    }

    public void UpdateSingleNodeColor(Node node)
    {
        if (node != null)
        {
            node.UpdateColor(restingMaterial, excitedMaterial, refractoryMaterial);
        }
    }

    private void CalculateNextStates()
    {
        nextStates.Clear();
        foreach (var node in allNodes)
        {
            NodeState nextState = node.CurrentState;
            switch (node.CurrentState)
            {
                case NodeState.Excited:
                    nextState = NodeState.Refractory;
                    break;
                case NodeState.Refractory:
                    nextState = NodeState.Resting;
                    break;
                case NodeState.Resting:
                    if (node.neighbors.Any(neighbor => neighbor.CurrentState == NodeState.Excited))
                    {
                        nextState = NodeState.Excited;
                    }
                    break;
            }
            nextStates[node] = nextState;
        }
    }

    private void ApplyNextStates()
    {
        foreach (var node in allNodes)
        {
            node.CurrentState = nextStates[node];
        }
    }

    private void UpdateAllNodeColors()
    {
        foreach (var node in allNodes)
        {
            node.UpdateColor(restingMaterial, excitedMaterial, refractoryMaterial);
        }
    }
}