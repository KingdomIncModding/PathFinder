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
        internal void Refresh(WorldFlowElement node)
        {
            if (!Unlocked) { ClearPaths(); return; }
            CollectDirectPaths(node && node.isActiveAndEnabled ? node.Element : null, desired);
            desiredConduits.Clear();
            leyline.Collect(node && node.isActiveAndEnabled ? node.Element : null, desired, desiredConduits);
            removedNodes.Clear();
            foreach (var locked in pinned)
            {
                if (!locked) { removedNodes.Add(locked); continue; }
                // Keep a pinned leyline discoverable when its partner is on another tab.
                leyline.Collect(locked.Element, desired, desiredConduits);
                if (!locked.isActiveAndEnabled) continue;
                CollectDirectPaths(locked.Element, nodePaths);
                desired.UnionWith(nodePaths);
            }
            foreach (var missing in removedNodes) pinned.Remove(missing);
            removed.Clear();
            foreach (var entry in paths)
                if (!desired.Contains(entry.Key) || !entry.Value.Alive) removed.Add(entry.Key);
            foreach (var pipe in removed) { paths[pipe].Dispose(); paths.Remove(pipe); }
            foreach (var pipe in desired)
            {
                if (!pipe.World || !pipe.World.isActiveAndEnabled) continue;
                if (!paths.TryGetValue(pipe, out var path))
                {
                    var source = pipe.World.GetComponentInChildren<LineRenderer>();
                    if (!source) continue;
                    if (!rainbowMaterial)
                    {
                        var shader = Shader.Find("Sprites/Default");
                        if (!shader) throw new InvalidOperationException("The game's unlit sprite shader is unavailable.");
                        rainbowMaterial = new Material(shader) { name = "PathFinder.NeonRainbow" };
                    }
                    path = new RainbowPath(source, rainbowMaterial, TryLeylineColor(pipe, out var tint) ? tint : ChooseColor());
                    paths.Add(pipe, path);
                }
                else if (TryLeylineColor(pipe, out var tint)) path.SetColor(tint);
            }
            visiblePaths.Clear();
            foreach (var path in paths.Values) if (path.Alive) visiblePaths.Add(path);
            laneLayout.Refresh(visiblePaths, widthSetting.Value);
            RefreshConduits();
        }
    }
}
