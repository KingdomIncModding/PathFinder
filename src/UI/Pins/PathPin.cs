using Assets.Behaviour.Flow;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomPathFinder
{
    internal sealed partial class PathPin : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        internal PathFinderPlugin Owner;
        internal WorldFlowElement Node;
        internal Button Button;
        PinSymbol symbol;
        bool hovering;
        float hoverAmount;


        internal static PathPin Create(PathFinderPlugin owner, WorldFlowElement node)
        {
            var obj = new GameObject("PathFinder.Pin", typeof(RectTransform), typeof(PinSymbol), typeof(Button), typeof(PathPin));
            obj.layer = node.gameObject.layer;
            obj.transform.SetParent(node.transform, false);
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(9, -9);
            rect.sizeDelta = new Vector2(18, 18);
            var pin = obj.GetComponent<PathPin>(); pin.Owner = owner; pin.Node = node;
            pin.Button = obj.GetComponent<Button>();
            pin.symbol = obj.GetComponent<PinSymbol>();
            pin.Button.targetGraphic = pin.symbol;
            pin.Button.transition = Selectable.Transition.None;
            pin.SetPinned(false);
            pin.Button.onClick.AddListener(() => { if (pin.Owner) pin.Owner.TogglePin(node); });
            return pin;
        }
    }
}
