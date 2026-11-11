using System;
using System.Collections.Generic;
using UnityEngine;

namespace KingdomPathFinder
{
    internal sealed partial class PathLaneLayout
    {
        partial struct Span
        {
            internal int Path, Segment;
            internal float Start, End;
        }
    }
}
