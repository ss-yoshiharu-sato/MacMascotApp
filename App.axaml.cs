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
        // macOSでの透明ウィンドウ処理のためのグローバル設定
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // メインウィンドウの作成と設定
            var mainWindow = new MainWindow();
            desktop.MainWindow = mainWindow;
            
            // アプリケーション終了時にリソースをクリーンアップ
            desktop.Exit += (s, e) => {
                Console.WriteLine("アプリケーション終了");
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}