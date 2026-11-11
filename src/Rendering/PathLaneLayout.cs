using System;
using System.Collections.Generic;
using UnityEngine;

namespace KingdomPathFinder
{
    // Only the highlight meshes move. Native pipes, colliders and saved bends stay intact.
    internal sealed partial class PathLaneLayout
    {
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
    }
}
