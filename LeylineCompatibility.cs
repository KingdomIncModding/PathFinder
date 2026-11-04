using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Source.Flow;
using Assets.Source.Player;
using BepInEx.Bootstrap;

namespace KingdomPathFinder
{
    // Read the optional mod's public model; never load its DLL or change its routing.
    internal sealed class LeylineCompatibility
    {
        internal const string PluginId = "urakka.kingdominc.leylineconduits";
        internal readonly Type ConduitType;
        readonly PropertyInfo channel, mode;
        readonly HashSet<FlowElement> seeds = new HashSet<FlowElement>();
        internal bool Available => ConduitType != null;

        internal LeylineCompatibility()
        {
            if (!Chainloader.PluginInfos.TryGetValue(PluginId, out var plugin)) return;
            var type = plugin.Instance.GetType().Assembly.GetType("KingdomLeylineConduits.LeylineConduit");
            channel = type?.GetProperty("Channel"); mode = type?.GetProperty("Mode");
            if (channel?.PropertyType == typeof(string) && mode != null && typeof(FlowElement).IsAssignableFrom(type))
                ConduitType = type;
        }

        internal bool IsConduit(FlowElement element) => element != null && Available && ConduitType.IsInstanceOfType(element);
        internal string Channel(FlowElement element) => IsConduit(element) ? (string)channel.GetValue(element) : null;

        internal void Collect(FlowElement selected, HashSet<FlowPipe> pipes, HashSet<FlowElement> targets)
        {
            if (!Available || selected == null || GamePlayer.Current == null) return;
            seeds.Clear();
            if (IsConduit(selected)) seeds.Add(selected);
            // A machine's immediate conduit is part of its direct connection too.
            foreach (var input in selected.Inputs) AddSeed(input.ConnectedPipe);
            foreach (var output in selected.Outputs) AddSeed(output.ConnectedPipe);
            foreach (var seed in seeds)
            {
                var name = (string)channel.GetValue(seed);
                if (string.IsNullOrEmpty(name)) continue;
                var direction = mode.GetValue(seed);
                foreach (var flow in GamePlayer.Current.Flows)
                    foreach (var peer in flow.Elements)
                    {
                        if (peer == seed || !IsConduit(peer) || Equals(direction, mode.GetValue(peer)) ||
                            !string.Equals(name, (string)channel.GetValue(peer), StringComparison.OrdinalIgnoreCase)) continue;
                        targets.Add(seed); targets.Add(peer);
                        foreach (var input in peer.Inputs) PathFinderPlugin.AddConnected(input.ConnectedPipe, peer, pipes);
                        foreach (var output in peer.Outputs) PathFinderPlugin.AddConnected(output.ConnectedPipe, peer, pipes);
                    }
            }
        }

        void AddSeed(FlowPipe pipe)
        {
            if (pipe?.ConnectedInput == null || pipe.ConnectedOutput == null ||
                pipe.ConnectedInput.ConnectedPipe != pipe || pipe.ConnectedOutput.ConnectedPipe != pipe) return;
            if (IsConduit(pipe.ConnectedInput.Parent)) seeds.Add(pipe.ConnectedInput.Parent);
            if (IsConduit(pipe.ConnectedOutput.Parent)) seeds.Add(pipe.ConnectedOutput.Parent);
        }
    }
}
