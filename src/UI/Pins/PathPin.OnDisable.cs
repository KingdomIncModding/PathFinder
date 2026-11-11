using Assets.Behaviour.Flow;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomPathFinder
{
    internal sealed partial class PathPin : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        void OnDisable() { hovering = false; hoverAmount = 0; if (symbol) { symbol.Hover = 0; symbol.Pressed = false; symbol.SetVerticesDirty(); } }
    }
}
