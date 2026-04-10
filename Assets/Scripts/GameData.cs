// GameData.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

public static class GameData
{   
    public static int slotToLoad = -1; 

    public static SerializableGraphData graphToPreserve = null;

    public static string presetToLoad = null; // ロードしたいプリセットファイル名を保持

    public static Vector3? cameraPosition = null; // カメラのXY移動状態とZ位置
    public static float? cameraOrthoSize = null; // Orthographicカメラのズームサイズ

    public static bool isGridVisible = true;
}