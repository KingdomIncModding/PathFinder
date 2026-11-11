using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Source.Flow;
using Assets.Source.Player;
using BepInEx.Bootstrap;

namespace KingdomPathFinder
{
    internal sealed partial class LeylineCompatibility
    {
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
    }
}
