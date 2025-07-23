using System;
using SuperNova.Events;
using SuperNova.Forms.ViewModels;
using SuperNova.Runtime.ProjectElements;
using SuperNova.VisualDesigner;

namespace SuperNova.IDE;

public class EditorService : IEditorService
{
    private readonly IEventBus eventBus;
    private readonly IMdiWindowManager mdiWindowManager;
    private readonly Func<CodeEditorViewModel> codeEditorViewModelFactory;
    private readonly Func<FormEditViewModel> formEditViewModelFactory;

    public EditorService(IEventBus eventBus,
        IMdiWindowManager mdiWindowManager,
        Func<CodeEditorViewModel> codeEditorViewModelFactory,
        Func<FormEditViewModel> formEditViewModelFactory)
    {
        this.eventBus = eventBus;
        this.mdiWindowManager = mdiWindowManager;
        this.codeEditorViewModelFactory = codeEditorViewModelFactory;
        this.formEditViewModelFactory = formEditViewModelFactory;
    }

    public void EditForm(FormDefinition? form)
    {
        if (form == null)
            return;
        var e = new ActivateFormEditorEvent(form);
        eventBus.Publish(e);
        if (e.Handled)
            return;
        mdiWindowManager.OpenWindow(formEditViewModelFactory().Initialize(form));
    }

    public void EditCode(FormDefinition? form)
    {
        if (form == null)
            return;
        var e = new ActivateCodeEditorEvent(form);
        eventBus.Publish(e);
        if (e.Handled)
            return;
        mdiWindowManager.OpenWindow(codeEditorViewModelFactory().Initialize(form));
    }
}