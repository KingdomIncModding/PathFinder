using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace KingdomPathFinder
{
    internal sealed partial class RainbowPath : IDisposable
    {
        internal readonly LineRenderer Source;
        internal Color Color { get; private set; }
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
    }
}
