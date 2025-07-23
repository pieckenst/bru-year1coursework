using System;
using Avalonia.Media;

namespace SuperNova.IDE;

public interface IMdiWindow
{
    string Title { get; }
    IImage Icon { get; }
    event Action<IMdiWindow>? CloseRequest;
}

/// <summary>
/// Safe MDI window interface without VB interpreter dependencies
/// </summary>
public interface ISafeMdiWindow : IMdiWindow
{
    bool CanClose { get; set; }
    object? Content { get; set; }
    void SafeClose();
}
