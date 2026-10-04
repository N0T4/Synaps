using System;
using System.Collections.Generic;
using Synaps.Core;
using Synaps.Search;
using UnityEngine;
using UnityEngine.UI;

namespace Synaps.UI
{
    // Lightweight world-space presentation. Replace layout/prefabs independently of the domain.
    public sealed class DesktopWorkspaceView : MonoBehaviour
    {
        public InputField QueryField { get; private set; }
        public InputField RelationField { get; private set; }
        public Button SearchButton { get; private set; }
        public Button SaveGraphButton { get; private set; }
        public Button ConnectButton { get; private set; }
        public Button CancelButton { get; private set; }
        public Button DeleteButton { get; private set; }
        public RectTransform GraphRoot { get; private set; }
        public int ResultRevision { get; private set; }
        public event Action<SearchResult> SaveResultRequested;
        public event Action<SearchResult> SummaryRequested;
        private RectTransform resultRoot;
        private Text status;
        private Text selection;
        private readonly List<GameObject> cards = new List<GameObject>();
        private readonly Dictionary<string, Text> snippets = new Dictionary<string, Text>();

        public void Build(Camera eventCamera)
        {
            var root = UiElements.Rect("Synaps Workspace", transform, Vector2.zero, new Vector2(1500f, 950f));
            root.localScale = Vector3.one * 0.01f;
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = eventCamera;
            root.gameObject.AddComponent<GraphicRaycaster>();
            UiElements.Panel("Background", root, Vector2.zero, root.sizeDelta, new Color(0.035f, 0.055f, 0.09f));
            UiElements.Label("Brand", root, new Vector2(-380f, 423f), new Vector2(650f, 46f), "SYNAPS  /  Desktop prototype", 30);
            SaveGraphButton = UiElements.Button("Save graph", root, new Vector2(610f, 425f), new Vector2(170f, 44f));
            QueryField = UiElements.Field("Query", root, new Vector2(-110f, 345f), new Vector2(1160f, 56f), "Search research...");
            SearchButton = UiElements.Button("Search", root, new Vector2(610f, 345f), new Vector2(170f, 56f));
            UiElements.Label("Results heading", root, new Vector2(-410f, 276f), new Vector2(600f, 36f), "SEARCH RESULTS  /  offline fixtures", 21);
            UiElements.Label("Graph heading", root, new Vector2(300f, 276f), new Vector2(790f, 36f), "KNOWLEDGE GRAPH  /  click and drag", 21);
            resultRoot = UiElements.Rect("Results", root, new Vector2(-410f, -69f), new Vector2(600f, 630f));
            RelationField = UiElements.Field("Relation", root, new Vector2(125f, 226f), new Vector2(230f, 40f), "related_to");
            RelationField.text = "related_to";
            ConnectButton = UiElements.Button("Connect", root, new Vector2(313f, 226f), new Vector2(126f, 40f));
            CancelButton = UiElements.Button("Cancel", root, new Vector2(465f, 226f), new Vector2(126f, 40f));
            DeleteButton = UiElements.Button("Delete node", root, new Vector2(620f, 226f), new Vector2(140f, 40f));
            GraphRoot = (RectTransform)UiElements.Panel("Graph", root, new Vector2(300f, -92f),
                new Vector2(790f, 590f), new Color(0.07f, 0.095f, 0.14f)).transform;
            status = UiElements.Label("Status", root, new Vector2(-410f, -427f), new Vector2(600f, 70f), "Type a query, then press Search.", 17);
            selection = UiElements.Label("Selection", root, new Vector2(300f, -427f), new Vector2(790f, 70f),
                "Save cards to graph. Select a node, Connect, then select its target.", 17);
            SetReady(false);
        }

        public void SetReady(bool ready)
        {
            QueryField.interactable = ready;
            SearchButton.interactable = SaveGraphButton.interactable = RelationField.interactable = ready;
            ConnectButton.interactable = CancelButton.interactable = DeleteButton.interactable = ready;
        }

        public void SetStatus(string message, bool error = false)
        {
            status.text = message;
            status.color = error ? new Color(1f, 0.62f, 0.5f) : UiElements.Ink;
        }
        public void SetSelection(string message) { selection.text = message; }

        public void RenderSearch(SearchState state)
        {
            ++ResultRevision;
            foreach (var card in cards) { card.SetActive(false); Destroy(card); }
            cards.Clear();
            snippets.Clear();
            if (state.Status == SearchStatus.Searching) { SetStatus("Searching..."); return; }
            if (state.Status == SearchStatus.Failed) { SetStatus("Search failed: " + state.Error, true); return; }
            if (state.Status == SearchStatus.Cancelled) { SetStatus("Search cancelled."); return; }
            if (state.Status == SearchStatus.Idle) { SetStatus("Type a query, then press Search."); return; }
            SetStatus(state.Results.Count + " results. Save useful cards to the graph.");
            // Prototype has five fixed slots. A production provider needs pagination/virtualization.
            for (var i = 0; i < Math.Min(state.Results.Count, 5); i++) CreateCard(state.Results[i].Copy(), i);
        }

        public void SetResultSummary(string resultId, string summary, int revision)
        {
            Text label;
            if (revision == ResultRevision && snippets.TryGetValue(resultId, out label)) label.text = summary;
        }

        private void CreateCard(SearchResult result, int index)
        {
            var image = UiElements.Panel(result.Title, resultRoot, new Vector2(0f, 251f - index * 126f),
                new Vector2(600f, 116f), new Color(0.09f, 0.14f, 0.21f));
            cards.Add(image.gameObject);
            UiElements.Label("Title", image.transform, new Vector2(0f, 36f), new Vector2(560f, 26f), result.Title, 21);
            snippets[result.Id] = UiElements.Label("Snippet", image.transform, new Vector2(0f, 0f), new Vector2(560f, 40f), result.Snippet, 15);
            UiElements.Label("Source", image.transform, new Vector2(-190f, -36f), new Vector2(190f, 24f), result.Source, 14);
            var open = UiElements.Button("Open", image.transform, new Vector2(-24f, -36f), new Vector2(100f, 28f));
            Uri uri;
            open.interactable = Uri.TryCreate(result.Url, UriKind.Absolute, out uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
            open.onClick.AddListener(() => Application.OpenURL(result.Url));
            var summary = UiElements.Button("Summary", image.transform, new Vector2(99f, -36f), new Vector2(120f, 28f));
            summary.onClick.AddListener(() => { var handler = SummaryRequested; if (handler != null) handler(result.Copy()); });
            var save = UiElements.Button("Save node", image.transform, new Vector2(230f, -36f), new Vector2(120f, 28f));
            save.onClick.AddListener(() => { var handler = SaveResultRequested; if (handler != null) handler(result.Copy()); });
        }
    }
}
