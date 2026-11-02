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
        readonly List<Vector3> samples = new List<Vector3>();
        Vector3[] positions = Array.Empty<Vector3>();
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

        internal void Refresh(float width)
        {
            samples.Clear();
            if (Source.positionCount > 0) samples.Add(Source.GetPosition(0));
            for (int i = 1; i < Source.positionCount; i++)
            {
                var a = Source.GetPosition(i - 1);
                var b = Source.GetPosition(i);
                samples.Add(b);
            }
            if (positions.Length != samples.Count) positions = new Vector3[samples.Count];
            samples.CopyTo(positions);
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
