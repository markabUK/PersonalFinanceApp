using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PersonalFinanceApp.Core.Services;
using PersonalFinanceApp.Desktop.ViewModels;
using PersonalFinanceApp.Desktop.Views;

namespace PersonalFinanceApp.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var state = StorageService.LoadState();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(state)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}