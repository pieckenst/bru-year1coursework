using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using SuperNova.Runtime.Components;
using SuperNova.Runtime.Interpreter;

namespace SuperNova.Runtime.BuiltinControls;

public class VBListBox : ListBox
{
    protected override Type StyleKeyOverride => typeof(ListBox);

    static VBListBox()
    {
        AttachedEvents.AttachClick<VBListBox>();
    }
}