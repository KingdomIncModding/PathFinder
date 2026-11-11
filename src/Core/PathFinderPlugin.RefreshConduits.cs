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
        void RefreshConduits()
        {
            removedConduits.Clear();
            foreach (var entry in conduits)
                if (!desiredConduits.Contains(entry.Key) || !entry.Key.World || !entry.Key.World.isActiveAndEnabled || !entry.Value)
                    removedConduits.Add(entry.Key);
            foreach (var element in removedConduits)
            {
                var graphic = conduits[element];
                if (graphic) { graphic.enabled = false; Destroy(graphic.gameObject); }
                conduits.Remove(element);
            }
            foreach (var element in desiredConduits)
            {
                if (!element.World || !element.World.isActiveAndEnabled) continue;
                if (!conduits.TryGetValue(element, out var graphic))
                {
                    graphic = ConduitHighlight.Create(element.World, LeylineColor(element)); conduits.Add(element, graphic);
                }
                graphic.color = LeylineColor(element);
                graphic.SetWidth(widthSetting.Value);
            }
        }
    }
}
