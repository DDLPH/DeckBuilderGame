using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways, RequireComponent(typeof(CanvasRenderer))]
public sealed class CosmicSidebarShade : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); Rect r = rectTransform.rect;
        Color clear = color; clear.a = 0;
        vh.AddVert(new Vector3(r.xMin,r.yMin), color, Vector2.zero);
        vh.AddVert(new Vector3(r.xMin,r.yMax), color, Vector2.zero);
        vh.AddVert(new Vector3(r.xMax,r.yMax), clear, Vector2.zero);
        vh.AddVert(new Vector3(r.xMax,r.yMin), clear, Vector2.zero);
        vh.AddTriangle(0,1,2); vh.AddTriangle(0,2,3);
    }
}
