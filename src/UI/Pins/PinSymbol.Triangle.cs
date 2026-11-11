using Assets.Behaviour.Flow;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomPathFinder
{
    internal sealed partial class PinSymbol : Graphic
    {
        void Triangle(VertexHelper vh, Vector2 center, float scale, Color tint, Vector2 a, Vector2 b, Vector2 c)
        {
            int first = vh.currentVertCount;
            vh.AddVert(center + a * scale, tint, Vector2.zero); vh.AddVert(center + b * scale, tint, Vector2.zero); vh.AddVert(center + c * scale, tint, Vector2.zero);
            vh.AddTriangle(first, first + 1, first + 2);
        }
    }
}
