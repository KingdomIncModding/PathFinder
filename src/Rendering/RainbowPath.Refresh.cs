using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace KingdomPathFinder
{
    internal sealed partial class RainbowPath : IDisposable
    {
        internal void Refresh(float width)
        {
            if (positions.Length != Display.Count) positions = new Vector3[Display.Count];
            for (int i = 0; i < Display.Count; i++)
                positions[i] = Source.useWorldSpace ? Display[i] : Source.transform.InverseTransformPoint(Display[i]);
            for (int i = 0; i < Layers.Length; i++)
            {
                var line = Layers[i];
                line.enabled = Source.enabled && Source.positionCount >= 2;
                line.useWorldSpace = Source.useWorldSpace;
                line.sortingLayerID = Source.sortingLayerID;
                line.sortingOrder = Source.sortingOrder + i + 1;
                line.widthMultiplier = width * (i == 0 ? 3f : i == 1 ? 1.9f : 1f);
                line.positionCount = positions.Length;
                line.SetPositions(positions);
            }
        }
    }
}
