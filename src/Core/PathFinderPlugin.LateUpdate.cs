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
        void LateUpdate()
        {
            if (failed) return;
            try
            {
                UpdateResearchPins();
                var node = Unlocked && enabledSetting.Value && WorldFlowManager.Visible &&
                    !(DragDropManager.Instance && DragDropManager.Instance.Busy)
                    ? FindHoveredNode(PlayerInput.MouseUI) : null;
                if (Unlocked && enabledSetting.Value && WorldFlowManager.Visible) Refresh(node);
                else ClearPaths();
            }
            catch (Exception error)
            {
                ClearPaths();
                failed = true;
                Logger.LogError("Path Finder disabled its highlights after an error: " + error);
            }
        }
    }
}
