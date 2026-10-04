# XR adapter boundary

This folder is intentionally free of Meta, OpenXR, XR Interaction Toolkit and headset packages.
The `Synaps.XR` assembly is reserved for adapters, not another implementation of search or graph state.

When a headset is available:

1. Add OpenXR and your chosen interaction SDK in Unity Package Manager.
2. Add their assembly references **only** to `Synaps.XR` (or a separate SDK-specific child assembly).
3. Implement `ITextInput`: publish text changes and submitted queries from hand/voice/experimental input.
4. Dispatch select and grab motion to `IWorkspaceInteraction.SelectNode` and `MoveNode`.
5. Convert tracked poses to workspace-local metres (`SpatialPosition`). Keep headset coordinates out of saved models.
6. Add an XR composition root with an XR rig and spatial views. Reuse `SearchController`,
   `KnowledgeGraphService`, `WorkspaceController`, `ISearchProvider`, `IAIService`, and `IGraphRepository`.
7. Do not instantiate `SynapsBootstrap` alongside the XR rig: it owns the desktop camera and event system.

The current graph view projects positions onto a flat world-space canvas. A 3D node view can use all
three coordinates without changing the models or graph commands. The domain does not read mouse,
keyboard, hand joints, Unity transforms, or SDK singletons.

Production search/AI adapters should call a backend that holds credentials. Do not ship API keys in a player build.
