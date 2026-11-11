using Assets.Behaviour.Flow;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomPathFinder
{
    internal sealed partial class PinSymbol : Graphic
    {
        void FillOctagon(VertexHelper vh, Vector2 center, float scale, Color tint)
        {
            for (int i = 0; i < Octagon.Length; i++) Triangle(vh, center, scale, tint, Vector2.zero, Octagon[i], Octagon[(i + 1) % Octagon.Length]);
        }
    }
}
