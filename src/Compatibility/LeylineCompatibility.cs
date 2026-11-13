using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using System.Linq;

namespace KingdomPathFinder
{
    internal sealed class LeylineCompatibility
    {
        void AddSeed(WorkspacePipe pipe)
        {
            if (pipe?.ConnectedInput == null || pipe.ConnectedOutput == null || pipe.ConnectedInput.ConnectedPipe != pipe || pipe.ConnectedOutput.ConnectedPipe != pipe)
                return;
            if (IsConduit(pipe.ConnectedInput.Parent))
                seeds.Add(pipe.ConnectedInput.Parent);
            if (IsConduit(pipe.ConnectedOutput.Parent))
                seeds.Add(pipe.ConnectedOutput.Parent);
        }

        internal string Channel(WorkspaceObject element) => IsConduit(element) ? (string)FoundationApi.WorkspaceAccess.ReadExtensionProperty(element, channel) : null;
        internal void Collect(WorkspaceObject selected, HashSet<WorkspacePipe> pipes, HashSet<WorkspaceObject> targets)
        {
            if (!Available || selected == null || !FoundationApi.World.IsInKingdom)
                return;
            seeds.Clear();
            if (IsConduit(selected))
                seeds.Add(selected);
            // A machine's immediate conduit is part of its direct connection too.
            foreach (var input in selected.Inputs)
                AddSeed(input.ConnectedPipe);
            foreach (var output in selected.Outputs)
                AddSeed(output.ConnectedPipe);
            foreach (var seed in seeds)
            {
                var name = (string)FoundationApi.WorkspaceAccess.ReadExtensionProperty(seed, channel);
                if (string.IsNullOrEmpty(name))
                    continue;
                global::System.Object direction = FoundationApi.WorkspaceAccess.ReadExtensionProperty(seed, mode);
                foreach (var peer in FoundationApi.WorkspaceAccess.AllObjects.Where(item => item.Kind == WorkspaceObjectKind.Building))
                {
                    if (peer == seed || !IsConduit(peer) || Equals(direction, FoundationApi.WorkspaceAccess.ReadExtensionProperty(peer, mode)) || !string.Equals(name, (string)FoundationApi.WorkspaceAccess.ReadExtensionProperty(peer, channel), StringComparison.OrdinalIgnoreCase))
                        continue;
                    targets.Add(seed);
                    targets.Add(peer);
                    foreach (var input in peer.Inputs)
                        PathFinderPlugin.AddConnected(input.ConnectedPipe, peer, pipes);
                    foreach (var output in peer.Outputs)
                        PathFinderPlugin.AddConnected(output.ConnectedPipe, peer, pipes);
                }
            }
        }

        internal const string PluginId = "urakka.kingdominc.leylineconduits";
        internal readonly Type ConduitType;
        readonly PropertyInfo channel, mode;
        readonly HashSet<WorkspaceObject> seeds = new HashSet<WorkspaceObject>();
        internal bool Available => ConduitType != null;

        internal LeylineCompatibility()
        {
            if (!Chainloader.PluginInfos.TryGetValue(PluginId, out var plugin))
                return;
            var type = plugin.Instance.GetType().Assembly.GetType("KingdomLeylineConduits.LeylineConduit");
            channel = type?.GetProperty("Channel");
            mode = type?.GetProperty("Mode");
            if (channel?.PropertyType == typeof(string) && mode != null && FoundationApi.WorkspaceAccess.IsMachineExtensionType(type))
                ConduitType = type;
        }

        internal bool IsConduit(WorkspaceObject element) => element != null && Available && FoundationApi.WorkspaceAccess.IsExtensionInstance(element, ConduitType);
    }
}
