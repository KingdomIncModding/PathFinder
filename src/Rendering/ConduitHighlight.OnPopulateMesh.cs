using Assets.Behaviour.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomPathFinder
{
    internal sealed partial class ConduitHighlight : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            float x = r.xMin - 3, y = r.yMin - 3, right = r.xMax + 3, top = r.yMax + 3;
            const float cut = 6;
            var points = new[] { new Vector2(x + cut, y), new Vector2(right - cut, y), new Vector2(right, y + cut),
                new Vector2(right, top - cut), new Vector2(right - cut, top), new Vector2(x + cut, top),
                new Vector2(x, top - cut), new Vector2(x, y + cut) };
            for (int layer = 0; layer < 3; layer++)
            {
                var tint = color; tint.a *= layer == 0 ? .13f : layer == 1 ? .34f : 1f;
                float half = width * (layer == 0 ? .8f : layer == 1 ? .5f : .25f);
                for (int i = 0; i < points.Length; i++)
                {
                    var a = points[i]; var b = points[(i + 1) % points.Length];
                    var d = (b - a).normalized; var n = new Vector2(-d.y, d.x) * half;
                    int start = vh.currentVertCount;
                    vh.AddVert(a - n - d * half, tint, Vector2.zero); vh.AddVert(a + n - d * half, tint, Vector2.zero);
                    vh.AddVert(b + n + d * half, tint, Vector2.zero); vh.AddVert(b - n + d * half, tint, Vector2.zero);
                    vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
                }
            }
        }
    }
}
