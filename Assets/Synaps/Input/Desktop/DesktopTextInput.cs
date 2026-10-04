using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Synaps.Input.Desktop
{
    public sealed class DesktopTextInput : MonoBehaviour, ITextInput
    {
        private InputField field;
        public string Text { get { return field == null ? "" : field.text; } }
        public event Action<string> TextChanged;
        public event Action<string> Submitted;

        public void Bind(InputField inputField)
        {
            Unbind();
            field = inputField;
            field.onValueChanged.AddListener(OnTextChanged);
            field.onEndEdit.AddListener(OnEndEdit);
        }

        public void SetText(string text) { if (field != null) field.text = text ?? ""; }
        public void Submit() { var handler = Submitted; if (handler != null) handler(Text); }
        private void OnTextChanged(string value) { var handler = TextChanged; if (handler != null) handler(value); }
        private void OnEndEdit(string value)
        {
            // Losing focus must not trigger another search. Enter is the desktop submit gesture.
            var keyboard = Keyboard.current;
            if (!field.wasCanceled && keyboard != null &&
                (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)) Submit();
        }
        private void Unbind()
        {
            if (field == null) return;
            field.onValueChanged.RemoveListener(OnTextChanged);
            field.onEndEdit.RemoveListener(OnEndEdit);
            field = null;
        }
        private void OnDestroy() { Unbind(); TextChanged = null; Submitted = null; }
    }
}
