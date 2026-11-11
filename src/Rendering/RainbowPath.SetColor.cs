using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace KingdomPathFinder
{
    internal sealed partial class RainbowPath : IDisposable
    {
        internal void SetColor(Color color)
        {
            if (Color == color) return;
            Color = color;
            for (int i = 0; i < Layers.Length; i++)
            {
                var tint = color; tint.a = i == 2 ? 1f : i == 1 ? .34f : .13f;
                Layers[i].startColor = tint; Layers[i].endColor = tint;
            }
        }
    }
}
