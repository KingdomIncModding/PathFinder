using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Behaviour.Input;
using Assets.Behaviour.UI;
using Assets.Source.Flow;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KingdomPathFinder
{
    public sealed partial class PathFinderPlugin : BaseUnityPlugin
    {
        internal WorldFlowElement FindHoveredNode(Vector2 screenPosition)
        {
            if (!EventSystem.current) return null;
            if (pointer == null || pointerSystem != EventSystem.current)
            {
                pointerSystem = EventSystem.current;
                pointer = new PointerEventData(pointerSystem);
            }
            pointer.Reset();
            pointer.position = screenPosition;
            hits.Clear();
            pointerSystem.RaycastAll(pointer, hits);
            // Respect the first UI hit: a popup must not highlight a node behind it.
            return hits.Count == 0 ? null : hits[0].gameObject.GetComponentInParent<WorldFlowElement>();
        }
    }
}
