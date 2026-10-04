using System;
using System.IO;
using Synaps.Core;
using UnityEngine;

namespace Synaps.Data.Unity
{
    public sealed class UnityJsonGraphSerializer : IGraphSerializer
    {
        public string Serialize(GraphDocument document) { return JsonUtility.ToJson(document, true); }

        public GraphDocument Deserialize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new InvalidDataException("Graph file is empty.");
            try
            {
                // Deserialize into an envelope with no defaults, so '{}' cannot silently erase a graph.
                var envelope = JsonUtility.FromJson<Envelope>(text);
                if (envelope == null || envelope.Nodes == null || envelope.Edges == null)
                    throw new InvalidDataException("Graph file is missing its required fields.");
                return new GraphDocument { SchemaVersion = envelope.SchemaVersion, Nodes = envelope.Nodes, Edges = envelope.Edges };
            }
            catch (ArgumentException exception) { throw new InvalidDataException("Graph JSON cannot be parsed.", exception); }
        }

        [Serializable]
        private sealed class Envelope
        {
            public int SchemaVersion;
            public System.Collections.Generic.List<KnowledgeNode> Nodes;
            public System.Collections.Generic.List<KnowledgeEdge> Edges;
        }
    }
}
