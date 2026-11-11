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
        internal void EnsurePin(WorldFlowElement node)
        {
            if (!Unlocked || !node || pins.ContainsKey(node)) return;
            pins.Add(node, PathPin.Create(this, node));
        }
    }
}
