using Assets.Behaviour.Flow;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomPathFinder
{
    internal sealed partial class PinSymbol : Graphic
    {
        void DrawTack(VertexHelper vh, Vector2 center, float scale, Vector2 offset, Color tint, bool highlight)
        {
            Quad(vh, center, scale, tint, Tilt(new Vector2(-3.4f, 4.4f), offset), Tilt(new Vector2(3.4f, 4.4f), offset), Tilt(new Vector2(3.1f, 2.7f), offset), Tilt(new Vector2(-3.1f, 2.7f), offset));
            Quad(vh, center, scale, tint, Tilt(new Vector2(-1.2f, 2.7f), offset), Tilt(new Vector2(1.2f, 2.7f), offset), Tilt(new Vector2(1.7f, -.5f), offset), Tilt(new Vector2(-1.7f, -.5f), offset));
            Quad(vh, center, scale, tint, Tilt(new Vector2(-3.2f, -.5f), offset), Tilt(new Vector2(3.2f, -.5f), offset), Tilt(new Vector2(2.5f, -1.8f), offset), Tilt(new Vector2(-2.5f, -1.8f), offset));
            Triangle(vh, center, scale, tint, Tilt(new Vector2(-.7f, -1.8f), offset), Tilt(new Vector2(.7f, -1.8f), offset), Tilt(new Vector2(0, -6), offset));
            if (highlight)
                Quad(vh, center, scale, Color.Lerp(tint, Color.white, .65f), Tilt(new Vector2(-2.8f, 4.4f), offset), Tilt(new Vector2(2.8f, 4.4f), offset), Tilt(new Vector2(2.8f, 3.8f), offset), Tilt(new Vector2(-2.8f, 3.8f), offset));
        }
    }
}
