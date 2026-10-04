using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Synaps.AI;
using Synaps.Core;
using Synaps.Data;
using Synaps.Data.Unity;
using Synaps.Input;
using Synaps.Input.Desktop;
using Synaps.KnowledgeGraph;
using Synaps.Search;
using Synaps.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Synaps.Bootstrap
{
    // Composition root: the only place choosing concrete providers and platform adapters.
    public sealed class SynapsBootstrap : MonoBehaviour
    {
        private SearchController search;
        private KnowledgeGraphService graph;
        private WorkspaceController workspace;
        private IAIService ai;
        private ITextInput textInput;
        private DesktopWorkspaceView view;
        private GraphPresenter graphPresenter;
        private Camera desktopCamera;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private readonly Dictionary<string, string> summaries = new Dictionary<string, string>();
        private bool ready;
        private bool disposed;
        private float nextSaveTime;

        private void Start()
        {
            desktopCamera = new GameObject("Desktop Camera", typeof(Camera)).GetComponent<Camera>();
            desktopCamera.transform.SetParent(transform, false);
            desktopCamera.transform.localPosition = new Vector3(0f, 0f, -20f);
            desktopCamera.orthographic = true;
            desktopCamera.nearClipPlane = 0.1f;
            desktopCamera.farClipPlane = 100f;
            desktopCamera.clearFlags = CameraClearFlags.SolidColor;
            desktopCamera.backgroundColor = new Color(0.025f, 0.035f, 0.06f);
            var events = new GameObject("Desktop Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
            var module = events.GetComponent<InputSystemUIInputModule>();
            if (module.actionsAsset == null) module.AssignDefaultActions();
            view = gameObject.AddComponent<DesktopWorkspaceView>();
            view.Build(desktopCamera);
            try
            {
                // Swap these factories for backend/XR adapters later. No service locator or static domain state.
                search = new SearchController(new MockSearchProvider());
                ai = new MockAIService();
                var file = Path.Combine(Application.persistentDataPath, "Synaps", "graph.json");
                graph = new KnowledgeGraphService(new FileGraphRepository(file, new UnityJsonGraphSerializer()));
                var loaded = graph.Load();
                workspace = new WorkspaceController(graph);
                var desktopText = gameObject.AddComponent<DesktopTextInput>();
                desktopText.Bind(view.QueryField);
                textInput = desktopText;
                graphPresenter = new GraphPresenter(graph, workspace, view.GraphRoot, ConfigureNodeInteraction);
                search.StateChanged += OnSearchChanged;
                textInput.Submitted += OnQuerySubmitted;
                view.SearchButton.onClick.AddListener(desktopText.Submit);
                view.SaveResultRequested += OnSaveResult;
                view.SummaryRequested += OnSummaryRequested;
                view.SaveGraphButton.onClick.AddListener(() => PersistGraph(true));
                view.ConnectButton.onClick.AddListener(() => RunCommand(() => workspace.BeginConnection(view.RelationField.text)));
                view.CancelButton.onClick.AddListener(() => workspace.CancelConnection());
                view.DeleteButton.onClick.AddListener(() => RunCommand(workspace.DeleteSelected));
                graph.Changed += ScheduleSave;
                workspace.SelectionChanged += ShowSelection;
                ready = true;
                view.SetReady(true);
                textInput.SetText("best VR text input methods");
                nextSaveTime = Time.unscaledTime + 0.6f;
                view.SetStatus(loaded.RecoveredFromBackup ? "Recovered graph from backup. A repaired save is pending." :
                    "Ready. Search, save cards, and connect nodes. Changes save automatically.");
                Debug.Log("Synaps graph file: " + file);
            }
            catch (Exception exception)
            {
                // Never continue with an empty writable graph after a failed load.
                view.SetReady(false);
                ReportError(exception);
            }
        }

        private void Update()
        {
            if (desktopCamera != null)
                desktopCamera.orthographicSize = Mathf.Max(5.15f, 7.9f / Mathf.Max(0.1f, desktopCamera.aspect));
            if (ready && graph.IsDirty && Time.unscaledTime >= nextSaveTime) PersistGraph(false);
        }

        private void ConfigureNodeInteraction(GraphNodeView node)
        {
            node.gameObject.AddComponent<DesktopNodeInteraction>().Configure(node.NodeId, view.GraphRoot, workspace, ReportError);
        }

        private async void OnQuerySubmitted(string query)
        {
            if (!ready || disposed) return;
            try { await search.SearchAsync(query, lifetime.Token); }
            catch (OperationCanceledException) { }
            catch (Exception exception) { if (!disposed) ReportError(exception); }
        }

        private void OnSearchChanged(SearchState state)
        {
            if (disposed) return;
            summaries.Clear();
            view.RenderSearch(state);
        }

        private void OnSaveResult(SearchResult result)
        {
            RunCommand(() =>
            {
                var index = graph.Snapshot().Nodes.Count % 8;
                var position = new SpatialPosition(index % 2 == 0 ? -1.65f : 1.65f, 2f - index / 2 * 1.35f);
                var node = graph.SaveSearchResult(result, position);
                string summary;
                if (summaries.TryGetValue(result.Id, out summary)) graph.UpdateSummary(node.Id, summary);
                workspace.CancelConnection();
                workspace.SelectNode(node.Id);
                view.SetStatus("Saved \"" + node.Title + "\" to graph.");
            });
        }

        private async void OnSummaryRequested(SearchResult result)
        {
            if (!ready || disposed) return;
            var revision = view.ResultRevision;
            try
            {
                var summary = await ai.SummarizeAsync(result.Snippet, lifetime.Token);
                if (disposed || lifetime.IsCancellationRequested || revision != view.ResultRevision) return;
                summaries[result.Id] = summary;
                view.SetResultSummary(result.Id, summary, revision);
                var existing = graph.Snapshot().Nodes.Find(node => node.SearchResultId == result.Id);
                if (existing != null) { graph.UpdateSummary(existing.Id, summary); ShowSelection(); }
            }
            catch (OperationCanceledException) { }
            catch (Exception exception) { if (!disposed) ReportError(exception); }
        }

        private void ShowSelection()
        {
            var node = graph.FindNode(workspace.SelectedNodeId);
            if (node == null) { view.SetSelection("Select a node to connect it or delete it."); return; }
            if (workspace.IsConnecting) { view.SetSelection("Source: " + node.Title + ". Click a different target node, or Cancel."); return; }
            var detail = string.IsNullOrWhiteSpace(node.Summary) ? "Drag to move. Connect to choose a target." : node.Summary;
            if (detail.Length > 130) detail = detail.Substring(0, 130) + "...";
            view.SetSelection(node.Title + " / " + detail);
        }

        private void ScheduleSave() { nextSaveTime = Time.unscaledTime + 0.6f; }
        private void PersistGraph(bool force)
        {
            if (!ready || graph == null || (!force && !graph.IsDirty)) return;
            try
            {
                graph.Save();
                if (!disposed && view != null) view.SetStatus("Graph saved locally.");
            }
            catch (Exception exception)
            {
                nextSaveTime = Time.unscaledTime + 5f;
                ReportError(exception);
            }
        }
        private void RunCommand(Action command)
        {
            if (!ready || disposed) return;
            try { command(); }
            catch (Exception exception) { ReportError(exception); }
        }
        private void ReportError(Exception exception)
        {
            Debug.LogException(exception);
            if (!disposed && view != null) view.SetStatus(exception.Message, true);
        }
        private void OnApplicationPause(bool paused) { if (paused) PersistGraph(false); }
        private void OnApplicationQuit() { PersistGraph(false); }
        private void OnDestroy()
        {
            disposed = true;
            PersistGraph(false);
            ready = false;
            lifetime.Cancel();
            if (textInput != null) textInput.Submitted -= OnQuerySubmitted;
            if (view != null) { view.SaveResultRequested -= OnSaveResult; view.SummaryRequested -= OnSummaryRequested; }
            if (graph != null) graph.Changed -= ScheduleSave;
            if (workspace != null) workspace.SelectionChanged -= ShowSelection;
            if (graphPresenter != null) graphPresenter.Dispose();
            if (search != null) search.Dispose();
            lifetime.Dispose();
        }
    }
}
