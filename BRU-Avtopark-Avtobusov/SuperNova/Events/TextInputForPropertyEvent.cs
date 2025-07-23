using SuperNova.IDE;

namespace SuperNova.Events;

public class TextInputForPropertyEvent(string? text) : IEvent
{
    public string? Text { get; } = text;
}