using System;
using System.Collections.Generic;
using UnityEngine;

namespace KingdomPathFinder
{
    internal sealed partial class PathLaneLayout
    {
        partial struct Segment
        {
            internal int Path, Index;
            internal Vector3 Start, Direction;
            internal float Length;
        }
    }
}
