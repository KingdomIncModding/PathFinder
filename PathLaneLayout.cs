using System;
using System.Collections.Generic;
using UnityEngine;

namespace KingdomPathFinder
{
    // Only the highlight meshes move. Native pipes, colliders and saved bends stay intact.
    internal sealed class PathLaneLayout
    {
        struct Segment
        {
            internal int Path, Index;
            internal Vector3 Start, Direction;
            internal float Length;
        }

        struct Span
        {
            internal int Path, Segment;
            internal float Start, End;
        }

        readonly List<RainbowPath> previous = new List<RainbowPath>();
        readonly List<Segment> segments = new List<Segment>();
        readonly List<Span> overlaps = new List<Span>();
        readonly List<float> stops = new List<float>();
        int[] parents = Array.Empty<int>();
        float lastWidth = -1;

        internal void Refresh(List<RainbowPath> paths, float width)
        {
            bool changed = !Mathf.Approximately(lastWidth, width) || previous.Count != paths.Count;
            for (int i = 0; i < paths.Count; i++)
            {
                changed |= i >= previous.Count || !ReferenceEquals(previous[i], paths[i]);
                changed |= paths[i].ReadGeometry();
            }
            if (changed)
            {
                previous.Clear(); previous.AddRange(paths); lastWidth = width;
                Build(paths, width);
            }
            foreach (var path in paths) path.Refresh(width);
        }

        int Root(int index)
        {
            while (parents[index] != index) { parents[index] = parents[parents[index]]; index = parents[index]; }
            return index;
        }

        void Build(List<RainbowPath> paths, float width)
        {
            segments.Clear(); overlaps.Clear();
            if (parents.Length < paths.Count) parents = new int[paths.Count];
            for (int p = 0; p < paths.Count; p++)
            {
                parents[p] = p;
                var path = paths[p];
                path.Route.Clear();
                // Ignore redundant collinear control points when routing an overlap,
                // so a straight shared run does not pinch together at every pipe handle.
                foreach (var point in path.Original)
                {
                    if (path.Route.Count > 0 && (point - path.Route[path.Route.Count - 1]).sqrMagnitude < .000001f) continue;
                    while (path.Route.Count >= 2)
                    {
                        var a = path.Route[path.Route.Count - 1] - path.Route[path.Route.Count - 2];
                        var b = point - path.Route[path.Route.Count - 1];
                        if (Vector3.Dot(a.normalized, b.normalized) < .999999f) break;
                        path.Route.RemoveAt(path.Route.Count - 1);
                    }
                    path.Route.Add(point);
                }
                if (!path.Source.enabled) continue;
                for (int i = 0; i + 1 < path.Route.Count; i++)
                {
                    var delta = path.Route[i + 1] - path.Route[i];
                    float length = delta.magnitude;
                    if (length < .001f) continue;
                    segments.Add(new Segment { Path = p, Index = i, Start = path.Route[i], Direction = delta / length, Length = length });
                }
            }

            for (int i = 0; i < segments.Count; i++)
            for (int j = i + 1; j < segments.Count; j++)
            {
                var a = segments[i]; var b = segments[j];
                if (a.Path == b.Path || Mathf.Abs(Vector3.Dot(a.Direction, b.Direction)) < .99999f) continue;
                float b0 = Vector3.Dot(b.Start - a.Start, a.Direction);
                float b1 = Vector3.Dot(b.Start + b.Direction * b.Length - a.Start, a.Direction);
                float start = Mathf.Max(0, Mathf.Min(b0, b1));
                float end = Mathf.Min(a.Length, Mathf.Max(b0, b1));
                if (end - start < .01f) continue;
                var startPoint = a.Start + a.Direction * start;
                var endPoint = a.Start + a.Direction * end;
                float tolerance = width * .6f;
                // Parallel lines closer than the core width can hide each other too.
                if (DistanceToLine(startPoint, b) > tolerance || DistanceToLine(endPoint, b) > tolerance) continue;
                float other0 = Vector3.Dot(startPoint - b.Start, b.Direction);
                float other1 = Vector3.Dot(endPoint - b.Start, b.Direction);
                overlaps.Add(new Span { Path = a.Path, Segment = a.Index, Start = start, End = end });
                overlaps.Add(new Span { Path = b.Path, Segment = b.Index, Start = Mathf.Max(0, Mathf.Min(other0, other1)), End = Mathf.Min(b.Length, Mathf.Max(other0, other1)) });
                parents[Root(b.Path)] = Root(a.Path);
            }

            for (int p = 0; p < paths.Count; p++)
            {
                var path = paths[p];
                path.Display.Clear();
                int count = 0, rank = 0;
                for (int other = 0; other < paths.Count; other++)
                    if (Root(other) == Root(p)) { count++; if (paths[other].Id < path.Id) rank++; }
                float offset = (rank - (count - 1) * .5f) * width * 1.6f;
                if (count == 1 || Mathf.Abs(offset) < .001f)
                {
                    path.Display.AddRange(path.Original);
                    continue;
                }
                if (path.Route.Count > 0) path.Display.Add(path.Route[0]);
                foreach (var segment in segments)
                {
                    if (segment.Path != p) continue;
                    stops.Clear(); stops.Add(0); stops.Add(segment.Length);
                    foreach (var span in overlaps)
                    {
                        if (span.Path != p || span.Segment != segment.Index) continue;
                        float fade = Fade(span, width, offset);
                        stops.Add(span.Start); stops.Add(span.Start + fade * .5f); stops.Add(span.Start + fade);
                        stops.Add((span.Start + span.End) * .5f);
                        stops.Add(span.End - fade); stops.Add(span.End - fade * .5f); stops.Add(span.End);
                    }
                    stops.Sort();
                    var direction = segment.Direction;
                    if (direction.x < -.0001f || (Mathf.Abs(direction.x) <= .0001f && direction.y < 0)) direction = -direction;
                    var normal = new Vector3(-direction.y, direction.x, 0).normalized;
                    float previousStop = -1;
                    foreach (float stop in stops)
                    {
                        if (stop < .0001f || stop - previousStop < .001f) continue;
                        previousStop = stop;
                        float influence = 0;
                        foreach (var span in overlaps)
                        {
                            if (span.Path != p || span.Segment != segment.Index || stop < span.Start || stop > span.End) continue;
                            float fade = Fade(span, width, offset);
                            float factor = Mathf.Min((stop - span.Start) / fade, (span.End - stop) / fade);
                            influence = Mathf.Max(influence, Mathf.SmoothStep(0, 1, Mathf.Clamp01(factor)));
                        }
                        path.Display.Add(segment.Start + segment.Direction * stop + normal * (offset * influence));
                    }
                }
            }
        }

        static float Fade(Span span, float width, float offset)
            => Mathf.Max(.001f, Mathf.Min((span.End - span.Start) * .25f, Mathf.Max(width * 4f, Mathf.Abs(offset) * 2f)));

        static float DistanceToLine(Vector3 point, Segment line)
        {
            var delta = point - line.Start;
            return (delta - line.Direction * Vector3.Dot(delta, line.Direction)).magnitude;
        }
    }
}
