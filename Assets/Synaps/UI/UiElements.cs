using UnityEngine;
using UnityEngine.UI;

namespace Synaps.UI
{
    internal static class UiElements
    {
        internal static readonly Color Ink = new Color(0.86f, 0.9f, 0.95f);
        internal static readonly Color Accent = new Color(0.27f, 0.83f, 0.73f);
        private static Font font;
        private static Font Font { get { return font == null ? (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")) : font; } }

        internal static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        internal static Image Panel(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var image = Rect(name, parent, position, size).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        internal static Text Label(string name, Transform parent, Vector2 position, Vector2 size,
            string value, int fontSize = 18, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var text = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = fontSize;
            text.color = Ink;
            text.alignment = alignment;
            text.text = value;
            text.supportRichText = false;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        internal static Button Button(string title, Transform parent, Vector2 position, Vector2 size)
        {
            var image = Panel(title, parent, position, size, new Color(0.13f, 0.28f, 0.33f));
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Label("Label", image.transform, Vector2.zero, size - new Vector2(8f, 4f), title, 16, TextAnchor.MiddleCenter);
            return button;
        }

        internal static InputField Field(string name, Transform parent, Vector2 position, Vector2 size, string placeholder)
        {
            var image = Panel(name, parent, position, size, new Color(0.1f, 0.15f, 0.22f));
            var field = image.gameObject.AddComponent<InputField>();
            field.targetGraphic = image;
            field.textComponent = Label("Text", image.transform, Vector2.zero, size - new Vector2(24f, 8f), "", 20);
            var hint = Label("Placeholder", image.transform, Vector2.zero, size - new Vector2(24f, 8f), placeholder, 20);
            hint.color = new Color(0.5f, 0.58f, 0.65f);
            field.placeholder = hint;
            field.lineType = InputField.LineType.SingleLine;
            field.characterLimit = 512;
            return field;
        }
    }
}
