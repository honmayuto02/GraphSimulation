using UnityEngine;
using System.Collections.Generic;

// [System.Serializable] を付けることで、JsonUtilityで扱えるようになります

[System.Serializable]
public class SerializableNodeData
{
    public int id;
    public Vector3 position;
}

[System.Serializable]
public class SerializableEdgeData
{
    public int startNodeId;
    public int endNodeId;
}

[System.Serializable]
public class SerializableGraphData
{
    public List<SerializableNodeData> nodes = new List<SerializableNodeData>();
    public List<SerializableEdgeData> edges = new List<SerializableEdgeData>();
}