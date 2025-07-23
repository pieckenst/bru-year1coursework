using SuperNova.Runtime.ProjectElements;

namespace SuperNova.IDE;

public interface IEditorService
{
    void EditForm(FormDefinition? form);
    void EditCode(FormDefinition? form);
}