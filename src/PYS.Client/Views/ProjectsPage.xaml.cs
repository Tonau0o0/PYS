using PYS.Client.ViewModels;

namespace PYS.Client.Views;

public partial class ProjectsPage : ContentPage
{
    private readonly ProjectsViewModel _vm;

    public ProjectsPage(ProjectsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = SafeLoadAsync();
    }

    private async Task SafeLoadAsync()
    {
        try { await _vm.LoadAsync(); }
        catch (Exception ex) { App.LogException(nameof(ProjectsPage), ex); }
    }
}
