using System;
using System.Collections.Generic;
using Avalonia.Controls.Primitives;
using SuperNova.Runtime.Components;
using SuperNova.Runtime.Interpreter;

namespace SuperNova.Runtime.BuiltinControls;

public class VBScrollBar : ScrollBar
{
    protected override Type StyleKeyOverride => typeof(ScrollBar);

    public VBScrollBar()
    {
        ValueChanged += OnValueChanged;
    }

    private void OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        this.ExecuteSub(ScrollBarComponentClass.ChangeEvent);
    }
}