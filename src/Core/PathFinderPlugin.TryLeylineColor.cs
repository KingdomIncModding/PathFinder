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
        bool TryLeylineColor(FlowPipe pipe, out Color tint)
        {
            var from = pipe.ConnectedOutput?.Parent; var to = pipe.ConnectedInput?.Parent;
            var conduit = from != null && desiredConduits.Contains(from) ? from : to != null && desiredConduits.Contains(to) ? to : null;
            tint = conduit == null ? default : LeylineColor(conduit);
            return conduit != null;
        }
    }
}
