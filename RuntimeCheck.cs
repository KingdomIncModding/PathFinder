using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Player;
using Assets.Source.Util;
using BepInEx;
using HarmonyLib;
using LightJson;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KingdomPathFinder
{
    public sealed partial class PathFinderPlugin
    {
        readonly List<string> checks = new List<string>();
        void Check(bool passed, string name)
        {
            if (!passed) throw new InvalidOperationException(name);
            checks.Add(name);
        }

        static FlowPipe Connect(FlowElement from, int output, FlowElement to, int input, params Vector2Int[] bends)
        {
            var pipe = new FlowPipe();
            pipe.Connect(from.Outputs[output], to.Inputs[input]);
            pipe.Nodes.AddRange(bends);
            GamePlayer.Current.ActiveFlow.Pipes.Add(pipe);
            return pipe;
        }

        IEnumerator RuntimeCheck()
        {
            yield return new WaitForSecondsRealtime(5);
            var directory = Path.Combine(Paths.GameRootPath, "_Mods", "PathFinder", "Verification");
            Directory.CreateDirectory(directory);
            string error = null;
            Manifold center = null, parentA = null, parentB = null, childA = null, childB = null;
            FlowPipe incomingA = null, incomingB = null, outgoingA = null, outgoingB = null, grandparent = null, grandchild = null;
            Mouse mouse = null;
            GameObject blocker = null;
            string initialFlow = null;
            Vector2 hoverPosition = default;
            try
            {
                Check(!GameUI.Instance && GamePlayer.Current == null, "No normal kingdom or save loaded");
                var player = GamePlayer.CreateNewPlayer();
                player.Flows[0].Elements.Clear(); player.Flows[0].Pipes.Clear();
                player.Flows[0].Label = "Path Finder verification";
                player.AddTech("manifold"); player.AddTech("collector"); player.AddTech("distributor");
                center = new Manifold { Position = new Vector2Int(-120, 80) };
                parentA = new Manifold { Position = new Vector2Int(-650, 310) };
                parentB = new Manifold { Position = new Vector2Int(-650, -180) };
                childA = new Manifold { Position = new Vector2Int(440, 310) };
                childB = new Manifold { Position = new Vector2Int(440, -180) };
                var root = new Manifold { Position = new Vector2Int(-1110, 310) };
                var leaf = new Manifold { Position = new Vector2Int(970, 310) };
                player.Flows[0].Elements.AddRange(new FlowElement[] { root, parentA, parentB, center, childA, childB, leaf });
                grandparent = Connect(root, 0, parentA, 0);
                incomingA = Connect(parentA, 0, center, 0, new Vector2Int(-340, 245), new Vector2Int(-340, 20));
                incomingB = Connect(parentB, 0, center, 1, new Vector2Int(-280, -245), new Vector2Int(-280, -44));
                outgoingA = Connect(center, 0, childA, 0, new Vector2Int(230, 20), new Vector2Int(230, 245));
                outgoingB = Connect(center, 1, childB, 0, new Vector2Int(300, -44), new Vector2Int(300, -245));
                grandchild = Connect(childA, 0, leaf, 0);
                var selected = new HashSet<FlowPipe>();
                CollectDirectPaths(center, selected);
                Check(selected.SetEquals(new[] { incomingA, incomingB, outgoingA, outgoingB }), "Both direct parents and both direct children selected");
                Check(!selected.Contains(grandparent) && !selected.Contains(grandchild), "Grandparents and grandchildren excluded");
                CollectDirectPaths(null, selected); Check(selected.Count == 0, "Empty hover selects no paths");
                var ghost = new FlowPipe(); ghost.Connect(center.Outputs[0], childA.Inputs[0], true);
                Check(center.Outputs[0].ConnectedPipe == outgoingA, "Ghost connection does not replace real connection");
                SceneManager.LoadScene("Game");
            }
            catch (Exception ex) { error = ex.ToString(); }

            yield return new WaitForSecondsRealtime(4);
            if (error == null) try
            {
                Check(center.World && incomingA.World, "Disposable kingdom built with native node and pipe renderers");
                WorldFlowCamera.Instance.SetZoomLevel(2);
                var cam = Camera.main; cam.transform.position = new Vector3(0, 0, cam.transform.position.z);
                Canvas.ForceUpdateCanvases();
                hoverPosition = RectTransformUtility.WorldToScreenPoint(center.World.GetComponentInParent<Canvas>().worldCamera, center.World.Center);
                Check(FindHoveredNode(hoverPosition) == center.World, "Actual UI raycast resolves hovered node");
                initialFlow = GamePlayer.Current.ActiveFlow.ToJson().ToString();
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                mouse = InputSystem.AddDevice<Mouse>("PathFinderDiagnosticMouse");
                InputSystem.QueueStateEvent(mouse, new MouseState { position = hoverPosition });
            }
            catch (Exception ex) { error = ex.ToString(); }

            yield return new WaitForSecondsRealtime(.5f);
            if (error == null) try
            {
                Check(!failed && paths.Count == 4, "Real mouse movement activates four highlights through LateUpdate");
                string serialized = GamePlayer.Current.ActiveFlow.ToJson().ToString();
                foreach (var entry in paths)
                {
                    var path = entry.Value;
                    Check(path.Layers.All(l => l && l.enabled), "Core and both glow layers enabled: " + entry.Key.ConnectedOutput.Parent.Position);
                    Check(path.Layers.All(l => l.GetComponents<Component>().Length == 2), "Overlay has only transform and renderer; no pointer interceptors");
                    Check(path.Layers[2].positionCount == path.Source.positionCount, "Solid line follows native pipe vertices exactly");
                    Check(path.Layers[2].startColor == path.Layers[2].endColor && path.Layers[2].colorGradient.colorKeys.All(k =>
                        Vector3.Distance(new Vector3(k.color.r, k.color.g, k.color.b), new Vector3(path.Color.r, path.Color.g, path.Color.b)) < .01f), "One solid neon color across the entire line");
                    Check(path.Layers[0].widthMultiplier > path.Layers[2].widthMultiplier, "Glow is wider than bright core");
                    for (int i = 0; i < path.Source.positionCount; i++)
                    {
                        var corner = path.Source.GetPosition(i);
                        Check(Enumerable.Range(0, path.Layers[2].positionCount).Any(p => Vector3.Distance(path.Layers[2].GetPosition(p), corner) < .01f), "Overlay preserves pipe endpoint or bend");
                    }
                }
                Check(!paths.ContainsKey(grandparent) && !paths.ContainsKey(grandchild), "Only immediate neighbors highlighted in game");
                Check(paths.Values.Select(p => p.Color).Distinct().Count() == 4, "Four highlighted lines use four different neon palette colors");
                Check(pins.Count == 7 && pins.Values.All(p => ((RectTransform)p.transform).sizeDelta == new Vector2(18, 18)), "Every node receives one tiny 18-unit pin button");
                Check(initialFlow == serialized, "Highlight creation leaves native serialized flow unchanged");
                Capture(Path.Combine(directory, "path-finder-hover.png"));
                CapturePinStyles(Path.Combine(directory, "path-finder-pin-styles.png"));
                VerifyOverlappingHighlights(center.World, directory);

                var original = paths[incomingA].Source;
                original.startColor = Color.red; original.endColor = Color.red;
                Refresh(center.World);
                Check(original.startColor == Color.red && original.endColor == Color.red, "Native transfer/rejection colors remain untouched during hover");
                ClearPaths();
                Check(paths.Count == 0 && original.startColor == Color.red, "Cleanup preserves current native rejection state");

                // Child controls (ports/gear) resolve back to their owning node.
                var port = center.World.GetComponentsInChildren<WorldFlowIO>().First();
                var portPosition = RectTransformUtility.WorldToScreenPoint(port.GetComponentInParent<Canvas>().worldCamera, port.transform.position);
                Check(FindHoveredNode(portPosition) == center.World, "Hovering an IO control resolves its owning node");

                var diagram = GamePlayer.Current.ActiveFlow;
                WorldFlowManager.Instance.DisconnectPipe(incomingB);
                Refresh(center.World);
                Check(paths.Count == 3 && !paths.ContainsKey(incomingB), "Disconnect removes its highlight immediately");
                var replacement = new FlowPipe(); replacement.Connect(parentB.Outputs[0], center.Inputs[1]);
                WorldFlowManager.Instance.CreateNewPipe(replacement); Refresh(center.World);
                Check(paths.Count == 4 && paths.ContainsKey(replacement), "Newly connected pipe appears without leaving hover");

                // Straight pipes retain the same solid color from end to end.
                Check(paths[replacement].Source.positionCount == 2 && paths[replacement].Layers[2].startColor == paths[replacement].Layers[2].endColor, "Straight pipe stays one solid neon color");
                var bend = new Vector2Int(-350, -300); replacement.Nodes.Add(bend); replacement.World.UpdateLinePositions(); Refresh(center.World);
                Check(Enumerable.Range(0, paths[replacement].Layers[2].positionCount).Any(p => Vector3.Distance(paths[replacement].Layers[2].GetPosition(p), new Vector3(bend.x, bend.y)) < .01f), "Edited bend follows native pipe geometry");
                parentA.Mirrored = true; parentA.World.UpdateVisuals(); incomingA.World.UpdateLinePositions(); Refresh(center.World);
                Check(paths[incomingA].Layers[2].GetPosition(0) == paths[incomingA].Source.GetPosition(0), "Mirrored parent endpoint follows native geometry");
                parentA.Mirrored = false; parentA.World.UpdateVisuals(); incomingA.World.UpdateLinePositions();

                var loop = new FlowPipe(); loop.Connect(childB.Outputs[0], parentB.Inputs[1]); WorldFlowManager.Instance.CreateNewPipe(loop);
                Refresh(center.World); Check(paths.Count == 4, "Cycle elsewhere in chain does not recurse or add indirect paths");
                var self = new FlowPipe(); self.Connect(childB.Outputs[1], childB.Inputs[1]); WorldFlowManager.Instance.CreateNewPipe(self);
                Refresh(childB.World); Check(paths.Count == 3 && paths.ContainsKey(self), "Self-loop appears once despite input and output membership");

                childB.World.gameObject.SetActive(false); Refresh(childB.World); Check(paths.Count == 0, "Hidden or destroyed hovered node clears all paths");
                childB.World.gameObject.SetActive(true);
                Refresh(center.World); Check(paths.Count == 4, "Hover change replaces previous node highlights");
                var pinScreen = RectTransformUtility.WorldToScreenPoint(center.World.GetComponentInParent<Canvas>().worldCamera, pins[center.World].transform.position);
                hits.Clear(); var pinPointer = new PointerEventData(EventSystem.current) { position = pinScreen };
                EventSystem.current.RaycastAll(pinPointer, hits);
                Check(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == pins[center.World].gameObject, "Tiny pin is the actual click target at the node top-left");
                ExecuteEvents.Execute(pins[center.World].gameObject, pinPointer, ExecuteEvents.pointerClickHandler);
                Refresh(null); Check(pinned.Contains(center.World) && paths.Count == 4, "Pin click locks direct paths after leaving the node");
                var pinnedColor = paths[outgoingA].Color;
                Refresh(childA.World); Check(paths.Count == 5 && paths[outgoingA].Color == pinnedColor, "Pinned paths coexist with hover and shared line keeps its color");
                ExecuteEvents.Execute(pins[parentA.World].gameObject, pinPointer, ExecuteEvents.pointerClickHandler);
                Refresh(null); Check(pinned.Count == 2 && paths.Count == 5, "Multiple pinned nodes combine their direct paths without duplicates");
                ExecuteEvents.Execute(pins[parentA.World].gameObject, pinPointer, ExecuteEvents.pointerClickHandler);
                Refresh(null); Check(pinned.Count == 1 && paths.Count == 4, "Unpin removes only paths no longer needed by another pin");
                enabledSetting.Value = false; LateUpdate(); Check(paths.Count == 0, "Enabled setting immediately clears highlights");
                enabledSetting.Value = true;
                Refresh(null); Check(paths.Count == 4, "Re-enabling restores pinned highlights");
                ExecuteEvents.Execute(pins[center.World].gameObject, pinPointer, ExecuteEvents.pointerClickHandler);
                Refresh(null); Check(pinned.Count == 0 && paths.Count == 0, "Second click unpins the node and clears its paths");
                Check(serialized != diagram.ToJson().ToString(), "Diagnostic rewiring really altered the disposable graph");
                InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(1, 1) });
            }
            catch (Exception ex) { error = ex.ToString(); }

            yield return new WaitForSecondsRealtime(.3f);
            if (error == null) try
            {
                Check(paths.Count == 0, "Moving mouse off the node clears highlights on next frame");
                Check(UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None).All(l => !l.name.StartsWith("PathFinder.")), "Cleanup leaves no orphan highlight renderers");
                Capture(Path.Combine(directory, "path-finder-clear.png"));
                // Allow a rendered frame for the new canvas to receive graphic depths.
                blocker = new GameObject("PathFinderDiagnosticPopup", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
                var blockerCanvas = blocker.GetComponent<Canvas>(); blockerCanvas.renderMode = RenderMode.ScreenSpaceOverlay; blockerCanvas.sortingOrder = 32000;
                var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(blocker.transform, false);
                var rect = (RectTransform)panel.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            catch (Exception ex) { error = ex.ToString(); }
            yield return new WaitForSecondsRealtime(.3f);
            if (error == null) try { Check(FindHoveredNode(hoverPosition) == null, "Popup blocks highlighting nodes underneath"); }
            catch (Exception ex) { error = ex.ToString(); }
            if (blocker) Destroy(blocker);
            if (error == null) try
            {
                var pinScreen = RectTransformUtility.WorldToScreenPoint(center.World.GetComponentInParent<Canvas>().worldCamera, pins[center.World].transform.position);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = pinScreen });
            }
            catch (Exception ex) { error = ex.ToString(); }
            yield return new WaitForSecondsRealtime(.15f);
            if (error == null) InputSystem.QueueStateEvent(mouse, new MouseState { position = mouse.position.ReadValue(), buttons = 1 });
            yield return new WaitForSecondsRealtime(.15f);
            if (error == null) InputSystem.QueueStateEvent(mouse, new MouseState { position = mouse.position.ReadValue() });
            yield return new WaitForSecondsRealtime(.2f);
            if (error == null) try
            {
                Check(pinned.Contains(center.World), "Real mouse press and release toggles the tiny pin");
                InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(1, 1) });
            }
            catch (Exception ex) { error = ex.ToString(); }
            yield return new WaitForSecondsRealtime(.2f);
            if (error == null) try
            {
                Check(paths.Count == 4 && pinned.Contains(center.World), "Pinned paths stay visible after real mouse movement away");
                Capture(Path.Combine(directory, "path-finder-pinned.png"));
                center.World.gameObject.SetActive(false); LateUpdate(); Check(paths.Count == 0, "Hidden pinned node does not leave stale highlights");
                center.World.gameObject.SetActive(true); LateUpdate(); Check(paths.Count == 4, "Pinned highlights return when node becomes visible");
                WorldFlowManager.Instance.RemoveElement(center);
            }
            catch (Exception ex) { error = ex.ToString(); }
            yield return new WaitForSecondsRealtime(.2f);
            if (error == null) try { Check(pinned.Count == 0 && paths.Count == 0, "Deleting a pinned node removes its pin and highlights"); }
            catch (Exception ex) { error = ex.ToString(); }
            if (error == null) yield return VerifyLeylineCompatibility(directory, mouse, message => error = error ?? message);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            var report = new JsonObject
            {
                { "passed", error == null }, { "version", Version }, { "gameVersion", Application.version },
                { "unityVersion", Application.unityVersion }, { "bepInExVersion", typeof(BaseUnityPlugin).Assembly.GetName().Version.ToString() },
                { "dllSha256", HashFile(typeof(PathFinderPlugin).Assembly.Location) },
                { "leylineCompatibility", leyline.Available },
                { "checks", new JsonArray(checks.Select(c => new JsonValue(c)).ToArray()) }, { "count", checks.Count }, { "error", error ?? "" }
            };
            File.WriteAllText(Path.Combine(directory, "runtime-tests.json"), report.ToString());
            if (error != null) Logger.LogError(error); else Logger.LogInfo("Path Finder diagnostic passed: " + checks.Count + " checks.");
            Application.Quit();
        }

        static string HashFile(string path)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        void VerifyOverlappingHighlights(WorldFlowElement center, string directory)
        {
            var objects = new List<GameObject>();
            var synthetic = new List<RainbowPath>();
            RainbowPath Make(Color color, params Vector3[] points)
            {
                var obj = new GameObject("PathFinderOverlapDiagnosticSource"); objects.Add(obj);
                var line = obj.AddComponent<LineRenderer>(); line.useWorldSpace = true;
                line.positionCount = points.Length; line.SetPositions(points);
                var path = new RainbowPath(line, rainbowMaterial, color); synthetic.Add(path); return path;
            }
            void Set(RainbowPath path, params Vector3[] points) { path.Source.positionCount = points.Length; path.Source.SetPositions(points); }
            Vector3 At(RainbowPath path, float coordinate, bool useY = false)
            {
                for (int i = 1; i < path.Display.Count; i++)
                {
                    var a = path.Display[i - 1]; var b = path.Display[i];
                    float start = useY ? a.y : a.x, end = useY ? b.y : b.x;
                    if (Mathf.Abs(end - start) < .001f || coordinate < Mathf.Min(start, end) - .001f || coordinate > Mathf.Max(start, end) + .001f) continue;
                    return Vector3.Lerp(a, b, (coordinate - start) / (end - start));
                }
                throw new InvalidOperationException("Diagnostic sample not on displayed path");
            }
            try
            {
                var a = Make(Color.red, Vector3.zero, new Vector3(400, 0));
                var b = Make(Color.cyan, Vector3.zero, new Vector3(400, 0));
                var group = new List<RainbowPath> { a, b }; var layout = new PathLaneLayout();
                layout.Refresh(group, 5);
                Check(Mathf.Abs(At(a, 200).y - At(b, 200).y) >= 7.99f, "Fully coincident horizontal pipes retain two separate colored lanes");
                Check(a.Display[0] == Vector3.zero && a.Display[a.Display.Count - 1] == new Vector3(400, 0), "Shared straight highlights still meet the original endpoints");
                Check(a.Source.positionCount == 2 && a.Source.GetPosition(0) == Vector3.zero && a.Source.GetPosition(1) == new Vector3(400, 0), "Overlap layout never modifies the native pipe renderer positions");
                Set(b, new Vector3(400, 0), Vector3.zero); layout.Refresh(group, 5);
                Check(Mathf.Abs(At(a, 200).y - At(b, 200).y) >= 7.99f, "Opposite pipe directions use the same lane orientation");
                Set(b, new Vector3(100, 0), new Vector3(300, 0)); layout.Refresh(group, 5);
                Check(Mathf.Abs(At(a, 200).y - At(b, 200).y) >= 7.99f, "Partially coincident runs separate throughout their shared center");
                Check(Mathf.Abs(At(a, 50).y) < .001f && Mathf.Abs(At(a, 350).y) < .001f, "Nonoverlapping portions stay on their original centerline");
                Set(a, Vector3.zero, new Vector3(0, 400)); Set(b, new Vector3(0, 400), Vector3.zero); layout.Refresh(group, 5);
                Check(Mathf.Abs(At(a, 200, true).x - At(b, 200, true).x) >= 7.99f, "Reversed vertical overlaps keep both colors visible");
                Set(a, Vector3.zero, new Vector3(400, 400)); Set(b, Vector3.zero, new Vector3(400, 400)); layout.Refresh(group, 5);
                Check(Mathf.Abs((At(a, 200).y - 200) - (At(b, 200).y - 200)) > 10, "Diagonal straight overlaps receive distinct parallel lanes");
                Set(a, Vector3.zero, new Vector3(400, 0)); Set(b, new Vector3(200, -200), new Vector3(200, 200)); layout.Refresh(group, 5);
                Check(a.Display.Count == 2 && b.Display.Count == 2, "Ordinary crossing pipes are not treated as shared straight runs");
                Set(b, new Vector3(0, 20), new Vector3(400, 20)); layout.Refresh(group, 5);
                Check(a.Display.Count == 2 && b.Display.Count == 2 && At(b, 200).y == 20, "Separate parallel pipes retain their existing geometry");
                Set(a, Vector3.zero, new Vector3(200, 0), new Vector3(400, 0)); Set(b, Vector3.zero, new Vector3(400, 0)); layout.Refresh(group, 5);
                Check(Mathf.Abs(At(a, 200).y - At(b, 200).y) >= 7.99f, "Redundant straight pipe handles do not pinch overlapping colors together");
                var c = Make(Color.yellow, Vector3.zero, new Vector3(400, 0));
                var d = Make(Color.green, new Vector3(400, 0), Vector3.zero);
                group.Add(c); group.Add(d); layout.Refresh(group, 5);
                var lanes = group.Select(p => At(p, 200).y).OrderBy(y => y).ToArray();
                Check(lanes.Zip(lanes.Skip(1), (x, y) => y - x).All(gap => gap >= 7.99f), "Four fully aligned pipes keep four individually visible neon cores");
                Check(group.All(p => p.Layers[2].startColor == p.Layers[2].endColor), "Overlap correction preserves one solid color per line");
                b.Source.enabled = false; layout.Refresh(group, 5);
                Check(b.Layers.All(l => !l.enabled), "Disabled sources cannot leave a highlight lane visible");
                b.Source.enabled = true; layout.Refresh(group, 5);
                Check(b.Layers.All(l => l.enabled), "Re-enabling a pipe restores its overlapping highlight");
                layout.Refresh(group, 12); lanes = group.Select(p => At(p, 200).y).OrderBy(y => y).ToArray();
                Check(lanes.Zip(lanes.Skip(1), (x, y) => y - x).All(gap => gap >= 19.19f), "Overlap spacing follows the maximum configured line width");
                layout.Refresh(group, 2); lanes = group.Select(p => At(p, 200).y).OrderBy(y => y).ToArray();
                Check(lanes.Zip(lanes.Skip(1), (x, y) => y - x).All(gap => gap >= 3.19f), "Overlap spacing follows the minimum configured line width");
                group.Remove(c); group.Remove(d); b.Source.useWorldSpace = a.Source.useWorldSpace = false;
                a.Source.transform.position = b.Source.transform.position = new Vector3(30, 20);
                Set(a, Vector3.zero, new Vector3(400, 0)); Set(b, Vector3.zero, new Vector3(400, 0)); layout.Refresh(group, 5);
                Check(Mathf.Abs(At(a, 230).y - At(b, 230).y) >= 7.99f, "Overlap layout supports transformed local-space source renderers");
                Check(a.Layers[2].GetPosition(0) == Vector3.zero && a.Source.GetPosition(0) == Vector3.zero, "Local-space endpoints and native coordinates remain unchanged");
                group.Remove(b); layout.Refresh(group, 5);
                Check(a.Display.Count == 2 && At(a, 230).y == 20, "Removing the other highlight restores a single native straight line");
                b.Source.useWorldSpace = a.Source.useWorldSpace = true;
                a.Source.transform.position = b.Source.transform.position = Vector3.zero;
                group.Add(b); Set(a, Vector3.zero, new Vector3(10, 0)); Set(b, Vector3.zero, new Vector3(10, 0)); layout.Refresh(group, 5);
                Check(Mathf.Abs(At(a, 5).y - At(b, 5).y) >= 7.99f, "Short coincident runs also retain distinct colored cores");
            }
            finally
            {
                foreach (var path in synthetic) path.Dispose();
                foreach (var obj in objects) { obj.SetActive(false); Destroy(obj); }
            }

            // Reproduce four real model pipes sharing exactly the same straight section.
            var selected = paths.Keys.ToArray();
            var bends = selected.Select(pipe => pipe.Nodes.ToArray()).ToArray();
            try
            {
                foreach (var pipe in selected)
                {
                    pipe.Nodes.Clear(); pipe.Nodes.Add(new Vector2Int(-300, 440)); pipe.Nodes.Add(new Vector2Int(300, 440));
                    pipe.World.UpdateLinePositions();
                }
                string state = GamePlayer.Current.ActiveFlow.ToJson().ToString();
                Refresh(center);
                var nativeLanes = paths.Values.Select(p => At(p, 0).y).OrderBy(y => y).ToArray();
                Check(nativeLanes.Length == 4 && nativeLanes.Zip(nativeLanes.Skip(1), (x, y) => y - x).All(gap => gap >= 7.99f), "Real node pipes sharing a horizontal run render all four colors side by side");
                Check(state == GamePlayer.Current.ActiveFlow.ToJson().ToString(), "Shared-run rendering leaves serialized pipe data untouched");
                Capture(Path.Combine(directory, "path-finder-overlapping-paths.png"));
            }
            finally
            {
                for (int i = 0; i < selected.Length; i++) { selected[i].Nodes.Clear(); selected[i].Nodes.AddRange(bends[i]); selected[i].World.UpdateLinePositions(); }
                Refresh(center);
            }
        }

        static void Capture(string path)
        {
            Canvas.ForceUpdateCanvases();
            var target = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);
            var previous = RenderTexture.active;
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            try
            {
                foreach (var camera in Camera.allCameras.OrderBy(c => c.depth))
                {
                    var old = camera.targetTexture;
                    try { camera.targetTexture = target; camera.Render(); }
                    finally { camera.targetTexture = old; }
                }
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); texture.Apply();
                File.WriteAllBytes(path, ImageConversion.EncodeToPNG(texture));
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); Destroy(texture); }
        }

        static void CapturePinStyles(string path)
        {
            // Render the actual UI mesh in three states for close visual inspection.
            var cameraObject = new GameObject("PathFinderStyleCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true; camera.orthographicSize = 18;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.045f, .032f, .045f);
            camera.cullingMask = 1 << 30;
            var root = new GameObject("PathFinderStyleCanvas", typeof(RectTransform), typeof(Canvas));
            root.layer = 30;
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
            ((RectTransform)root.transform).sizeDelta = new Vector2(130, 36);
            for (int i = 0; i < 3; i++)
            {
                var obj = new GameObject("Style" + i, typeof(RectTransform), typeof(PinSymbol));
                obj.layer = 30; obj.transform.SetParent(root.transform, false);
                var rect = (RectTransform)obj.transform; rect.sizeDelta = new Vector2(18, 18); rect.anchoredPosition = new Vector2((i - 1) * 40, 0);
                var symbol = obj.GetComponent<PinSymbol>(); symbol.Hover = i == 1 ? 1 : 0; symbol.Pinned = i == 2;
            }
            var target = RenderTexture.GetTemporary(720, 200, 24);
            var previous = RenderTexture.active;
            var texture = new Texture2D(720, 200, TextureFormat.RGB24, false);
            try
            {
                Canvas.ForceUpdateCanvases(); camera.targetTexture = target; camera.Render();
                RenderTexture.active = target; texture.ReadPixels(new Rect(0, 0, 720, 200), 0, 0); texture.Apply();
                File.WriteAllBytes(path, ImageConversion.EncodeToPNG(texture));
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target); Destroy(texture); root.SetActive(false); Destroy(root); Destroy(cameraObject);
            }
        }

        [HarmonyPatch(typeof(SaveGame), nameof(SaveGame.Store))] static class NoSave { static bool Prefix() => !Diagnostic; }
        [HarmonyPatch(typeof(SaveGame), nameof(SaveGame.StoreAutosaveState))] static class NoAutosave { static bool Prefix() => !Diagnostic; }
        [HarmonyPatch(typeof(SaveGame), nameof(SaveGame.LoadLatestSave))] static class NoLoad
        { static bool Prefix(ref bool __result) { if (!Diagnostic) return true; __result = false; return false; } }
        [HarmonyPatch(typeof(SteamAchievement), nameof(SteamAchievement.Trigger))] static class NoAchievement { static bool Prefix() => !Diagnostic; }
        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Initialized), MethodType.Getter)] static class Offline
        { static bool Prefix(ref bool __result) { if (!Diagnostic) return true; __result = false; return false; } }
    }
}
