using PYS.Client.ViewModels;

namespace PYS.Client.Views;

public partial class TaskEditPage : ContentPage
{
    public TaskEditPage(TaskEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
