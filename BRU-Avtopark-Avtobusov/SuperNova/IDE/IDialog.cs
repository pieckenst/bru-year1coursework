using System;

namespace SuperNova.IDE;

public interface IDialog
{
    string Title { get; }
    bool CanResize { get; }
    event Action<bool> CloseRequested;
}