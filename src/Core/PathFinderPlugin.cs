using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KingdomPathFinder
{
    [BepInPlugin(Id, "Path Finder", Version)]
    [BepInDependency("urakka.kingdominc.foundation", "0.14.0")]
    [BepInDependency(LeylineCompatibility.PluginId, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class PathFinderPlugin : BaseUnityPlugin
    {
        internal static void AddConnected(WorkspacePipe pipe, WorkspaceObject node, HashSet<WorkspacePipe> result)
        {
            if (pipe?.ConnectedInput == null || pipe.ConnectedOutput == null)
                return;
            if (pipe.ConnectedInput.ConnectedPipe != pipe || pipe.ConnectedOutput.ConnectedPipe != pipe)
                return;
            if (pipe.ConnectedInput.Parent == node || pipe.ConnectedOutput.Parent == node)
                result.Add(pipe);
        }

        static void AddPinAfterSet(UnityEngine.Behaviour __instance) => Instance?.EnsurePin(__instance);
        Color ChooseColor()
        {
            // Keep existing paths stable and use the least-used palette color for a new line.
            int best = 0, least = int.MaxValue;
            for (int i = 0; i < Palette.Length; i++)
            {
                int count = 0;
                foreach (var path in paths.Values)
                    if (path.Color == Palette[i])
                        count++;
                foreach (var tint in leylineColors.Values)
                    if (tint == Palette[i])
                        count++;
                if (count < least)
                {
                    least = count;
                    best = i;
                }
            }

            return Palette[best];
        }

        internal void ClearPaths()
        {
            foreach (var path in paths.Values)
                path.Dispose();
            paths.Clear();
            foreach (var graphic in conduits.Values)
                if (graphic)
                {
                    graphic.enabled = false;
                    Destroy(graphic.gameObject);
                }

            conduits.Clear();
            desiredConduits.Clear();
        }

        internal static void CollectDirectPaths(WorkspaceObject node, HashSet<WorkspacePipe> result)
        {
            result.Clear();
            if (node == null)
                return;
            foreach (var input in node.Inputs)
                AddConnected(input.ConnectedPipe, node, result);
            foreach (var output in node.Outputs)
                AddConnected(output.ConnectedPipe, node, result);
        }

        public const string Id = "urakka.kingdominc.pathfinder", Version = "1.1.2";
        readonly Dictionary<WorkspacePipe, RainbowPath> paths = new Dictionary<WorkspacePipe, RainbowPath>();
        readonly HashSet<WorkspacePipe> desired = new HashSet<WorkspacePipe>();
        readonly List<WorkspacePipe> removed = new List<WorkspacePipe>();
        readonly List<RaycastResult> hits = new List<RaycastResult>();
        readonly Dictionary<UnityEngine.Behaviour, PathPin> pins = new Dictionary<UnityEngine.Behaviour, PathPin>();
        readonly HashSet<UnityEngine.Behaviour> pinned = new HashSet<UnityEngine.Behaviour>();
        readonly HashSet<WorkspacePipe> nodePaths = new HashSet<WorkspacePipe>();
        readonly List<UnityEngine.Behaviour> removedNodes = new List<UnityEngine.Behaviour>();
        readonly List<RainbowPath> visiblePaths = new List<RainbowPath>();
        readonly PathLaneLayout laneLayout = new PathLaneLayout();
        readonly HashSet<WorkspaceObject> desiredConduits = new HashSet<WorkspaceObject>();
        readonly Dictionary<WorkspaceObject, ConduitHighlight> conduits = new Dictionary<WorkspaceObject, ConduitHighlight>();
        readonly List<WorkspaceObject> removedConduits = new List<WorkspaceObject>();
        LeylineCompatibility leyline;
        readonly Dictionary<string, Color> leylineColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
        internal static PathFinderPlugin Instance;
        ConfigEntry<bool> enabledSetting;
        ConfigEntry<float> widthSetting;
        Material rainbowMaterial;
        PointerEventData pointer;
        EventSystem pointerSystem;
        IDisposable hooks;
        IDisposable editHook;
        IDisposable deserializeHook;
        KingdomFoundation.Api.FoundationModContext modContext;
        bool highlightsPaused;
        int consecutiveFailures;
        float retryAtTime;
        string lastError;
        GameKingdom activeSession;
        int activeWorkspace = -1;
        bool lastEnabledSetting = true;
        PathFinderResearch research;
        internal string LastError => lastError;
        internal bool HighlightsPausedDueToError => highlightsPaused;
        internal bool Unlocked => research?.Unlocked == true;
        internal IEnumerable<PathPin> ResearchPins => pins.Values;

        internal void ClearResearchPins()
        {
            pinned.Clear();
            ClearPaths();
        }

        static readonly Color[] Palette =
        {
            new Color(1f, .04f, .18f),
            new Color(1f, .38f, .02f),
            new Color(1f, .95f, .02f),
            new Color(.15f, 1f, .05f),
            new Color(.02f, 1f, 1f),
            new Color(.2f, .35f, 1f),
            new Color(.72f, .1f, 1f),
            new Color(1f, .02f, .65f)
        };
        void Awake()
        {
            Instance = this;
            modContext = KingdomFoundation.Api.FoundationApi.ForMod(Id);
            research = new PathFinderResearch(this);
            research.Register(modContext);
            leyline = new LeylineCompatibility();
            enabledSetting = Config.Bind("Highlights", "Enabled", true, "Highlight immediate input and output pipes while hovering a node.");
            widthSetting = Config.Bind("Highlights", "Width", 5f, new ConfigDescription("Width of each solid neon line in world units; glow scales with it.", new AcceptableValueRange<float>(2f, 12f)));
            lastEnabledSetting = enabledSetting.Value;
            hooks = FoundationApi.GameHooks.Subscribe(Id, GameHookPoints.BuildingBound, after: call =>
            {
                AddPinAfterSet(call.View as UnityEngine.Behaviour);
                Instance?.leyline?.InvalidateCache();
            });
            editHook = FoundationApi.ExecutionHooks.Subscribe(Id, GameExecutionPoints.WorkspaceEdit, _ => null, (_, __) => Instance?.leyline?.InvalidateCache());
            deserializeHook = FoundationApi.GameHooks.Subscribe(Id, GameHookPoints.BuildingDeserialize, after: _ => Instance?.leyline?.InvalidateCache());
            Logger.LogInfo("Path Finder ready: hover or pin a node for solid neon paths to its immediate parents and children.");
            if (leyline.Available)
                Logger.LogInfo("Leyline Conduits compatibility enabled.");
        }

        internal void EnsurePin(UnityEngine.Behaviour node)
        {
            if (!Unlocked || !node || pins.ContainsKey(node))
                return;
            pins.Add(node, PathPin.Create(this, node));
        }

        internal UnityEngine.Behaviour FindHoveredNode(Vector2 screenPosition)
        {
            if (!EventSystem.current)
                return null;
            if (pointer == null || pointerSystem != EventSystem.current)
            {
                pointerSystem = EventSystem.current;
                pointer = new PointerEventData(pointerSystem);
            }

            pointer.Reset();
            pointer.position = screenPosition;
            hits.Clear();
            pointerSystem.RaycastAll(pointer, hits);
            // Respect the first UI hit: a popup must not highlight a node behind it.
            return hits.Count == 0 ? null : FoundationApi.WorkspaceAccess.FindBuildingView(hits[0].gameObject);
        }

        internal void ForgetPin(UnityEngine.Behaviour node)
        {
            pinned.Remove(node);
            pins.Remove(node);
        }

        void LateUpdate()
        {
            var currentKingdom = FoundationApi.Kingdom;
            int currentWorkspace = currentKingdom != null ? currentKingdom.CurrentWorkspace : -1;
            bool enabledNow = enabledSetting.Value;

            if (!ReferenceEquals(activeSession, currentKingdom))
            {
                activeSession = currentKingdom;
                activeWorkspace = currentWorkspace;
                ClearSessionState();
                ResetErrorRecovery();
            }
            else if (currentWorkspace != activeWorkspace)
            {
                activeWorkspace = currentWorkspace;
                ResetErrorRecovery();
            }
            else if (enabledNow != lastEnabledSetting)
            {
                lastEnabledSetting = enabledNow;
                ResetErrorRecovery();
            }

            if (highlightsPaused || (retryAtTime > 0f && Time.unscaledTime < retryAtTime))
                return;

            try
            {
                research.UpdatePins();
                var node = Unlocked && enabledNow && FoundationApi.UiAccess.IsWorkspaceVisible && !FoundationApi.UiAccess.IsDragDropBusy ? FindHoveredNode(FoundationApi.View.MouseUi) : null;
                if (Unlocked && enabledNow && FoundationApi.UiAccess.IsWorkspaceVisible)
                    Refresh(node);
                else
                    ClearPaths();

                if (consecutiveFailures > 0 || lastError != null)
                    ResetErrorRecovery();
            }
            catch (Exception error)
            {
                ClearPaths();
                consecutiveFailures++;
                lastError = error.Message;
                bool isTransient = error is MissingReferenceException || error is NullReferenceException;
                if (isTransient && consecutiveFailures < 5)
                {
                    retryAtTime = Time.unscaledTime + 0.5f;
                    Logger.LogWarning("Path Finder recovered from transient view state error and will retry: " + error.Message);
                }
                else if (consecutiveFailures >= 3)
                {
                    highlightsPaused = true;
                    retryAtTime = float.PositiveInfinity;
                    Logger.LogError("Path Finder paused its highlights after repeated errors (change tab/kingdom or toggle Highlights Enabled to retry): " + error);
                    FoundationApi.UiAccess.ShowStatusMessage("Path Finder paused highlights after an error. Toggle Highlights or switch tabs/kingdoms to retry.");
                }
                else
                {
                    retryAtTime = Time.unscaledTime + 1.0f;
                    Logger.LogError("Path Finder hit a highlight error and will retry: " + error);
                }
            }
        }

        private void ClearSessionState()
        {
            ClearPaths();
            leylineColors.Clear();
            pinned.Clear();
            pins.Clear();
            leyline?.ResetSession();
        }

        private void ResetErrorRecovery()
        {
            highlightsPaused = false;
            consecutiveFailures = 0;
            retryAtTime = 0f;
            lastError = null;
        }

        Color LeylineColor(WorkspaceObject conduit)
        {
            var name = leyline.Channel(conduit);
            if (!leylineColors.TryGetValue(name, out var tint))
            {
                tint = ChooseColor();
                leylineColors.Add(name, tint);
            }

            return tint;
        }

        void OnDestroy()
        {
            ClearPaths();
            if (rainbowMaterial)
                Destroy(rainbowMaterial);
            hooks?.Dispose();
            editHook?.Dispose();
            deserializeHook?.Dispose();
            modContext?.Dispose();
            foreach (var pin in pins.Values)
                if (pin)
                {
                    pin.Owner = null;
                    Destroy(pin.gameObject);
                }

            pins.Clear();
            pinned.Clear();
            leylineColors.Clear();
            leyline?.ResetSession();
            activeSession = null;
            Instance = null;
        }

        void OnDisable() => ClearPaths();
        internal void Refresh(UnityEngine.Behaviour node)
        {
            if (!Unlocked)
            {
                ClearPaths();
                return;
            }

            CollectDirectPaths(node && node.isActiveAndEnabled ? FoundationApi.WorkspaceAccess.FromView(node) : null, desired);
            desiredConduits.Clear();
            leyline.Collect(node && node.isActiveAndEnabled ? FoundationApi.WorkspaceAccess.FromView(node) : null, desired, desiredConduits);
            removedNodes.Clear();
            foreach (var locked in pinned)
            {
                if (!locked)
                {
                    removedNodes.Add(locked);
                    continue;
                }

                // Keep a pinned leyline discoverable when its partner is on another tab.
                leyline.Collect(FoundationApi.WorkspaceAccess.FromView(locked), desired, desiredConduits);
                if (!locked.isActiveAndEnabled)
                    continue;
                CollectDirectPaths(FoundationApi.WorkspaceAccess.FromView(locked), nodePaths);
                desired.UnionWith(nodePaths);
            }

            foreach (var missing in removedNodes)
                pinned.Remove(missing);
            removed.Clear();
            foreach (var entry in paths)
                if (!desired.Contains(entry.Key) || !entry.Value.Alive)
                    removed.Add(entry.Key);
            foreach (var pipe in removed)
            {
                paths[pipe].Dispose();
                paths.Remove(pipe);
            }

            foreach (var pipe in desired)
            {
                if (!pipe.World || !pipe.World.isActiveAndEnabled)
                    continue;
                if (!paths.TryGetValue(pipe, out var path))
                {
                    var source = FoundationApi.WorkspaceVisuals.PipeLine(pipe.World);
                    if (!source)
                        continue;
                    if (!rainbowMaterial)
                    {
                        var shader = Shader.Find("Sprites/Default");
                        if (!shader)
                            throw new InvalidOperationException("The game's unlit sprite shader is unavailable.");
                        rainbowMaterial = new Material(shader)
                        {
                            name = "PathFinder.NeonRainbow"
                        };
                    }

                    path = new RainbowPath(source, rainbowMaterial, TryLeylineColor(pipe, out var tint) ? tint : ChooseColor());
                    paths.Add(pipe, path);
                }
                else if (TryLeylineColor(pipe, out var tint))
                    path.SetColor(tint);
            }

            visiblePaths.Clear();
            foreach (var path in paths.Values)
                if (path.Alive)
                    visiblePaths.Add(path);
            laneLayout.Refresh(visiblePaths, widthSetting.Value);
            RefreshConduits();
        }

        void RefreshConduits()
        {
            removedConduits.Clear();
            foreach (var entry in conduits)
                if (!desiredConduits.Contains(entry.Key) || !entry.Key.World || !entry.Key.World.isActiveAndEnabled || !entry.Value)
                    removedConduits.Add(entry.Key);
            foreach (var element in removedConduits)
            {
                var graphic = conduits[element];
                if (graphic)
                {
                    graphic.enabled = false;
                    Destroy(graphic.gameObject);
                }

                conduits.Remove(element);
            }

            foreach (var element in desiredConduits)
            {
                if (!element.World || !element.World.isActiveAndEnabled)
                    continue;
                if (!conduits.TryGetValue(element, out var graphic))
                {
                    graphic = ConduitHighlight.Create(element.World, LeylineColor(element));
                    conduits.Add(element, graphic);
                }

                graphic.color = LeylineColor(element);
                graphic.SetWidth(widthSetting.Value);
            }
        }

        internal void TogglePin(UnityEngine.Behaviour node)
        {
            if (!Unlocked || !node)
                return;
            if (!pinned.Add(node))
                pinned.Remove(node);
            if (pins.TryGetValue(node, out var pin))
                pin.SetPinned(pinned.Contains(node));
        }

        bool TryLeylineColor(WorkspacePipe pipe, out Color tint)
        {
            var from = pipe.ConnectedOutput?.Parent;
            var to = pipe.ConnectedInput?.Parent;
            var conduit = from != null && desiredConduits.Contains(from) ? from : to != null && desiredConduits.Contains(to) ? to : null;
            tint = conduit == null ? default : LeylineColor(conduit);
            return conduit != null;
        }
    }
}
