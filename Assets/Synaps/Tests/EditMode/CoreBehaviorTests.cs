using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using NUnit.Framework;
using Synaps.Core;
using Synaps.Data;
using Synaps.KnowledgeGraph;
using Synaps.Search;

namespace Synaps.Tests
{
    public sealed class CoreBehaviorTests
    {
        [Test]
        public void SavingSameResultDoesNotDuplicateOrMoveNode()
        {
            var graph = NewGraph();
            var first = graph.SaveSearchResult(Result("a"), new SpatialPosition(1f, 2f));
            var second = graph.SaveSearchResult(Result("a"), new SpatialPosition(9f, 9f));
            Assert.AreEqual(first.Id, second.Id);
            Assert.AreEqual(1, graph.Snapshot().Nodes.Count);
            Assert.AreEqual(1f, second.Position.X);
        }

        [Test]
        public void SnapshotsAndReturnedNodesCannotMutateOwnedState()
        {
            var graph = NewGraph();
            var node = graph.SaveSearchResult(Result("a"), new SpatialPosition());
            node.Title = "mutated";
            var snapshot = graph.Snapshot();
            snapshot.Nodes[0].Title = "mutated again";
            snapshot.Nodes.Clear();
            Assert.AreEqual("a", graph.FindNode(node.Id).Title);
            Assert.AreEqual(1, graph.Snapshot().Nodes.Count);
        }

        [Test]
        public void ConnectionsAreDirectedTypedAndIdempotent()
        {
            var graph = NewGraph();
            var a = graph.SaveSearchResult(Result("a"), new SpatialPosition()).Id;
            var b = graph.SaveSearchResult(Result("b"), new SpatialPosition()).Id;
            var first = graph.Connect(a, b, "USES");
            var again = graph.Connect(a, b, " uses ");
            graph.Connect(b, a, "uses");
            graph.Connect(a, b, "related_to");
            Assert.AreEqual(first.Id, again.Id);
            Assert.AreEqual(3, graph.Snapshot().Edges.Count);
            Assert.AreEqual(1, graph.GetNeighbours(a).Count);
            Assert.Throws<ArgumentException>(() => graph.Connect(a, a, "uses"));
            Assert.Throws<KeyNotFoundException>(() => graph.Connect(a, "missing", "uses"));
        }

        [Test]
        public void DeletingNodeRemovesIncomingAndOutgoingEdges()
        {
            var graph = NewGraph();
            var a = graph.SaveSearchResult(Result("a"), new SpatialPosition()).Id;
            var b = graph.SaveSearchResult(Result("b"), new SpatialPosition()).Id;
            graph.Connect(a, b, "uses");
            graph.Connect(b, a, "supports");
            graph.RemoveNode(a);
            Assert.AreEqual(0, graph.Snapshot().Edges.Count);
            Assert.AreEqual(1, graph.Snapshot().Nodes.Count);
        }

        [Test]
        public void NonFinitePositionsAreRejectedWithoutMutation()
        {
            var graph = NewGraph();
            var node = graph.SaveSearchResult(Result("a"), new SpatialPosition());
            Assert.Throws<ArgumentException>(() => graph.MoveNode(node.Id, new SpatialPosition(float.NaN, 0f)));
            Assert.Throws<ArgumentException>(() => graph.MoveNode(node.Id, new SpatialPosition(0f, float.PositiveInfinity)));
            Assert.AreEqual(0f, graph.FindNode(node.Id).Position.X);
        }

        [Test]
        public void FailedSaveKeepsGraphDirtyForRetry()
        {
            var repository = new MemoryRepository { FailSave = true };
            var graph = new KnowledgeGraphService(repository);
            graph.SaveSearchResult(Result("a"), new SpatialPosition());
            Assert.Throws<IOException>(() => graph.Save());
            Assert.IsTrue(graph.IsDirty);
            repository.FailSave = false;
            graph.Save();
            Assert.IsFalse(graph.IsDirty);
        }

        [Test]
        public void FailedLoadPreservesExistingInMemoryGraph()
        {
            var repository = new MemoryRepository();
            var graph = new KnowledgeGraphService(repository);
            var node = graph.SaveSearchResult(Result("a"), new SpatialPosition());
            repository.Document.SchemaVersion = 999;
            Assert.Throws<NotSupportedException>(() => graph.Load());
            Assert.AreEqual("a", graph.FindNode(node.Id).Title);
            Assert.IsTrue(graph.IsDirty);
        }

