using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace KingdomPathFinder
{
    internal sealed partial class RainbowPath : IDisposable
    {
        internal bool ReadGeometry()
        {
            bool changed = Original.Count != Source.positionCount;
            bool visible = Source.enabled && Source.gameObject.activeInHierarchy;
            changed |= wasVisible != visible;
            wasVisible = visible;
            for (int i = 0; i < Source.positionCount; i++)
            {
                var point = Source.GetPosition(i);
                if (!Source.useWorldSpace) point = Source.transform.TransformPoint(point);
                if (i >= Original.Count) Original.Add(point);
                else { changed |= (Original[i] - point).sqrMagnitude > .000001f; Original[i] = point; }
            }
            if (Original.Count > Source.positionCount) Original.RemoveRange(Source.positionCount, Original.Count - Source.positionCount);
            return changed;
        }
    }
}
