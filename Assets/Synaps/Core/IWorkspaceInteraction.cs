namespace Synaps.Core
{
    // Mouse, controller ray and hand tracking all dispatch these same commands.
    public interface IWorkspaceInteraction
    {
        void SelectNode(string nodeId);
        void MoveNode(string nodeId, SpatialPosition position);
    }
}
