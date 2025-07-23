using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using SuperNova.Controls;

namespace SuperNova.IDE;

public class ManagedWindow : MDIWindow
{
    protected override Type StyleKeyOverride => typeof(MDIWindow);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        var buttons = e.NameScope.Get<MDICaptionButtons>("PART_CaptionButtons");
    }
}