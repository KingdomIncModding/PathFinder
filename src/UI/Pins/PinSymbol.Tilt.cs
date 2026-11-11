using Assets.Behaviour.Flow;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomPathFinder
{
    internal sealed partial class PinSymbol : Graphic
    {
        static Vector2 Tilt(Vector2 point, Vector2 offset)
            => new Vector2(point.x * .866f + point.y * .5f, -point.x * .5f + point.y * .866f) + offset;
    }
}
