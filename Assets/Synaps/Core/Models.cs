using System;
using System.Collections.Generic;

namespace Synaps.Core
{
    public enum SearchResultType { WebPage, Paper, Video, Note }
    public enum KnowledgeNodeType { WebPage, Paper, Video, Note, Concept, Idea }

    // Workspace-local metres. Unity Vector3 belongs in an adapter, never in the domain.
    [Serializable]
    public struct SpatialPosition
    {
        public float X;
        public float Y;
        public float Z;

        public SpatialPosition(float x, float y, float z = 0f) { X = x; Y = y; Z = z; }
        public bool IsFinite
        {
            get { return Finite(X) && Finite(Y) && Finite(Z); }
        }
        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }

    [Serializable]
    public sealed class SearchResult
    {
        public string Id;
        public string Title;
        public string Url;
        public string Snippet;
        public string Source;
        public SearchResultType Type;

        public SearchResult Copy() { return (SearchResult)MemberwiseClone(); }
    }

    [Serializable]
    public sealed class KnowledgeNode
    {
        public string Id;
        public string SearchResultId;
        public string Title;
        public string Content;
        public string Summary;
        public string SourceUrl;
        public KnowledgeNodeType NodeType;
        public SpatialPosition Position;
        public long CreatedAtUtcTicks;
        public long UpdatedAtUtcTicks;

        public KnowledgeNode Copy() { return (KnowledgeNode)MemberwiseClone(); }
    }

    [Serializable]
    public sealed class KnowledgeEdge
    {
        public string Id;
        public string FromNodeId;
        public string ToNodeId;
        public string RelationType;

        public KnowledgeEdge Copy() { return (KnowledgeEdge)MemberwiseClone(); }
    }

    [Serializable]
    public sealed class SuggestedEdge
    {
        public string FromNodeId;
        public string ToNodeId;
        public string RelationType;
        public string Reason;
    }

    // Plain fields are intentional: compatible with Unity JsonUtility, without referencing Unity.
    [Serializable]
    public sealed class GraphDocument
    {
        public const int CurrentSchemaVersion = 1;
        public int SchemaVersion = CurrentSchemaVersion;
        public List<KnowledgeNode> Nodes = new List<KnowledgeNode>();
        public List<KnowledgeEdge> Edges = new List<KnowledgeEdge>();

        public GraphDocument Copy()
        {
            var copy = new GraphDocument { SchemaVersion = SchemaVersion };
            foreach (var node in Nodes) copy.Nodes.Add(node.Copy());
            foreach (var edge in Edges) copy.Edges.Add(edge.Copy());
            return copy;
        }
    }
}