        [Test]
        public void WorkspaceConnectModeUsesSameCommandsAsDesktopOrXR()
        {
            var graph = NewGraph();
            var a = graph.SaveSearchResult(Result("a"), new SpatialPosition()).Id;
            var b = graph.SaveSearchResult(Result("b"), new SpatialPosition()).Id;
            var workspace = new WorkspaceController(graph);
            IWorkspaceInteraction input = workspace;
            input.SelectNode(a);
            workspace.BeginConnection("derived_from");
            input.SelectNode(b);
            input.MoveNode(b, new SpatialPosition(1f, 2f, 3f));
            Assert.IsFalse(workspace.IsConnecting);
            Assert.AreEqual(b, workspace.SelectedNodeId);
            Assert.AreEqual(a, graph.Snapshot().Edges[0].FromNodeId);
            Assert.AreEqual(3f, graph.FindNode(b).Position.Z);
            workspace.DeleteSelected();
            Assert.IsNull(workspace.SelectedNodeId);
            Assert.AreEqual(0, graph.Snapshot().Edges.Count);
        }

        [Test]
        public void InvalidGraphDocumentsAreRejected()
        {
            var graph = NewGraph();
            var a = graph.SaveSearchResult(Result("a"), new SpatialPosition());
            var document = graph.Snapshot();
            document.Nodes.Add(a.Copy());
            Assert.Throws<InvalidDataException>(() => GraphValidator.Validate(document));
            document = graph.Snapshot();
            document.Edges.Add(new KnowledgeEdge { Id = "e", FromNodeId = a.Id, ToNodeId = "missing", RelationType = "uses" });
            Assert.Throws<InvalidDataException>(() => GraphValidator.Validate(document));
            document = graph.Snapshot();
            document.Nodes[0].NodeType = (KnowledgeNodeType)999;
            Assert.Throws<InvalidDataException>(() => GraphValidator.Validate(document));
        }

        [Test]
        public void RepositoryReloadsNodesEdgesSummaryAndPositions()
        {
            InTemporaryDirectory(directory =>
            {
                var path = Path.Combine(directory, "graph.json");
                var graph = new KnowledgeGraphService(new FileGraphRepository(path, new TestXmlSerializer()));
                var a = graph.SaveSearchResult(Result("Текст \"A\""), new SpatialPosition(1.2f, -2f, 3f));
                var b = graph.SaveSearchResult(Result("b"), new SpatialPosition());
                graph.UpdateSummary(a.Id, "Стислий опис\nsecond line");
                graph.Connect(a.Id, b.Id, "supports");
                graph.Save();
                var reopened = new KnowledgeGraphService(new FileGraphRepository(path, new TestXmlSerializer()));
                reopened.Load();
                Assert.AreEqual(2, reopened.Snapshot().Nodes.Count);
                Assert.AreEqual("supports", reopened.Snapshot().Edges[0].RelationType);
                Assert.AreEqual(3f, reopened.FindNode(a.Id).Position.Z);
                Assert.AreEqual("Стислий опис\nsecond line", reopened.FindNode(a.Id).Summary);
            });
        }

        [Test]
        public void CorruptPrimaryRecoversAndPreservesGoodBackup()
        {
            InTemporaryDirectory(directory =>
            {
                var path = Path.Combine(directory, "graph.json");
                var repo = new FileGraphRepository(path, new TestXmlSerializer());
                var graph = new KnowledgeGraphService(repo);
                graph.SaveSearchResult(Result("a"), new SpatialPosition());
                graph.Save();
                graph.SaveSearchResult(Result("b"), new SpatialPosition());
                graph.Save();
                var backup = File.ReadAllText(path + ".bak");
                File.WriteAllText(path, "broken");
                var loaded = graph.Load();
                Assert.IsTrue(loaded.RecoveredFromBackup);
                Assert.AreEqual(1, graph.Snapshot().Nodes.Count);
                Assert.IsTrue(graph.IsDirty);
                graph.Save();
                Assert.AreEqual(backup, File.ReadAllText(path + ".bak"));
                Assert.AreEqual(1, Directory.GetFiles(directory, "*.corrupt-*").Length);
                Assert.AreEqual(1, new FileGraphRepository(path, new TestXmlSerializer()).Load().Document.Nodes.Count);
                Assert.AreEqual(0, Directory.GetFiles(directory, "*.tmp").Length);
            });
        }

