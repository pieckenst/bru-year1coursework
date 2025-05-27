using Avalonia.Controls.Primitives;

namespace SuperNova.Runtime.BuiltinControls;

public class VBPictureBox : TemplatedControl
{
    static VBPictureBox()
    {
        AttachedEvents.AttachClick<VBPictureBox>();
    }
}