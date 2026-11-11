using Assets.Behaviour.Flow;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomPathFinder
{
    internal sealed partial class PathPin : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        void Update()
        {
            float next = Mathf.MoveTowards(hoverAmount, hovering ? 1 : 0, Time.unscaledDeltaTime * 9f);
            if (Mathf.Approximately(next, hoverAmount)) return;
            hoverAmount = next; symbol.Hover = next; symbol.SetVerticesDirty();
        }
    }
}
