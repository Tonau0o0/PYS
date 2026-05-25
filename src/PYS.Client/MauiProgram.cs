using Microsoft.Extensions.Logging;
using PYS.Client.Services;
using PYS.Client.ViewModels;
using PYS.Client.Views;

namespace PYS.Client;

public static class MauiProgram
{
    public const string ApiBaseUrl = "http://localhost:5014";

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<AuthState>();
        builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri(ApiBaseUrl) });
        builder.Services.AddSingleton<ApiClient>();
        builder.Services.AddSingleton<PysApi>();

        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<ChangePasswordViewModel>();
        builder.Services.AddTransient<ProjectsViewModel>();
        builder.Services.AddTransient<ProjectEditViewModel>();
        builder.Services.AddTransient<MembersViewModel>();
        builder.Services.AddTransient<TasksViewModel>();
        builder.Services.AddTransient<TaskEditViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<ResourcesViewModel>();
        builder.Services.AddTransient<VideoPlayerViewModel>();

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<ChangePasswordPage>();
        builder.Services.AddTransient<ProjectsPage>();
        builder.Services.AddTransient<ProjectEditPage>();
        builder.Services.AddTransient<MembersPage>();
        builder.Services.AddTransient<TasksPage>();
        builder.Services.AddTransient<TaskEditPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<VideoPlayerPage>();

        return builder.Build();
    }
}
