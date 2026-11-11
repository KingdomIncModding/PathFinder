using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Source.Flow;
using Assets.Source.Player;
using BepInEx.Bootstrap;

namespace KingdomPathFinder
{
    // Read the optional mod's public model; never load its DLL or change its routing.
    internal sealed partial class LeylineCompatibility
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
    }
}
