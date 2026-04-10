using UnityEngine;
using UnityEngine.UI; // 普通のTextを使うために必要
using System.Collections.Generic;

// これをつけると Add Component > UI > Effects > Gradient で選べるようになります
[AddComponentMenu("UI/Effects/Gradient")]
public class LegacyTextGradient : BaseMeshEffect
{
    [Header("グラデーション色設定")]
    public Color colorLeft = Color.cyan;  // 左側の色
    public Color colorRight = Color.blue; // 右側の色

    // メッシュ（文字の形）が作られる時に割り込んで色を変える処理
    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        List<UIVertex> vertices = new List<UIVertex>();
        vh.GetUIVertexStream(vertices);

        int count = vertices.Count;
        if (count == 0) return;

        // 文字列全体の「左端」と「右端」の座標を探す
        float leftX = vertices[0].position.x;
        float rightX = vertices[0].position.x;

        for (int i = 1; i < count; i++)
        {
            float x = vertices[i].position.x;
            if (x < leftX) leftX = x;
            if (x > rightX) rightX = x;
        }

        float width = rightX - leftX;

        // 1文字1文字の頂点に色を塗っていく
        for (int i = 0; i < count; i++)
        {
            UIVertex v = vertices[i];

            // 現在の点が、全体の中でどの位置(0.0 〜 1.0)にあるか計算
            float t = (width > 0) ? (v.position.x - leftX) / width : 0f;

            // グラデーション色を計算
            Color newColor = Color.Lerp(colorLeft, colorRight, t);
            
            // 元の透明度(Alpha)は維持する（フェードアウト等に対応するため）
            newColor.a = (v.color.a / 255f);

            v.color = newColor;
            vertices[i] = v;
        }

        // 書き換えた頂点データを戻す
        vh.Clear();
        vh.AddUIVertexTriangleStream(vertices);
    }
}