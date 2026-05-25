using PYS.Client.Views;

namespace PYS.Client;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("register", typeof(RegisterPage));
        Routing.RegisterRoute("change-password", typeof(ChangePasswordPage));
        Routing.RegisterRoute("profile", typeof(ProfilePage));
        Routing.RegisterRoute("settings", typeof(SettingsPage));
        Routing.RegisterRoute("video-player", typeof(VideoPlayerPage));
        Routing.RegisterRoute("project-edit", typeof(ProjectEditPage));
        Routing.RegisterRoute("members", typeof(MembersPage));
        Routing.RegisterRoute("tasks", typeof(TasksPage));
        Routing.RegisterRoute("task-edit", typeof(TaskEditPage));
        Routing.RegisterRoute("task-detail", typeof(TaskDetailPage));
    }
}
