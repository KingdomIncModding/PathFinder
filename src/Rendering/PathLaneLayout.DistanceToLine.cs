using System;
using System.Collections.Generic;
using UnityEngine;

namespace KingdomPathFinder
{
    internal sealed partial class PathLaneLayout
    {
        static float DistanceToLine(Vector3 point, Segment line)
        {
            var delta = point - line.Start;
            return (delta - line.Direction * Vector3.Dot(delta, line.Direction)).magnitude;
        }
    }
}
