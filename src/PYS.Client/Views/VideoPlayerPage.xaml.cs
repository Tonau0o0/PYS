using PYS.Client.ViewModels;

namespace PYS.Client.Views;

public partial class VideoPlayerPage : ContentPage
{
    public VideoPlayerPage(VideoPlayerViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
