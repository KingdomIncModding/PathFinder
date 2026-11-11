using Assets.Behaviour.Flow;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomPathFinder
{
    internal sealed partial class PathPin : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public void OnPointerDown(PointerEventData data) { if (data.button == PointerEventData.InputButton.Left) { symbol.Pressed = true; symbol.SetVerticesDirty(); } }
    }
}
