using System;
using System.Diagnostics;
using System.Linq;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.Messaging;

using CoyoteStudio.App.Message;
using CoyoteStudio.App.ViewModels;
using CoyoteStudio.App.Views;
using CoyoteStudio.Core;
using CoyoteStudio.Shared;
using CoyoteStudio.Shared.Error;
using CoyoteStudio.Shared.Message;

using Microsoft.Extensions.DependencyInjection;

namespace CoyoteStudio.App;

public partial class App : Application
{
    public IServiceProvider? ServiceProvider { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void AddAppServices(IServiceCollection services)
    {
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<IMessager, Messager>();
    }

    private IAppCore InitApp()
    {
        var services = new ServiceCollection();
        services.AddCoreServices();
        AddAppServices(services);

        ServiceProvider = services.BuildServiceProvider();
        return ServiceProvider.GetRequiredService<IAppCore>();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var appCore = InitApp();

        WeakReferenceMessenger.Default.Register<ErrorMessage>(this, (r, m) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                Debug.WriteLine($"Error ${m.Code}: ${m.Message}");
            });
        });

        appCore.StartServerAsync(6969);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = ServiceProvider?.GetRequiredService<MainWindowViewModel>(),
            };

            desktop.Exit += (s, e) =>
            {
                (ServiceProvider as IDisposable)?.Dispose();
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