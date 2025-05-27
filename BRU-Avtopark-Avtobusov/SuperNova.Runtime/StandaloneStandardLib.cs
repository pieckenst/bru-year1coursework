using System.Threading.Tasks;
using SuperNova.Runtime.Interpreter;
using Classic.CommonControls.Dialogs;

namespace SuperNova.Runtime;

public class StandaloneStandardLib : IBasicStandardLibrary
{
    private readonly VBFormRuntime form;

    public StandaloneStandardLib(VBFormRuntime form)
    {
        this.form = form;
    }

    public async Task<MessageBoxResult> MsgBox(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
    {
        return await MessageBox.ShowDialog(form, text, caption, buttons, icon);
    }

    public async Task<string?> InputBox(string prompt, string title, string defaultText)
    {
        return null;
        //return await Classic.CommonControls.Dialogs.MessageBox.ShowDialog(form, prompt, title, MessageBoxButtons.Ok,MessageBoxIcon.Warning);
    }
}