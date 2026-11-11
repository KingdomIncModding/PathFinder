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
        internal string Channel(FlowElement element) => IsConduit(element) ? (string)channel.GetValue(element) : null;
    }
}
