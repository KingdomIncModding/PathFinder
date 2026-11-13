using System;
using System.Collections.Generic;
using UnityEngine;

namespace KingdomPathFinder
{
    internal struct PathLaneLayoutSegment
    {
        internal int Path, Index;
        internal Vector3 Start, Direction;
        internal float Length;
    }
}
