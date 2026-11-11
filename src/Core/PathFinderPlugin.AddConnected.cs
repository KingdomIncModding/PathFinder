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
        internal static void AddConnected(FlowPipe pipe, FlowElement node, HashSet<FlowPipe> result)
        {
            if (pipe?.ConnectedInput == null || pipe.ConnectedOutput == null) return;
            if (pipe.ConnectedInput.ConnectedPipe != pipe || pipe.ConnectedOutput.ConnectedPipe != pipe) return;
            if (pipe.ConnectedInput.Parent == node || pipe.ConnectedOutput.Parent == node) result.Add(pipe);
        }
    }
}
