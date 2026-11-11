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
        void AddSeed(FlowPipe pipe)
        {
            if (pipe?.ConnectedInput == null || pipe.ConnectedOutput == null ||
                pipe.ConnectedInput.ConnectedPipe != pipe || pipe.ConnectedOutput.ConnectedPipe != pipe) return;
            if (IsConduit(pipe.ConnectedInput.Parent)) seeds.Add(pipe.ConnectedInput.Parent);
            if (IsConduit(pipe.ConnectedOutput.Parent)) seeds.Add(pipe.ConnectedOutput.Parent);
        }
    }
}
