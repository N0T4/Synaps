using Synaps.Core;

namespace Synaps.Data
{
    public interface IGraphSerializer
    {
        string Serialize(GraphDocument document);
        GraphDocument Deserialize(string text);
    }
}
