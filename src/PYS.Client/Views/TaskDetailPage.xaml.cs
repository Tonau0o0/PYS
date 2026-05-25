using PYS.Client.ViewModels;

namespace PYS.Client.Views;

public partial class TaskDetailPage : ContentPage
{
    public TaskDetailPage(TaskDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
