using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.TechTree;
using Assets.Source.Player;
using BepInEx.Bootstrap;
using KingdomFoundation.Api;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomPathFinder
{
    public sealed partial class PathFinderPlugin
    {
        void VerifyResearchUnlock(WorldFlowElement node)
        {
            Check(Chainloader.PluginInfos.ContainsKey("urakka.kingdominc.foundation"), "Foundation loads before Path Finder");
            Check(FoundationApi.TechNodes.TryGet(ResearchId, out var registration) && ReferenceEquals(registration, research)
                && registration is INativeTechNode, "Public Foundation registry owns the Path Finder research model");
            Check(!Unlocked && pins.Count == 0, "Unresearched kingdom has no Path Finder pins");
            Refresh(node); TogglePin(node);
            Check(paths.Count == 0 && conduits.Count == 0 && pinned.Count == 0, "Unresearched kingdom cannot activate hover, pin or conduit highlights");
            var player = GamePlayer.Current;
            GameUI.Instance.ToggleTechMenu(true);
            try
            {
                var tree = TechTreeMenu.Instance;
                Check(!tree.Nodes.Any(n => n.Tech.Identifier == ResearchId), "Path Finder research stays off the Base Game tree");
                var mods = Resources.FindObjectsOfTypeAll<Button>().Single(b => b.name == "ModsTab" && b.gameObject.activeInHierarchy);
                mods.onClick.Invoke();
                Resources.FindObjectsOfTypeAll<Button>().Single(b => b.name == "Mod:" + Id && b.gameObject.activeInHierarchy).onClick.Invoke();
                Check(tree.Nodes.Count(n => n.Tech.Identifier == ResearchId) == 1, "Mods tree contains exactly one native Path Finder unlock");
                var view = tree.Nodes.Single(n => n.Tech.Identifier == ResearchId);
                var native = ((INativeTechNode)registration).NativeNode;
                Check(ReferenceEquals(view.Tech, native) && native.Name == "Path Finder" && native.GetCost().GetCurrency().Single() == (Currency.Gold, 100L),
                    "Path Finder uses the native research card and fixed 100-gold price");
                var click = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
                player.AddCurrency(Currency.Gold, -player.GetCurrency(Currency.Gold), false);
                ExecuteEvents.Execute(view.gameObject, click, ExecuteEvents.pointerClickHandler);
                Check(!Unlocked && player.GetCurrency(Currency.Gold) == 0 && pins.Count == 0, "Insufficient gold cannot unlock highlights or pins");
                player.AddCurrency(Currency.Gold, 150, false);
                ExecuteEvents.Execute(view.gameObject, click, ExecuteEvents.pointerClickHandler);
                Check(Unlocked && player.GetCurrency(Currency.Gold) == 50, "Native research click spends 100 gold and unlocks Path Finder");
                Check(pins.Count == WorldFlowManager.Instance.Elements.Count() && pins.Values.All(p => p.gameObject.activeSelf),
                    "Successful research immediately adds pins to already placed nodes");
                var saved = player.ToJson();
                var restored = GamePlayer.FromJson(saved);
                GamePlayer.Current = player;
                Check(restored.HasTech(ResearchId), "Path Finder unlock survives native kingdom serialization");
                TogglePin(node);
                var newPlayer = GamePlayer.CreateNewPlayer();
                Check(!Unlocked && !newPlayer.HasTech(ResearchId), "Unlock is kingdom-specific and does not leak to a new kingdom");
                UpdateResearchPins();
                Check(pinned.Count == 0 && pins.Values.All(p => !p.gameObject.activeSelf), "Switching to an unresearched kingdom clears pins and hides their buttons");
                GamePlayer.Current = player;
                UpdateResearchPins();
                Check(Unlocked, "Restoring the researched kingdom restores highlight availability");
            }
            finally { GamePlayer.Current = player; GameUI.Instance.ToggleTechMenu(false); }
        }
    }
}
