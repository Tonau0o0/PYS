using PYS.Client.ViewModels;

namespace PYS.Client.Views;

public partial class TasksPage : ContentPage
{
    private readonly TasksViewModel _vm;

    public TasksPage(TasksViewModel vm)
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
        catch (Exception ex) { App.LogException(nameof(TasksPage), ex); }
    }
}
