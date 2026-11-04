using System;
using System.Collections;
using System.IO;
using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Player;
using BepInEx.Bootstrap;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace KingdomPathFinder
{
    public sealed partial class PathFinderPlugin
    {
        IEnumerator VerifyLeylineCompatibility(string directory, Mouse mouse, Action<string> fail)
        {
            if (!leyline.Available)
            {
                Check(!Chainloader.PluginInfos.ContainsKey(LeylineCompatibility.PluginId), "Path Finder runs without Leyline Conduits installed");
                yield break;
            }
            FlowElement inlet = null, outlet = null, second = null, wrong = null, sameMode = null, blank = null;
            Manifold source = null, sink = null;
            FlowPipe incoming = null, outgoing = null, secondPipe = null;
            var manager = WorldFlowManager.Instance;
            void Configure(FlowElement element, bool output, string channel)
            {
                var modeType = leyline.ConduitType.GetProperty("Mode").PropertyType;
                var method = leyline.ConduitType.GetMethod("Configure", new[] { modeType, typeof(string), typeof(string).MakeByRefType() });
                object[] args = { Enum.ToObject(modeType, output ? 1 : 0), channel, null };
                Check((bool)method.Invoke(element, args), "Configure diagnostic conduit: " + channel);
            }
            FlowElement Place(bool output, string channel, int x, int y)
            {
                var element = (FlowElement)Activator.CreateInstance(leyline.ConduitType);
                if (channel != null) Configure(element, output, channel);
                element.Position = new Vector2Int(x, y); manager.CreateNewElement(element); return element;
            }
            FlowPipe Wire(FlowElement from, FlowElement to)
            {
                var pipe = new FlowPipe(); pipe.Connect(from.Outputs[0], to.Inputs[0]); manager.CreateNewPipe(pipe); return pipe;
            }
            try
            {
                ClearPaths();
                var flow = GamePlayer.Current.ActiveFlow;
                foreach (var pipe in flow.Pipes.ToArray()) manager.DisconnectPipe(pipe);
                foreach (var element in flow.Elements.ToArray()) manager.RemoveElement(element);
                GamePlayer.Current.AddTech("leyline_conduits");
                source = new Manifold { Position = new Vector2Int(-700, 160) };
                sink = new Manifold { Position = new Vector2Int(400, 160) };
                var sink2 = new Manifold { Position = new Vector2Int(400, -140) };
                manager.CreateNewElement(source); manager.CreateNewElement(sink); manager.CreateNewElement(sink2);
                inlet = Place(false, "  Iron  ", -260, 120); outlet = Place(true, "iron", 100, 120);
                second = Place(true, "IRON", 100, -180); wrong = Place(true, "copper", 100, -400);
                sameMode = Place(false, "iron", -260, -180); blank = Place(false, null, -260, -400);
                Place(false, "copper", -500, -400);
                incoming = Wire(source, inlet); outgoing = Wire(outlet, sink); secondPipe = Wire(second, sink2);
                Canvas.ForceUpdateCanvases();
                WorldFlowCamera.Instance.SetZoomLevel(2);
                Camera.main.transform.position = new Vector3(0, 0, Camera.main.transform.position.z);
                var position = RectTransformUtility.WorldToScreenPoint(inlet.World.GetComponentInParent<Canvas>().worldCamera, inlet.World.Center);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            }
            catch (Exception ex) { fail(ex.ToString()); yield break; }
            yield return new WaitForSecondsRealtime(.5f);
            try
            {
                Check(!failed && FindHoveredNode(mouse.position.ReadValue()) == inlet.World, "Actual conduit hover reaches Path Finder through Leyline UI");
                Check(paths.Keys.ToHashSet().SetEquals(new[] { incoming, outgoing, secondPipe }) && conduits.Count == 3,
                    "Input hover highlights both matching Output conduits and their pipes, ignoring channel case");
                Check(!conduits.ContainsKey(wrong) && !conduits.ContainsKey(sameMode) && !conduits.ContainsKey(blank),
                    "Other channels, same-direction conduits and unbound conduits stay unhighlighted");
                Check(conduits.Values.All(c => !c.raycastTarget), "Conduit outlines do not intercept gear, pin or node pointer events");
                Check(conduits[outlet].color == paths[outgoing].Color && conduits[inlet].color == paths[incoming].Color,
                    "Conduit outline matches its attached pipe's solid neon color");
                var ironColor = paths[incoming].Color;
                Check(paths.Values.All(p => p.Color == ironColor && Vector4.Distance(p.Layers[2].startColor, ironColor) < .01f && Vector4.Distance(p.Layers[2].endColor, ironColor) < .01f)
                    && conduits.Values.All(c => c.color == ironColor), "The full branching leyline route uses one color on every pipe and conduit");
                TogglePin(wrong.World); Refresh(inlet.World);
                Check(conduits[wrong].color != ironColor && conduits[inlet].color == ironColor,
                    "A second pinned leyline channel receives a separate color without changing the first route");
                TogglePin(wrong.World); Refresh(inlet.World);
                string saved = GamePlayer.Current.ActiveFlow.ToJson().ToString();
                Refresh(inlet.World);
                Check(saved == GamePlayer.Current.ActiveFlow.ToJson().ToString(), "Conduit highlights leave channels, icons, buffers and saved graph untouched");
                Capture(Path.Combine(directory, "path-finder-leyline.png"));
                Refresh(outlet.World);
                Check(conduits.Count == 3 && conduits.ContainsKey(inlet) && conduits.ContainsKey(sameMode) && !conduits.ContainsKey(second) && paths.Count == 2,
                    "Output hover follows only opposite-mode Inputs, not other Outputs");
                Check(paths.Values.All(p => p.Color == ironColor) && conduits.Values.All(c => c.color == ironColor),
                    "Hovering from the other end retains the shared leyline color including an unconnected partner");
                Refresh(source.World);
                Check(paths.Count == 3 && conduits.Count == 3, "Hovering the directly connected machine follows its conduit's leyline");
                Refresh(blank.World); Check(paths.Count == 0 && conduits.Count == 0, "Unnamed conduit produces no linked highlights");
                TogglePin(inlet.World); Refresh(null);
                Check(paths.Count == 3 && conduits.Count == 3, "Pinned conduit retains connected conduit and pipe highlights away from hover");
                Configure(outlet, true, "copper"); Refresh(null);
                Check(!conduits.ContainsKey(outlet) && !paths.ContainsKey(outgoing) && conduits.Count == 2,
                    "Retuning removes the old partner immediately while source remains pinned");
                Configure(outlet, true, "iron"); Refresh(null);
                Check(conduits.ContainsKey(outlet) && paths.ContainsKey(outgoing), "Retuning back restores its live connection");
                Check(paths[outgoing].Color == ironColor && conduits[outlet].color == ironColor,
                    "Retuned partner rejoins the original route color");
                manager.DisconnectPipe(secondPipe); Refresh(null);
                Check(conduits.ContainsKey(second) && !paths.ContainsKey(secondPipe), "A named partner without a pipe still highlights without a stale pipe");
                Check(conduits[second].color == ironColor, "Disconnected partner retains its named leyline color");
                GamePlayer.Current.Flows.Add(new FlowDiagram { Label = "Leyline partner tab" });
                int tab = GamePlayer.Current.Flows.Count - 1;
                manager.MoveToFlow(outlet.World, 0, tab); manager.MoveToFlow(sink.World, 0, tab); manager.MoveToFlow(outgoing.World, 0, tab);
                manager.ShowFlow(tab); Refresh(null);
                Check(!inlet.World.isActiveAndEnabled && conduits.Count == 1 && conduits.ContainsKey(outlet) && paths.Count == 1 && paths.ContainsKey(outgoing),
                    "Pinned source on another tab highlights only the visible linked conduit and its pipe");
                Check(paths[outgoing].Color == ironColor && conduits[outlet].color == ironColor,
                    "Shared route color survives a workshop-tab change");
                manager.ShowFlow(0); Refresh(null);
                Check(!conduits.ContainsKey(outlet) && conduits.ContainsKey(inlet) && conduits.ContainsKey(second) && paths.Count == 1,
                    "Returning tabs restores source highlights without leaking hidden partner outlines");
                enabledSetting.Value = false; LateUpdate();
                Check(paths.Count == 0 && conduits.Count == 0, "Disabling Path Finder clears conduit outlines and pipes together");
                enabledSetting.Value = true; Refresh(null);
                Check(conduits.Count == 2, "Re-enabling restores pinned conduit partners");
                var pinPosition = RectTransformUtility.WorldToScreenPoint(inlet.World.GetComponentInParent<Canvas>().worldCamera, pins[inlet.World].transform.position);
                Check(FindHoveredNode(pinPosition) == inlet.World, "Conduit highlight leaves its tiny pin reachable by UI raycast");
                ExecuteEvents.Execute(pins[inlet.World].gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
                Refresh(null); Check(paths.Count == 0 && conduits.Count == 0, "Conduit pin click removes linked highlights");
                TogglePin(inlet.World); manager.RemoveElement(second); Refresh(null);
                Check(!conduits.ContainsKey(second) && conduits.Count == 1, "Deleting a partner removes its outline immediately while other links remain");
                manager.ShowFlow(tab); manager.RemoveElement(outlet); manager.ShowFlow(0); Refresh(null);
                Check(conduits.Count == 0, "Deleting the final named partner clears the source outline");
                TogglePin(inlet.World); ClearPaths();
                InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(1, 1) });
            }
            catch (Exception ex) { fail(ex.ToString()); }
            yield return new WaitForSecondsRealtime(.2f);
            try { Check(!UnityEngine.Object.FindObjectsByType<ConduitHighlight>(FindObjectsSortMode.None).Any(), "Cleanup leaves no conduit outline objects"); }
            catch (Exception ex) { fail(ex.ToString()); }
        }
    }
}
