using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using SuperNova.Forms.ViewModels;
using SuperNova.Forms.Views;
using SuperNova.IDE;
using SuperNova.Tools;
using SuperNova.Tools.Navigation;
using SuperNova.Tools.Reports;
using SuperNova.VisualDesigner;
using SuperNova.VisualDesigner.Views;
using FormEditViewModel = SuperNova.VisualDesigner.FormEditViewModel;
using MDIControllerView = SuperNova.IDE.MDIControllerView;

namespace SuperNova;

public class ViewLocator : IDataTemplate
{
    private static Dictionary<Type, Func<Control>> templates = new();

    private static void Register<TViewModel, TView>() where TView : Control, new()
    {
        templates[typeof(TViewModel)] = () => new TView();
    }

    static ViewLocator()
    {
        Register<CodeEditorViewModel, CodeEditorView>();
        Register<FormEditViewModel, FormEditView>();
        Register<MenuEditorViewModel, MenuEditorView>();
        Register<ToolBoxToolViewModel, ToolBoxToolView>();
        Register<ProjectToolViewModel, ProjectToolView>();
        Register<FormLayoutToolViewModel, FormLayoutToolView>();
        Register<NavigationToolViewModel, NavigationToolView>();
        Register<ReportsToolViewModel, ReportsToolView>();
        Register<MDIControllerViewModel, MDIControllerView>();
        Register<PropertiesToolViewModel, PropertiesToolView>();
        Register<AddInManagerViewModel, AddInManagerView>();
        Register<AddProcedureViewModel, AddProcedureView>();
        Register<OptionsViewModel, OptionsView>();
        Register<RuntimeErrorViewModel, RuntimeErrorView>();
        Register<LocalsToolViewModel, LocalsToolView>();
        Register<WatchesToolViewModel, WatchesToolView>();
        Register<ImmediateToolViewModel, ImmediateToolView>();
        Register<ColorPaletteToolViewModel, ColorPaletteToolView>();
        Register<NewProjectViewModel, NewProjectView>();
        Register<SaveChangesViewModel, SaveChangesView>();
        Register<ProjectPropertiesViewModel, ProjectPropertiesView>();
        Register<ReferencesViewModel, ReferencesView>();
        Register<ComponentsViewModel, ComponentsView>();
    }

    public Control? Build(object? param)
    {
        if (param != null &&
            templates.TryGetValue(param.GetType(), out var template))
            return template();
        return null;
    }

    public bool Match(object? data)
    {
        return data != null && templates.ContainsKey(data.GetType());
    }
}