        [Test]
        public void CorruptGraphWithoutUsableBackupIsNotSilentlyReset()
        {
            InTemporaryDirectory(directory =>
            {
                var path = Path.Combine(directory, "graph.json");
                File.WriteAllText(path, "broken primary");
                File.WriteAllText(path + ".bak", "broken backup");
                var repo = new FileGraphRepository(path, new TestXmlSerializer());
                Assert.Throws<InvalidDataException>(() => repo.Load());
                Assert.AreEqual("broken primary", File.ReadAllText(path));
            });
        }

        [Test]
        public void NewerSchemaIsNeverDowngradedToOlderBackup()
        {
            InTemporaryDirectory(directory =>
            {
                var path = Path.Combine(directory, "graph.json");
                var serializer = new TestXmlSerializer();
                File.WriteAllText(path, serializer.Serialize(new GraphDocument { SchemaVersion = 999 }));
                File.WriteAllText(path + ".bak", serializer.Serialize(new GraphDocument()));
                var repo = new FileGraphRepository(path, serializer);
                Assert.Throws<NotSupportedException>(() => repo.Load());
                Assert.IsTrue(File.ReadAllText(path).Contains("999"));
            });
        }

        [Test]
        public void InvalidSerializedOutputCannotReplaceValidSave()
        {
            InTemporaryDirectory(directory =>
            {
                var path = Path.Combine(directory, "graph.json");
                var serializer = new TestXmlSerializer();
                var repo = new FileGraphRepository(path, serializer);
                repo.Save(new GraphDocument());
                var original = File.ReadAllText(path);
                serializer.BreakOutput = true;
                Assert.Throws<InvalidDataException>(() => repo.Save(new GraphDocument()));
                Assert.AreEqual(original, File.ReadAllText(path));
                Assert.AreEqual(0, Directory.GetFiles(directory, "*.tmp").Length);
            });
        }

        [Test]
        public async Task LatestSearchWinsEvenIfProviderIgnoresCancellation()
        {
            var provider = new ControlledProvider();
            using (var controller = new SearchController(provider))
            {
                var oldSearch = controller.SearchAsync("old");
                var newSearch = controller.SearchAsync("new");
                provider.Complete("new");
                await newSearch;
                provider.Complete("old");
                await oldSearch;
                Assert.AreEqual("new", controller.State.Query);
                Assert.AreEqual("new", controller.State.Results[0].Id);
                Assert.AreEqual(SearchStatus.Succeeded, controller.State.Status);
            }
        }

        [Test]
        public async Task StaleProviderFailureDoesNotReplaceNewResults()
        {
            var provider = new ControlledProvider();
            using (var controller = new SearchController(provider))
            {
                var oldSearch = controller.SearchAsync("old");
                var newSearch = controller.SearchAsync("new");
                provider.Complete("new");
                await newSearch;
                provider.Fail("old");
                await oldSearch;
                Assert.AreEqual(SearchStatus.Succeeded, controller.State.Status);
                Assert.AreEqual("new", controller.State.Query);
            }
        }

        [Test]
        public async Task EmptyQueryClearsResultsAndSupersedesPendingSearch()
        {
            var provider = new ControlledProvider();
            using (var controller = new SearchController(provider))
            {
                var oldSearch = controller.SearchAsync("old");
                await controller.SearchAsync("   ");
                provider.Complete("old");
                await oldSearch;
                Assert.AreEqual(SearchStatus.Idle, controller.State.Status);
                Assert.AreEqual(0, controller.State.Results.Count);
            }
        }

        [Test]
        public async Task CancelledRequestCannotPublishResults()
        {
            var provider = new ControlledProvider();
            using (var controller = new SearchController(provider))
            using (var cancellation = new CancellationTokenSource())
            {
                var task = controller.SearchAsync("a", cancellation.Token);
                cancellation.Cancel();
                provider.Complete("a");
                await task;
                Assert.AreEqual(SearchStatus.Cancelled, controller.State.Status);
            }
        }

