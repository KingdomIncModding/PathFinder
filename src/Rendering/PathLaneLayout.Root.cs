using System;
using System.Collections.Generic;
using UnityEngine;

namespace KingdomPathFinder
{
    internal sealed partial class PathLaneLayout
    {
        int Root(int index)
        {
            while (parents[index] != index) { parents[index] = parents[parents[index]]; index = parents[index]; }
            return index;
        }
    }
}
