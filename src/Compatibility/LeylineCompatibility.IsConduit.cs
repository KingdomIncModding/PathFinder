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
        internal bool IsConduit(FlowElement element) => element != null && Available && ConduitType.IsInstanceOfType(element);
    }
}
