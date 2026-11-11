using Assets.Behaviour.Flow;
using Assets.Source.Player;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomPathFinder
{
    public sealed partial class PathFinderPlugin
    {
        internal const string ResearchId = Id + ".research";
        ITechNode research;
        bool pinsUnlocked;
        internal bool Unlocked => research?.IsRegistered == true && GamePlayer.Current != null && GamePlayer.Current.HasTech(ResearchId);


        void RegisterResearch()
        {
            research = modContext.Register(new TechNodeDefinition(
                ResearchId, "Path Finder", Id, "Path Finder", new Vector2(480, 0),
                "Reveal a node's immediate input and output paths in bright neon colors. Pin a node to keep its paths highlighted.",
                research: new TechNodeOptions(nodeType: TechNodeType.Utility,
                    fixedCost: new CurrencyCollection(Currency.Gold, 100), iconName: "Icons_17")));
            research.Clicked += OnResearchUnlocked;
        }
    }
}
