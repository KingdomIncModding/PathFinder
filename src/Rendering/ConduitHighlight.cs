using Assets.Behaviour.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomPathFinder
{
    internal sealed partial class ConduitHighlight : MaskableGraphic
    {
        float width;

        internal static ConduitHighlight Create(WorldFlowElement node, Color tint)
        {
            var obj = new GameObject("PathFinder.Conduit", typeof(RectTransform), typeof(ConduitHighlight));
            obj.layer = node.gameObject.layer; obj.transform.SetParent(node.transform, false);
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            var graphic = obj.GetComponent<ConduitHighlight>();
            graphic.raycastTarget = false; graphic.color = tint;
            return graphic;
        }
    }
}
