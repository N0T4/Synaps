using System;
using Synaps.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Synaps.Input.Desktop
{
    public sealed class DesktopNodeInteraction : MonoBehaviour, IPointerDownHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private string nodeId;
        private RectTransform nodeRect;
        private RectTransform workspaceRect;
        private IWorkspaceInteraction commands;
        private Action<Exception> onError;
        private Vector2 dragOffset;
        private bool wasDragged;
        public const float UiUnitsPerMetre = 100f;

        public void Configure(string id, RectTransform workspace, IWorkspaceInteraction interaction, Action<Exception> error)
        {
            nodeId = id;
            workspaceRect = workspace;
            nodeRect = (RectTransform)transform;
            commands = interaction;
            onError = error;
        }

        public void OnPointerDown(PointerEventData eventData) { wasDragged = false; }
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || wasDragged) return;
            Invoke(() => commands.SelectNode(nodeId));
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            wasDragged = true;
            Vector2 point;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(workspaceRect, eventData.position,
                eventData.pressEventCamera, out point)) dragOffset = (Vector2)nodeRect.localPosition - point;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            Vector2 point;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(workspaceRect, eventData.position,
                eventData.pressEventCamera, out point)) return;
            point += dragOffset;
            point.x = Mathf.Clamp(point.x, workspaceRect.rect.xMin + nodeRect.rect.width / 2f,
                workspaceRect.rect.xMax - nodeRect.rect.width / 2f);
            point.y = Mathf.Clamp(point.y, workspaceRect.rect.yMin + nodeRect.rect.height / 2f,
                workspaceRect.rect.yMax - nodeRect.rect.height / 2f);
            var position = new SpatialPosition(point.x / UiUnitsPerMetre, point.y / UiUnitsPerMetre,
                nodeRect.localPosition.z / UiUnitsPerMetre);
            Invoke(() => commands.MoveNode(nodeId, position));
        }
        public void OnEndDrag(PointerEventData eventData) { }
        private void Invoke(Action action)
        {
            try { if (commands != null) action(); }
            catch (Exception exception) { if (onError != null) onError(exception); }
        }
    }
}
