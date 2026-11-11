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
        void OnDestroy()
        {
            ClearPaths();
            if (rainbowMaterial) Destroy(rainbowMaterial);
            hooks?.UnpatchSelf();
            modContext?.Dispose();
            foreach (var pin in pins.Values) if (pin) { pin.Owner = null; Destroy(pin.gameObject); }
            pins.Clear(); pinned.Clear(); Instance = null;
        }
    }
}
