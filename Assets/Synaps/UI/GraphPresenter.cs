using System;
using System.Collections.Generic;
using Synaps.Core;
using Synaps.KnowledgeGraph;
using UnityEngine;
using UnityEngine.UI;

namespace Synaps.UI
{
    public sealed class GraphPresenter : IDisposable
    {
        private sealed class EdgeView { public GraphEdgeGraphic Graphic; public Text Label; }
        private readonly KnowledgeGraphService graph;
        private readonly WorkspaceController workspace;
        private readonly RectTransform edgeLayer;
        private readonly RectTransform nodeLayer;
        private readonly Action<GraphNodeView> configureInteraction;
        private readonly Dictionary<string, GraphNodeView> nodes = new Dictionary<string, GraphNodeView>();
        private readonly Dictionary<string, EdgeView> edges = new Dictionary<string, EdgeView>();

        public GraphPresenter(KnowledgeGraphService graph, WorkspaceController workspace, RectTransform root,
            Action<GraphNodeView> configureInteraction)
        {
            this.graph = graph;
            this.workspace = workspace;
            this.configureInteraction = configureInteraction;
            edgeLayer = UiElements.Rect("Edges", root, Vector2.zero, root.sizeDelta);
            nodeLayer = UiElements.Rect("Nodes", root, Vector2.zero, root.sizeDelta);
            graph.Changed += Render;
            workspace.SelectionChanged += Render;
            Render();
        }

        private void Render()
        {
            var snapshot = graph.Snapshot();
            var liveNodes = new HashSet<string>();
            var nodeData = new Dictionary<string, KnowledgeNode>();
            foreach (var node in snapshot.Nodes)
            {
                liveNodes.Add(node.Id);
                nodeData.Add(node.Id, node);
                GraphNodeView view;
                if (!nodes.TryGetValue(node.Id, out view))
                {
                    view = GraphNodeView.Create(nodeLayer, node);
                    nodes.Add(node.Id, view);
                    configureInteraction(view);
                }
                view.Render(node, node.Id == workspace.SelectedNodeId);
            }
            foreach (var id in new List<string>(nodes.Keys))
                if (!liveNodes.Contains(id)) { nodes[id].gameObject.SetActive(false); UnityEngine.Object.Destroy(nodes[id].gameObject); nodes.Remove(id); }

            var liveEdges = new HashSet<string>();
            foreach (var edge in snapshot.Edges)
            {
                liveEdges.Add(edge.Id);
                EdgeView view;
                if (!edges.TryGetValue(edge.Id, out view))
                {
                    var rect = UiElements.Rect("Edge " + edge.Id, edgeLayer, Vector2.zero, edgeLayer.sizeDelta);
                    var graphic = rect.gameObject.AddComponent<GraphEdgeGraphic>();
                    graphic.raycastTarget = false;
                    graphic.color = UiElements.Accent;
                    view = new EdgeView { Graphic = graphic, Label = UiElements.Label("Relation", rect,
                        Vector2.zero, new Vector2(180f, 26f), edge.RelationType, 14, TextAnchor.MiddleCenter) };
                    edges.Add(edge.Id, view);
                }
                var from = nodeData[edge.FromNodeId].Position;
                var to = nodeData[edge.ToNodeId].Position;
                var a = new Vector2(from.X * 100f, from.Y * 100f);
                var b = new Vector2(to.X * 100f, to.Y * 100f);
                var direction = (b - a).normalized;
                var fromOffset = BoundaryOffset(direction);
                var toOffset = BoundaryOffset(-direction);
                var hasSpace = (b - a).magnitude > fromOffset + toOffset + 6f;
                view.Graphic.SetEndpoints(hasSpace ? a + direction * fromOffset : Vector2.zero,
                    hasSpace ? b - direction * toOffset : Vector2.zero);
                view.Label.enabled = hasSpace;
                view.Label.rectTransform.anchoredPosition = (a + b) / 2f + new Vector2(-direction.y, direction.x) * 18f;
            }
            foreach (var id in new List<string>(edges.Keys))
                if (!liveEdges.Contains(id)) { UnityEngine.Object.Destroy(edges[id].Graphic.gameObject); edges.Remove(id); }
        }

        private static float BoundaryOffset(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.01f) return 0f;
            var x = Mathf.Abs(direction.x) < 0.001f ? float.PositiveInfinity : 110f / Mathf.Abs(direction.x);
            var y = Mathf.Abs(direction.y) < 0.001f ? float.PositiveInfinity : 43f / Mathf.Abs(direction.y);
            return Mathf.Min(x, y) + 3f;
        }

        public void Dispose()
        {
            graph.Changed -= Render;
            workspace.SelectionChanged -= Render;
            if (edgeLayer != null) UnityEngine.Object.Destroy(edgeLayer.gameObject);
            if (nodeLayer != null) UnityEngine.Object.Destroy(nodeLayer.gameObject);
        }
    }
}