        [Test]
        public async Task ProviderFailureIsVisibleAndNextSearchCanRecover()
        {
            var provider = new ControlledProvider();
            using (var controller = new SearchController(provider))
            {
                var task = controller.SearchAsync("a");
                provider.Fail("a");
                await task;
                Assert.AreEqual(SearchStatus.Failed, controller.State.Status);
                Assert.AreEqual("Provider failed", controller.State.Error);
                task = controller.SearchAsync("b");
                provider.Complete("b");
                await task;
                Assert.AreEqual(SearchStatus.Succeeded, controller.State.Status);
            }
        }

        [Test]
        public async Task DisposedControllerDoesNotPublishPendingResponses()
        {
            var provider = new ControlledProvider();
            var controller = new SearchController(provider);
            var events = 0;
            controller.StateChanged += state => events++;
            var task = controller.SearchAsync("a");
            controller.Dispose();
            provider.Complete("a");
            await task;
            Assert.AreEqual(1, events); // Searching only.
        }

        [Test]
        public async Task SearchStateCopiesProtectControllerFromViewMutation()
        {
            using (var controller = new SearchController(new MockSearchProvider(0)))
            {
                await controller.SearchAsync("text input");
                controller.State.Results[0].Title = "changed by view";
                Assert.AreEqual("FingerType", controller.State.Results[0].Title);
                Assert.AreEqual(5, controller.State.Results.Count);
            }
        }

        private static KnowledgeGraphService NewGraph() { return new KnowledgeGraphService(new MemoryRepository()); }
        private static SearchResult Result(string id)
        {
            return new SearchResult { Id = id, Title = id, Url = "", Snippet = "sample", Source = "fixture", Type = SearchResultType.Paper };
        }
        private static void InTemporaryDirectory(Action<string> action)
        {
            var directory = Path.Combine(Path.GetTempPath(), "SynapsTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try { action(directory); }
            finally
            {
                var cleanupTarget = Path.GetFullPath(directory);
                var allowedPrefix = Path.Combine(Path.GetFullPath(Path.GetTempPath()), "SynapsTests-");
                if (!cleanupTarget.StartsWith(allowedPrefix, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Unexpected test cleanup path.");
                Directory.Delete(cleanupTarget, true);
            }
        }

        private sealed class MemoryRepository : IGraphRepository
        {
            public GraphDocument Document = new GraphDocument();
            public bool FailSave;
            public GraphLoadResult Load() { return new GraphLoadResult(Document.Copy()); }
            public void Save(GraphDocument document)
            {
                if (FailSave) throw new IOException("Save failed");
                Document = document.Copy();
            }
        }
        private sealed class ControlledProvider : ISearchProvider
        {
            private readonly Dictionary<string, TaskCompletionSource<IReadOnlyList<SearchResult>>> requests =
                new Dictionary<string, TaskCompletionSource<IReadOnlyList<SearchResult>>>();
            public Task<IReadOnlyList<SearchResult>> SearchAsync(string query, CancellationToken token)
            {
                var request = new TaskCompletionSource<IReadOnlyList<SearchResult>>();
                requests.Add(query, request);
                return request.Task; // Deliberately non-cooperative provider.
            }
            public void Complete(string query) { requests[query].SetResult(new List<SearchResult> { Result(query) }.AsReadOnly()); }
            public void Fail(string query) { requests[query].SetException(new IOException("Provider failed")); }
        }
        private sealed class TestXmlSerializer : IGraphSerializer
        {
            // Exercises the real file repository independently of Unity. JSON tests live in the Unity suite.
            public bool BreakOutput;
            public string Serialize(GraphDocument document)
            {
                if (BreakOutput) return "broken";
                using (var writer = new StringWriter())
                {
                    new XmlSerializer(typeof(GraphDocument)).Serialize(writer, document);
                    return writer.ToString();
                }
            }
            public GraphDocument Deserialize(string text)
            {
                try { using (var reader = new StringReader(text)) return (GraphDocument)new XmlSerializer(typeof(GraphDocument)).Deserialize(reader); }
                catch (InvalidOperationException exception) { throw new InvalidDataException("Invalid test document.", exception); }
            }
        }
    }
}
