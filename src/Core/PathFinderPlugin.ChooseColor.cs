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
        Color ChooseColor()
        {
            // Keep existing paths stable and use the least-used palette color for a new line.
            int best = 0, least = int.MaxValue;
            for (int i = 0; i < Palette.Length; i++)
            {
                int count = 0;
                foreach (var path in paths.Values) if (path.Color == Palette[i]) count++;
                foreach (var tint in leylineColors.Values) if (tint == Palette[i]) count++;
                if (count < least) { least = count; best = i; }
            }
            return Palette[best];
        }
    }
}
