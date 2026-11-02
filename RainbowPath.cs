using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace KingdomPathFinder
{
    internal sealed class RainbowPath : IDisposable
    {
        internal readonly LineRenderer Source;
        internal readonly Color Color;
        internal readonly LineRenderer[] Layers = new LineRenderer[3];
        internal readonly List<Vector3> Original = new List<Vector3>();
        internal readonly List<Vector3> Route = new List<Vector3>();
        internal readonly List<Vector3> Display = new List<Vector3>();
        static int nextId;
        internal readonly int Id = ++nextId;
        Vector3[] positions = Array.Empty<Vector3>();
        bool wasVisible;
        internal bool Alive => Source && Source.gameObject.activeInHierarchy;

        internal RainbowPath(LineRenderer source, Material material, Color color)
        {
            Source = source;
            Color = color;
            for (int i = 0; i < Layers.Length; i++)
            {
                var obj = new GameObject("PathFinder." + (i == 2 ? "RainbowCore" : "Glow" + i));
                obj.layer = source.gameObject.layer;
                obj.transform.SetParent(source.transform, false);
                var line = obj.AddComponent<LineRenderer>();
                line.sharedMaterial = material;
                line.widthCurve = AnimationCurve.Constant(0, 1, 1);
                line.alignment = source.alignment;
                line.textureMode = LineTextureMode.Stretch;
                line.numCornerVertices = 5;
                line.numCapVertices = 5;
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                var tint = color; tint.a = i == 2 ? 1f : i == 1 ? .34f : .13f;
                line.startColor = tint; line.endColor = tint;
                Layers[i] = line;
            }
        }

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

        public void Dispose()
        {
            foreach (var line in Layers)
                if (line) { line.enabled = false; UnityEngine.Object.Destroy(line.gameObject); }
        }
    }
}
