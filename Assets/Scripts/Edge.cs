using UnityEngine;

public class Edge : MonoBehaviour
{
    public Node startNode;
    public Node endNode;
    public float length;
    public LineRenderer lineRenderer;

    private Material originalMaterial;
    private float originalWidth;

    private MeshCollider meshCollider;

    private void Awake()
    {
        // 初期状態のマテリアルと太さを記憶
        if (lineRenderer != null)
        {
            originalMaterial = lineRenderer.material;
            originalWidth = lineRenderer.startWidth;
        }

        meshCollider = GetComponent<MeshCollider>();
    }

    public void Highlight(Material highlightMaterial, float highlightWidth)
    {
        if (lineRenderer != null)
        {
            lineRenderer.material = highlightMaterial;
            lineRenderer.startWidth = highlightWidth;
            lineRenderer.endWidth = highlightWidth;
        }
    }

    public void Unhighlight()
    {
        if (lineRenderer != null && originalMaterial != null)
        {
            lineRenderer.material = originalMaterial;
            lineRenderer.startWidth = originalWidth;
            lineRenderer.endWidth = originalWidth;
        }
    }
    public void SetPositions()
    {
        lineRenderer.SetPosition(0, startNode.transform.position);
        lineRenderer.SetPosition(1, endNode.transform.position);
        length = Vector3.Distance(startNode.transform.position, endNode.transform.position);

        UpdateColliderShape();
    }
    
    private void UpdateColliderShape()
    {
        // 1. 元の見た目の太さを記憶しておく
        float visualWidth = lineRenderer.startWidth;
    
        // 2. 当たり判定用の、見えない太さを設定（この数値を大きくすると当たり判定が広がる）
        float colliderWidth = 0.5f; 
    
        // 3. 一時的にLineRendererを太くして、当たり判定用のメッシュを生成
        lineRenderer.startWidth = colliderWidth;
        lineRenderer.endWidth = colliderWidth;
        
        Mesh mesh = new Mesh();
        lineRenderer.BakeMesh(mesh, true);
        meshCollider.sharedMesh = mesh;
    
        // 4. すぐにLineRendererを元の見た目の太さに戻す
        lineRenderer.startWidth = visualWidth;
        lineRenderer.endWidth = visualWidth;
    }
}