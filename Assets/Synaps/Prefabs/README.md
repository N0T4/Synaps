# Prototype views

The first desktop scene builds its world-space cards, nodes and edges from `UiElements` at runtime,
so it can start without manual Inspector wiring or imported assets. There are no authored prefabs yet.
Replace these factories with prefabs here when iterating on visual design. Keep service references
in the composition root, and route interactions through the existing interfaces.
