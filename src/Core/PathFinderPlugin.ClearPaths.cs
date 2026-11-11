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
        internal void ClearPaths()
        {
            foreach (var path in paths.Values) path.Dispose();
            paths.Clear();
            foreach (var graphic in conduits.Values) if (graphic) { graphic.enabled = false; Destroy(graphic.gameObject); }
            conduits.Clear(); desiredConduits.Clear();
        }
    }
}
