using UnityEngine;
using UnityEngine.UI;

namespace Synaps.UI
{
    public sealed class GraphEdgeGraphic : Graphic
    {
        private Vector2 start;
        private Vector2 end;
        public void SetEndpoints(Vector2 from, Vector2 to) { start = from; end = to; SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            var delta = end - start;
            if (delta.sqrMagnitude < 1f) return;
            var direction = delta.normalized;
            var side = new Vector2(-direction.y, direction.x);
            Quad(helper, start + side * 1.4f, end + side * 1.4f, end - side * 1.4f, start - side * 1.4f);
            var index = helper.currentVertCount;
            helper.AddVert(end, color, Vector2.zero);
            helper.AddVert(end - direction * 13f + side * 7f, color, Vector2.zero);
            helper.AddVert(end - direction * 13f - side * 7f, color, Vector2.zero);
            helper.AddTriangle(index, index + 1, index + 2);
        }

        private void Quad(VertexHelper helper, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            var i = helper.currentVertCount;
            helper.AddVert(a, color, Vector2.zero); helper.AddVert(b, color, Vector2.zero);
            helper.AddVert(c, color, Vector2.zero); helper.AddVert(d, color, Vector2.zero);
            helper.AddTriangle(i, i + 1, i + 2); helper.AddTriangle(i, i + 2, i + 3);
        }
    }
}
