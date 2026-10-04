using Synaps.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Synaps.UI
{
    public sealed class GraphNodeView : MonoBehaviour
    {
        public string NodeId { get; private set; }
        private Text title;
        private Text detail;
        private Image background;

        public static GraphNodeView Create(Transform parent, KnowledgeNode node)
        {
            var image = UiElements.Panel("Node " + node.Id, parent, Vector2.zero, new Vector2(220f, 86f), Color.white);
            var view = image.gameObject.AddComponent<GraphNodeView>();
            view.NodeId = node.Id;
            view.background = image;
            view.title = UiElements.Label("Title", image.transform, new Vector2(0f, 15f), new Vector2(194f, 38f), "", 18);
            view.detail = UiElements.Label("Type", image.transform, new Vector2(0f, -24f), new Vector2(194f, 20f), "", 13);
            view.Render(node, false);
            return view;
        }

        public void Render(KnowledgeNode node, bool selected)
        {
            var rect = (RectTransform)transform;
            rect.localPosition = new Vector3(node.Position.X * 100f, node.Position.Y * 100f, node.Position.Z * 100f);
            title.text = node.Title;
            detail.text = node.NodeType + (string.IsNullOrEmpty(node.Summary) ? "" : " / summarized");
            background.color = selected ? new Color(0.12f, 0.43f, 0.41f) : new Color(0.15f, 0.23f, 0.34f);
        }
    }
}
