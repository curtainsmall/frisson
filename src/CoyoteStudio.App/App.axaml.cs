using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using CoyoteStudio.App.Services;
using CoyoteStudio.App.ViewModels;
using CoyoteStudio.App.Views;
using CoyoteStudio.Core;
using CoyoteStudio.Core.Error;

namespace CoyoteStudio.App;

public partial class App : Application
{
    private AppCore? _appCore;
    public static Window? MainWindow { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _appCore = AppCore.Instance;

        _appCore.ErrorMessager.Listen<ErrorMessage>(msg =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                Debug.WriteLine($"Error ${msg.Code}: ${msg.Message}");
            });
        });

        _appCore.Startup(6969);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };
            desktop.MainWindow = MainWindow;

            desktop.Exit += (s, e) =>
            {
                _appCore.Dispose();

                var logDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "CoyoteStudio", "logs");
                var logPath = System.IO.Path.Combine(logDir, $"websocket-{DateTime.Now:yyyyMMdd-HHmmss}.log");
                LoggerService.Instance.SaveToFile(logPath);
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}