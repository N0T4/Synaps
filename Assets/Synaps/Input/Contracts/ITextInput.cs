using System;

namespace Synaps.Input
{
    public interface ITextInput
    {
        string Text { get; }
        event Action<string> TextChanged;
        event Action<string> Submitted;
        void SetText(string text);
    }
}
