using PYS.Client.ViewModels;

namespace PYS.Client.Views;

public partial class ProjectEditPage : ContentPage
{
    public ProjectEditPage(ProjectEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
