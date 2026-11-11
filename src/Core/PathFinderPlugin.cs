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
    [BepInPlugin(Id, "Path Finder", Version)]
    [BepInDependency("urakka.kingdominc.foundation")]
    [BepInDependency(LeylineCompatibility.PluginId, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed partial class PathFinderPlugin : BaseUnityPlugin
    {
        public const string Id = "urakka.kingdominc.pathfinder", Version = "1.1.0";

        readonly Dictionary<FlowPipe, RainbowPath> paths = new Dictionary<FlowPipe, RainbowPath>();
        readonly HashSet<FlowPipe> desired = new HashSet<FlowPipe>();
        readonly List<FlowPipe> removed = new List<FlowPipe>();
        readonly List<RaycastResult> hits = new List<RaycastResult>();
        readonly Dictionary<WorldFlowElement, PathPin> pins = new Dictionary<WorldFlowElement, PathPin>();
        readonly HashSet<WorldFlowElement> pinned = new HashSet<WorldFlowElement>();
        readonly HashSet<FlowPipe> nodePaths = new HashSet<FlowPipe>();
        readonly List<WorldFlowElement> removedNodes = new List<WorldFlowElement>();
        readonly List<RainbowPath> visiblePaths = new List<RainbowPath>();
        readonly PathLaneLayout laneLayout = new PathLaneLayout();
        readonly HashSet<FlowElement> desiredConduits = new HashSet<FlowElement>();
        readonly Dictionary<FlowElement, ConduitHighlight> conduits = new Dictionary<FlowElement, ConduitHighlight>();
        readonly List<FlowElement> removedConduits = new List<FlowElement>();
        LeylineCompatibility leyline;
        readonly Dictionary<string, Color> leylineColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
        internal static PathFinderPlugin Instance;
        ConfigEntry<bool> enabledSetting;
        ConfigEntry<float> widthSetting;
        Material rainbowMaterial;
        PointerEventData pointer;
        EventSystem pointerSystem;

        Harmony hooks;
        KingdomFoundation.Api.FoundationModContext modContext;
        bool failed;

        static readonly Color[] Palette = {
            new Color(1f, .04f, .18f), new Color(1f, .38f, .02f), new Color(1f, .95f, .02f),
            new Color(.15f, 1f, .05f), new Color(.02f, 1f, 1f), new Color(.2f, .35f, 1f),
            new Color(.72f, .1f, 1f), new Color(1f, .02f, .65f)
        };


        void Awake()
        {
            Instance = this;
            modContext = KingdomFoundation.Api.FoundationApi.ForMod(Id);
            RegisterResearch();
            leyline = new LeylineCompatibility();
            enabledSetting = Config.Bind("Highlights", "Enabled", true, "Highlight immediate input and output pipes while hovering a node.");
            widthSetting = Config.Bind("Highlights", "Width", 5f,
                new ConfigDescription("Width of each solid neon line in world units; glow scales with it.", new AcceptableValueRange<float>(2f, 12f)));
            hooks = new Harmony(Id);
            hooks.Patch(AccessTools.Method(typeof(WorldFlowElement), nameof(WorldFlowElement.SetElement)),
                postfix: new HarmonyMethod(typeof(PathFinderPlugin), nameof(AddPinAfterSet)));
            Logger.LogInfo("Path Finder ready: hover or pin a node for solid neon paths to its immediate parents and children.");
            if (leyline.Available) Logger.LogInfo("Leyline Conduits compatibility enabled.");
        }
    }
}
