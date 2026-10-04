using System;
using System.IO;
using NUnit.Framework;
using Synaps.Core;
using Synaps.Data;
using Synaps.Data.Unity;
using Synaps.KnowledgeGraph;

namespace Synaps.Tests
{
    public sealed class UnityJsonGraphSerializerTests
    {
        [Test]
        public void JsonRoundTripPreservesUnicodeLongTimestampsEnumsAndPositions()
        {
            var serializer = new UnityJsonGraphSerializer();
            var document = new GraphDocument();
            document.Nodes.Add(new KnowledgeNode { Id = "a", Title = "Ідея \"Synaps\"", Content = "Line 1\nLine 2",
                Summary = "Український опис", SourceUrl = "https://example.com", SearchResultId = "result-a",
                NodeType = KnowledgeNodeType.Idea, Position = new SpatialPosition(1.5f, -2f, 3f),
                CreatedAtUtcTicks = DateTime.UtcNow.Ticks, UpdatedAtUtcTicks = DateTime.UtcNow.Ticks });
            document.Nodes.Add(new KnowledgeNode { Id = "b", Title = "Concept", NodeType = KnowledgeNodeType.Concept });
            document.Edges.Add(new KnowledgeEdge { Id = "e", FromNodeId = "a", ToNodeId = "b", RelationType = "uses" });
            var copy = serializer.Deserialize(serializer.Serialize(document));
            GraphValidator.Validate(copy);
            Assert.AreEqual(document.Nodes[0].Title, copy.Nodes[0].Title);
            Assert.AreEqual(document.Nodes[0].CreatedAtUtcTicks, copy.Nodes[0].CreatedAtUtcTicks);
            Assert.AreEqual(KnowledgeNodeType.Idea, copy.Nodes[0].NodeType);
            Assert.AreEqual(3f, copy.Nodes[0].Position.Z);
            Assert.AreEqual("uses", copy.Edges[0].RelationType);
        }

        [Test]
        public void MissingRequiredJsonFieldsDoNotBecomeAnEmptyValidGraph()
        {
            var serializer = new UnityJsonGraphSerializer();
            Assert.Throws<InvalidDataException>(() => serializer.Deserialize("{}"));
            Assert.Throws<InvalidDataException>(() => serializer.Deserialize("{\"SchemaVersion\":1}"));
            Assert.Throws<InvalidDataException>(() => serializer.Deserialize(""));
            Assert.Throws<InvalidDataException>(() => serializer.Deserialize("not json"));
        }

        [Test]
        public void JsonFileRepositorySurvivesAReopen()
        {
            var directory = Path.Combine(Path.GetTempPath(), "SynapsJsonTest-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var file = Path.Combine(directory, "graph.json");
                var graph = new KnowledgeGraphService(new FileGraphRepository(file, new UnityJsonGraphSerializer()));
                var node = graph.SaveSearchResult(new SearchResult { Id = "a", Title = "FingerType",
                    Type = SearchResultType.Paper, Snippet = "fixture" }, new SpatialPosition(2f, 1f));
                graph.Save();
                var reopened = new KnowledgeGraphService(new FileGraphRepository(file, new UnityJsonGraphSerializer()));
                reopened.Load();
                Assert.AreEqual(node.Id, reopened.Snapshot().Nodes[0].Id);
                Assert.AreEqual(2f, reopened.Snapshot().Nodes[0].Position.X);
            }
            finally
            {
                var cleanupTarget = Path.GetFullPath(directory);
                var allowedPrefix = Path.Combine(Path.GetFullPath(Path.GetTempPath()), "SynapsJsonTest-");
                if (!cleanupTarget.StartsWith(allowedPrefix, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Unexpected test cleanup path.");
                Directory.Delete(cleanupTarget, true);
            }
        }
    }
}
