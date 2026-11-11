using Assets.Behaviour.Flow;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomPathFinder
{
    internal sealed partial class PinSymbol : Graphic
    {
        internal bool Pinned, Pressed;
        internal float Hover;
        static readonly Vector2[] Octagon = {
            new Vector2(-5, 9), new Vector2(5, 9), new Vector2(9, 5), new Vector2(9, -5),
            new Vector2(5, -9), new Vector2(-5, -9), new Vector2(-9, -5), new Vector2(-9, 5)
        };


        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            float scale = Mathf.Min(r.width, r.height) / 18f;
            Color cyan = new Color(.08f, .95f, 1f);
            // A compact bronze socket with a clipped-corner silhouette and inset enamel.
            FillOctagon(vh, r.center + Vector2.down * scale, scale * 1.07f, new Color(.015f, .008f, .015f, .9f));
            if (Pinned)
            {
                FillOctagon(vh, r.center, scale * 1.16f, new Color(.02f, .8f, 1f, .12f));
                FillOctagon(vh, r.center, scale * 1.06f, new Color(.04f, .9f, 1f, .3f));
            }
            FillOctagon(vh, r.center, scale, new Color(.11f, .06f, .025f));
            for (int i = 0; i < Octagon.Length; i++)
            {
                int j = (i + 1) % Octagon.Length;
                float light = Mathf.Clamp01((Octagon[i].y + Octagon[j].y + 18f) / 36f);
                Color bronze = Color.Lerp(new Color(.29f, .13f, .055f), new Color(.95f, .72f, .36f), light);
                bronze = Color.Lerp(bronze, new Color(1f, .86f, .52f), Hover * .28f);
                Quad(vh, r.center, scale, bronze, Octagon[i] * .95f, Octagon[j] * .95f, Octagon[j] * .77f, Octagon[i] * .77f);
            }
            FillOctagon(vh, r.center, scale * .77f, Pinned ? new Color(.025f, .34f, .37f) : new Color(.065f, .025f, .045f));
            FillOctagon(vh, r.center, scale * .68f, Pinned ? new Color(.025f, .1f, .13f) : new Color(.15f, .045f, .075f));

            var offset = Pressed ? new Vector2(.4f, -.65f) : Vector2.zero;
            DrawTack(vh, r.center, scale, offset + new Vector2(.7f, -.8f), new Color(0, 0, 0, .65f), false);
            Color tack = Pinned ? cyan : Color.Lerp(new Color(.94f, .79f, .46f), new Color(1f, .95f, .73f), Hover);
            DrawTack(vh, r.center, scale, offset, tack, true);
            // A small jewel glint makes the locked state readable at native UI size.
            if (Pinned)
                Quad(vh, r.center, scale, new Color(.8f, 1f, 1f), new Vector2(5, 5.8f), new Vector2(5.8f, 5), new Vector2(5, 4.2f), new Vector2(4.2f, 5));
        }
    }
}
