using UnityEngine;
using System.Collections.Generic;

public enum NodeState
{
    Resting,
    Excited,
    Refractory
}

public class Node : MonoBehaviour
{
    public List<Node> neighbors = new List<Node>();
    public Dictionary<Node, Edge> edges = new Dictionary<Node, Edge>();
    private Renderer nodeRenderer;
    private Material originalMaterial;

    [Header("Cellular Automaton")]
    public NodeState CurrentState = NodeState.Resting;

    private void Awake()
    {
        nodeRenderer = GetComponent<Renderer>();
        if (nodeRenderer != null)
        {
            originalMaterial = nodeRenderer.material;
        }
    }

    public void UpdateColor(Material restingMat, Material excitedMat, Material refractoryMat)
    {
        if (nodeRenderer == null) return;
        switch (CurrentState)
        {
            case NodeState.Resting:
                nodeRenderer.material = restingMat;
                break;
            case NodeState.Excited:
                nodeRenderer.material = excitedMat;
                break;
            case NodeState.Refractory:
                nodeRenderer.material = refractoryMat;
                break;
        }
        originalMaterial = nodeRenderer.material;
    }

    public void SetState(NodeState newState)
    {
        CurrentState = newState;
    }

    public void Highlight(Material highlightMaterial)
    {
        if (nodeRenderer != null)
        {
            nodeRenderer.material = highlightMaterial;
        }
    }

    public void Unhighlight()
    {
        if (nodeRenderer != null && originalMaterial != null)
        {
            nodeRenderer.material = originalMaterial;
        }
    }
}