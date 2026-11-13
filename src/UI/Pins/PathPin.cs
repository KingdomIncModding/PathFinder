using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomPathFinder
{
    internal sealed class PathPin : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        internal PathFinderPlugin Owner;
        internal UnityEngine.Behaviour Node;
        internal Button Button;
        PinSymbol symbol;
        bool hovering;
        float hoverAmount;
        internal static PathPin Create(PathFinderPlugin owner, UnityEngine.Behaviour node)
        {
            var obj = new GameObject("PathFinder.Pin", typeof(RectTransform), typeof(PinSymbol), typeof(Button), typeof(PathPin));
            obj.layer = node.gameObject.layer;
            obj.transform.SetParent(node.transform, false);
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(9, -9);
            rect.sizeDelta = new Vector2(18, 18);
            var pin = obj.GetComponent<PathPin>();
            pin.Owner = owner;
            pin.Node = node;
            pin.Button = obj.GetComponent<Button>();
            pin.symbol = obj.GetComponent<PinSymbol>();
            pin.Button.targetGraphic = pin.symbol;
            pin.Button.transition = Selectable.Transition.None;
            pin.SetPinned(false);
            pin.Button.onClick.AddListener(() =>
            {
                if (pin.Owner)
                    pin.Owner.TogglePin(node);
            });
            return pin;
        }

        void OnDestroy()
        {
            if (Owner)
                Owner.ForgetPin(Node);
        }

        void OnDisable()
        {
            hovering = false;
            hoverAmount = 0;
            if (symbol)
            {
                symbol.Hover = 0;
                symbol.Pressed = false;
                symbol.SetVerticesDirty();
            }
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left)
            {
                symbol.Pressed = true;
                symbol.SetVerticesDirty();
            }
        }

        public void OnPointerEnter(PointerEventData data) => hovering = true;
        public void OnPointerExit(PointerEventData data)
        {
            hovering = false;
            symbol.Pressed = false;
            symbol.SetVerticesDirty();
        }

        public void OnPointerUp(PointerEventData data)
        {
            symbol.Pressed = false;
            symbol.SetVerticesDirty();
        }

        internal void SetPinned(bool value)
        {
            symbol.Pinned = value;
            symbol.SetVerticesDirty();
        }

        void Update()
        {
            float next = Mathf.MoveTowards(hoverAmount, hovering ? 1 : 0, Time.unscaledDeltaTime * 9f);
            if (Mathf.Approximately(next, hoverAmount))
                return;
            hoverAmount = next;
            symbol.Hover = next;
            symbol.SetVerticesDirty();
        }
    }
}
