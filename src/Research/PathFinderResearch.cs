using System.Linq;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomPathFinder
{
    internal sealed class PathFinderResearch
    {
        private readonly PathFinderPlugin owner;
        internal PathFinderResearch(PathFinderPlugin owner)
        {
            this.owner = owner;
        }

        internal const string ResearchId = PathFinderPlugin.Id + ".research";
        ITechNode research;
        bool pinsUnlocked;
        internal bool Unlocked => research?.IsRegistered == true && FoundationApi.World.IsInKingdom && FoundationApi.Progression.HasTech(ResearchId);

        internal void Register(FoundationModContext context)
        {
            research = context.Register(new TechNodeDefinition(ResearchId, "Path Finder", PathFinderPlugin.Id, "Path Finder", new Vector2(480, 0), "Reveal a node's immediate input and output paths in bright neon colors. Pin a node to keep its paths highlighted.", research: TechNodeOptions.Create(nodeKind: "Utility", fixedCost: new GameMoney("Gold", 100), iconName: "Icons_17")));
            research.Clicked += OnResearchUnlocked;
        }

        void OnResearchUnlocked(ITechNode node) => UpdatePins();
        internal void UpdatePins()
        {
            bool unlocked = Unlocked;
            if (unlocked == pinsUnlocked)
                return;
            pinsUnlocked = unlocked;
            if (unlocked && FoundationApi.UiAccess.Root)
                foreach (var node in FoundationApi.WorkspaceAccess.ActiveObjects.Where(item => item.Kind == WorkspaceObjectKind.Building && item.World).Select(item => item.World))
                    owner.EnsurePin(node);
            foreach (var pin in owner.ResearchPins)
                if (pin)
                {
                    if (!unlocked)
                        pin.SetPinned(false);
                    pin.gameObject.SetActive(unlocked);
                }

            if (!unlocked)
            {
                owner.ClearResearchPins();
            }
        }
    }
}
