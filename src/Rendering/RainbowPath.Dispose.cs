using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace KingdomPathFinder
{
    internal sealed partial class RainbowPath : IDisposable
    {
        public void Dispose()
        {
            foreach (var line in Layers)
                if (line) { line.enabled = false; UnityEngine.Object.Destroy(line.gameObject); }
        }
    }
}
