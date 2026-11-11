using Assets.Behaviour.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomPathFinder
{
    internal sealed partial class ConduitHighlight : MaskableGraphic
    {
        internal void SetWidth(float value)
        {
            if (Mathf.Approximately(width, value)) return;
            width = value; SetVerticesDirty();
        }
    }
}
