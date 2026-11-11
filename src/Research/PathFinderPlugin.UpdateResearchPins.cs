using Assets.Behaviour.Flow;
using Assets.Source.Player;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomPathFinder
{
    public sealed partial class PathFinderPlugin
    {
        void UpdateResearchPins()
        {
            bool unlocked = Unlocked;
            if (unlocked == pinsUnlocked) return;
            pinsUnlocked = unlocked;
            if (unlocked && WorldFlowManager.Instance)
                foreach (var node in WorldFlowManager.Instance.Elements) EnsurePin(node);
            foreach (var pin in pins.Values) if (pin)
            {
                if (!unlocked) pin.SetPinned(false);
                pin.gameObject.SetActive(unlocked);
            }
            if (!unlocked) { pinned.Clear(); ClearPaths(); }
        }
    }
}
