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
        internal static void CollectDirectPaths(FlowElement node, HashSet<FlowPipe> result)
        {
            result.Clear();
            if (node == null) return;
            foreach (var input in node.Inputs) AddConnected(input.ConnectedPipe, node, result);
            foreach (var output in node.Outputs) AddConnected(output.ConnectedPipe, node, result);
        }
    }
}
