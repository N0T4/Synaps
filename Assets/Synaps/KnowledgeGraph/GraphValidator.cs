using System;
using System.Collections.Generic;
using System.IO;
using Synaps.Core;

namespace Synaps.KnowledgeGraph
{
    public static class GraphValidator
    {
        public static void Validate(GraphDocument document)
        {
            if (document == null) throw new InvalidDataException("Graph document is missing.");
            if (document.SchemaVersion != GraphDocument.CurrentSchemaVersion)
                throw new NotSupportedException("Unsupported graph schema: " + document.SchemaVersion + ". Migration is required.");
            if (document.Nodes == null || document.Edges == null)
                throw new InvalidDataException("Graph collections are missing.");
            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            var sourceIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var node in document.Nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.Id) || !nodeIds.Add(node.Id))
                    throw new InvalidDataException("Node IDs must be present and unique.");
                if (string.IsNullOrWhiteSpace(node.Title) || !node.Position.IsFinite ||
                    !Enum.IsDefined(typeof(KnowledgeNodeType), node.NodeType))
                    throw new InvalidDataException("Invalid node: " + node.Id);
                if (!string.IsNullOrEmpty(node.SearchResultId) && !sourceIds.Add(node.SearchResultId))
                    throw new InvalidDataException("Duplicate saved search result: " + node.SearchResultId);
            }
            var edgeIds = new HashSet<string>(StringComparer.Ordinal);
            var connections = new HashSet<string>(StringComparer.Ordinal);
            foreach (var edge in document.Edges)
            {
                if (edge == null || string.IsNullOrWhiteSpace(edge.Id) || !edgeIds.Add(edge.Id) ||
                    string.IsNullOrWhiteSpace(edge.RelationType) || !nodeIds.Contains(edge.FromNodeId ?? "") ||
                    !nodeIds.Contains(edge.ToNodeId ?? "") || edge.FromNodeId == edge.ToNodeId)
                    throw new InvalidDataException("Invalid or dangling edge.");
                // Length prefixes avoid collisions even when imported IDs contain separators.
                var relation = edge.RelationType.Trim().ToLowerInvariant();
                var key = edge.FromNodeId.Length + ":" + edge.FromNodeId + edge.ToNodeId.Length + ":" + edge.ToNodeId + relation;
                if (!connections.Add(key)) throw new InvalidDataException("Duplicate graph connection.");
            }
        }
    }
}
