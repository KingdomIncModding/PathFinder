using Assets.Behaviour.Flow;
using Assets.Source.Player;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomPathFinder
{
    public sealed partial class PathFinderPlugin
    {
        void OnResearchUnlocked(ITechNode node) => UpdateResearchPins();
    }
}
