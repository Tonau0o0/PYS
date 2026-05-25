using PYS.Client.ViewModels;

namespace PYS.Client.Views;

public partial class MembersPage : ContentPage
{
    private readonly MembersViewModel _vm;

    public MembersPage(MembersViewModel vm)
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
        catch (Exception ex) { App.LogException(nameof(MembersPage), ex); }
    }
}
