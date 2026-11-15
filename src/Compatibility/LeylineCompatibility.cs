using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomPathFinder
{
    internal sealed class LeylineCompatibility
    {
        private struct ConduitEntry
        {
            internal string Channel;
            internal object Mode;
        }

        internal const string PluginId = "urakka.kingdominc.leylineconduits";
        internal readonly Type ConduitType;
        private readonly PropertyInfo channel, mode;
        private readonly HashSet<WorkspaceObject> seeds = new HashSet<WorkspaceObject>();
        private readonly Dictionary<string, List<WorkspaceObject>> conduitsByChannel = new Dictionary<string, List<WorkspaceObject>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<WorkspaceObject, ConduitEntry> conduitEntries = new Dictionary<WorkspaceObject, ConduitEntry>();
        private GameKingdom indexedKingdom;
        private bool indexDirty = true;
        private float nextReconcileTime;

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

        internal void InvalidateCache() => indexDirty = true;

        internal void ResetSession()
        {
            indexedKingdom = null;
            conduitsByChannel.Clear();
            conduitEntries.Clear();
            indexDirty = true;
        }

        internal string Channel(WorkspaceObject element)
        {
            if (!IsConduit(element)) return null;
            if (conduitEntries.TryGetValue(element, out var cached) && !indexDirty)
                return cached.Channel;
            return (string)FoundationApi.WorkspaceAccess.ReadExtensionProperty(element, channel);
        }

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

            if (seeds.Count == 0)
                return;

            EnsureIndex();
            foreach (var seed in seeds)
            {
                var name = (string)FoundationApi.WorkspaceAccess.ReadExtensionProperty(seed, channel);
                if (string.IsNullOrEmpty(name))
                    continue;
                global::System.Object direction = FoundationApi.WorkspaceAccess.ReadExtensionProperty(seed, mode);
                if (!conduitEntries.TryGetValue(seed, out var cachedSeed) ||
                    !string.Equals(cachedSeed.Channel, name, StringComparison.OrdinalIgnoreCase) ||
                    !Equals(cachedSeed.Mode, direction))
                {
                    indexDirty = true;
                    EnsureIndex(force: true);
                }

                if (!conduitsByChannel.TryGetValue(name, out var channelPeers))
                    continue;

                for (int i = 0; i < channelPeers.Count; i++)
                {
                    var peer = channelPeers[i];
                    if (peer == seed) continue;
                    var peerEntry = conduitEntries[peer];
                    if (Equals(direction, peerEntry.Mode))
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

        private void EnsureIndex(bool force = false)
        {
            float now = Time.unscaledTime;
            var currentKingdom = FoundationApi.Kingdom;
            if (!force && !indexDirty && ReferenceEquals(indexedKingdom, currentKingdom) && now < nextReconcileTime)
                return;

            indexedKingdom = currentKingdom;
            indexDirty = false;
            nextReconcileTime = now + 0.5f;

            conduitsByChannel.Clear();
            conduitEntries.Clear();
            if (currentKingdom == null)
                return;

            foreach (var item in FoundationApi.WorkspaceAccess.AllObjects)
            {
                if (item.Kind != WorkspaceObjectKind.Building || !IsConduit(item))
                    continue;
                var ch = (string)FoundationApi.WorkspaceAccess.ReadExtensionProperty(item, channel);
                var dir = FoundationApi.WorkspaceAccess.ReadExtensionProperty(item, mode);
                conduitEntries[item] = new ConduitEntry { Channel = ch, Mode = dir };
                if (string.IsNullOrEmpty(ch))
                    continue;
                if (!conduitsByChannel.TryGetValue(ch, out var list))
                {
                    list = new List<WorkspaceObject>();
                    conduitsByChannel.Add(ch, list);
                }
                list.Add(item);
            }
        }

        private void AddSeed(WorkspacePipe pipe)
        {
            if (pipe?.ConnectedInput == null || pipe.ConnectedOutput == null || pipe.ConnectedInput.ConnectedPipe != pipe || pipe.ConnectedOutput.ConnectedPipe != pipe)
                return;
            if (IsConduit(pipe.ConnectedInput.Parent))
                seeds.Add(pipe.ConnectedInput.Parent);
            if (IsConduit(pipe.ConnectedOutput.Parent))
                seeds.Add(pipe.ConnectedOutput.Parent);
        }

        internal bool IsConduit(WorkspaceObject element) => element != null && Available && FoundationApi.WorkspaceAccess.IsExtensionInstance(element, ConduitType);
    }
}
