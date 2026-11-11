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
        Color LeylineColor(FlowElement conduit)
        {
            var name = leyline.Channel(conduit);
            if (!leylineColors.TryGetValue(name, out var tint))
            {
                tint = ChooseColor(); leylineColors.Add(name, tint);
            }
            return tint;
        }
    }
}
