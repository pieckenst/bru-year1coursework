using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using SuperNova.Runtime.Components;
using SuperNova.Runtime.Interpreter;

namespace SuperNova.Runtime.BuiltinControls;

public class VBTextBox : TextBox
{
    protected override Type StyleKeyOverride => typeof(TextBox);

    static VBTextBox()
    {
        TextProperty.Changed.AddClassHandler<VBTextBox>((textBox, e) =>
        {
            textBox.ExecuteSub(TextBoxComponentClass.ChangeEvent);
        });
        AttachedEvents.AttachClick<VBTextBox>();
    }
}