using System;
using System.Collections.Generic;
using UnityEngine;

namespace KingdomPathFinder
{
    internal sealed partial class PathLaneLayout
    {
        static float Fade(Span span, float width, float offset)
            => Mathf.Max(.001f, Mathf.Min((span.End - span.Start) * .25f, Mathf.Max(width * 4f, Mathf.Abs(offset) * 2f)));
    }
}
