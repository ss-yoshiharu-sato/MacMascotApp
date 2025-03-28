using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;

namespace MacMascotApp;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        // macOS IMKに関するログ警告を抑制（必要に応じてコメント解除）
        // AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
        // {
        //     if (args.Exception.Message.Contains("IMKCFRunLoopWakeUpReliable"))
        //     {
        //         // 警告を無視
        //         return;
        //     }
        // };
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}