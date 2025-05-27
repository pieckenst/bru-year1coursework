using System;
using Avalonia.Controls;

namespace SuperNova.Runtime.BuiltinControls;

public class VBComboBox : ComboBox
{
    protected override Type StyleKeyOverride => typeof(ComboBox);
}