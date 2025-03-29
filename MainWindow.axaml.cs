using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using System.Net.NetworkInformation;
using ReactiveUI;
// テキスト関連の名前空間を追加
using Avalonia.Media;

namespace MacMascotApp
{
    public partial class MainWindow : Window
    {
        private bool isDragging = false;
        private Point dragOffset = new Point();

        // 起動時のサイズを記録するフィールドを追加
        private double initialWidth;
        private double initialHeight;
        
        // キャラクター画像の初期サイズと位置を記録するフィールドを追加
        private double initialCharacterWidth;
        private double initialCharacterHeight;
        private Thickness initialCharacterMargin;

        // 修正: 起動時にサイズを記録
        private void InitializeCharacterImageAndWindowSize()
        {
            var baseDirectory = Directory.GetCurrentDirectory();
            var characterImagePath = Path.Combine(baseDirectory, "Resources", "Character.png");

            if (File.Exists(characterImagePath))
            {
                var bitmap = new Bitmap(characterImagePath);
                CharacterImage.Source = bitmap;

                double aspectRatio = bitmap.Size.Height / bitmap.Size.Width;
                double calculatedHeight = 300 * aspectRatio;

                // 起動時のサイズを記録
                initialWidth = 300;
                initialHeight = calculatedHeight + 130;

                this.Width = initialWidth;
                this.Height = initialHeight;
                
                // キャラクター画像の初期サイズと位置を記録
                initialCharacterWidth = CharacterImage.Width;
                initialCharacterHeight = CharacterImage.Height;
                initialCharacterMargin = CharacterImage.Margin;
                
                // 明示的にキャラクター画像のサイズを設定
                CharacterImage.Width = double.NaN; // Auto
                CharacterImage.Height = double.NaN; // Auto
            }
            else
            {
                Console.WriteLine($"Character image not found at: {characterImagePath}");
            }
        }

        // 修正: ウィンドウサイズをリセットするメソッドを強化
        private void RestoreWindowSize()
        {
            // 許容誤差を大きくして微小な差異は無視する（1.0から3.0に変更）
            if (Math.Abs(this.Width - initialWidth) > 3.0 || Math.Abs(this.Height - initialHeight) > 3.0)
            {
                Console.WriteLine($"ウィンドウサイズをリセット: {this.Width}x{this.Height} -> {initialWidth}x{initialHeight}");
                this.Width = initialWidth;
                this.Height = initialHeight;
            }
            
            // キャラクター画像を初期状態に戻す
            RestoreCharacterImageSize();
        }
        
        // 新規: キャラクター画像のサイズと位置を初期状態に戻すメソッド
        private void RestoreCharacterImageSize()
        {
            // キャラクター画像を初期サイズに戻す
            CharacterImage.Width = double.NaN; // Auto
            CharacterImage.Height = double.NaN; // Auto
            
            // マージンを底辺固定で調整
            CharacterImage.Margin = new Thickness(10, 10, 10, 10);
            CharacterImage.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom;
            CharacterImage.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            CharacterImage.Stretch = Avalonia.Media.Stretch.Uniform;
        }
        
        /// <summary>
        /// 現在の時刻を表示します
        /// </summary>
        private void ShowCurrentTime(object? sender, RoutedEventArgs e)
        {
            // 時刻のみをフォーマット
            string currentTime = DateTime.Now.ToString("HH時mm分ss秒");
            
            // 時刻のみのシンプルなメッセージ
            ShowSpeechBubble($"今は{currentTime}だよ〜");
        }
        
        /// <summary>
        /// 現在の日付を表示します
        /// </summary>
        private void ShowCurrentDate(object? sender, RoutedEventArgs e)
        {
            // 日付のみをフォーマット
            string currentDate = DateTime.Now.ToString("yyyy年MM月dd日");
            
            // 日付のみのシンプルなメッセージ
            ShowSpeechBubble($"今日は{currentDate}だよ〜");
        }

        /// <summary>
        /// 応答テキストを表示します
        /// </summary>
        // 吹き出しの表示ロジックを統一
        private async void ShowSpeechBubble(string message)
        {
            ResponseText.Text = message;
            SpeechBubble.IsVisible = true;
            
            // 表示状態を即時に適用
            SpeechBubble.InvalidateVisual();
            
            // キャラクター位置の調整を即時反映
            AdjustCharacterImagePosition();

            // 閉じるボタンの処理を追加したため、自動非表示は不要になった
            // await Task.Delay(3000); // 3秒間表示
            // SpeechBubble.IsVisible = false; // 吹き出しを非表示
        }
        
        /// <summary>
        /// 吹き出しの閉じるボタンがクリックされたときの処理
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // 処理順序を整理して確実に吹き出しが非表示になるようにする
            
            // すぐに再描画が反映されるように、UIスレッドで同期的に実行
            Dispatcher.UIThread.InvokeAsync(() => {
                try {
                    // 吹き出しを非表示に設定する前に、キャラクター位置を元に戻す
                    RestoreCharacterImageSize();
                    
                    // テキストをクリアしてメモリ解放を促進
                    ResponseText.Text = string.Empty;
                    
                    // 吹き出しを非表示にする
                    SpeechBubble.IsVisible = false;
                    
                    // バックグラウンド設定を確認
                    this.Background = Brushes.Transparent;
                    
                    // 即時に再描画を強制（同期的に実行）
                    SpeechBubble.InvalidateVisual();
                    CharacterImage.InvalidateVisual();
                    this.InvalidateVisual();
                    
                    // レイアウト全体の再計算を強制
                    this.InvalidateMeasure();
                    this.InvalidateArrange();
                } catch (Exception ex) {
                    Console.WriteLine($"吹き出しの非表示処理中にエラー発生: {ex.Message}");
                }
            }, DispatcherPriority.Send);
            
            // UI全体の再描画をDispatcherの高優先度で強制
            Dispatcher.UIThread.Post(() => {
                // 複数の要素を確実に再描画
                if (SpeechBubble != null) {
                    SpeechBubble.InvalidateVisual();
                }
                if (CharacterImage != null) {
                    CharacterImage.InvalidateVisual();
                }
                this.InvalidateVisual();
            }, DispatcherPriority.Render);
        }

        // 画面の右下にウィンドウを配置するメソッド
        private void PositionWindowBottomRight()
        {
            var screen = Screens.Primary;
            if (screen != null)
            {
                var workingArea = screen.WorkingArea;
                var padding = 20; // 画面端からの余白（ピクセル）

                // ウィンドウの位置を画面の右下に設定（doubleからintへ明示的に変換）
                this.Position = new PixelPoint(
                    (int)(workingArea.Right - this.ClientSize.Width - padding),
                    (int)(workingArea.Bottom - this.ClientSize.Height - padding)
                );
            }
        }

        // 追加: PointerPressedイベントハンドラを実装
        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // 吹き出しが表示されている場合、どこをクリックしても吹き出しを閉じる
            if (SpeechBubble.IsVisible)
            {
                // クリック位置に関わらず吹き出しを閉じる
                CloseButton_Click(this, new RoutedEventArgs());
                return;
            }
            
            // 通常のドラッグ処理はそのまま実行
            if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
            {
                isDragging = true;
                dragOffset = e.GetPosition(this);
            }
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (isDragging)
            {
                var currentMousePos = e.GetPosition(this);
                var delta = currentMousePos - dragOffset;
                this.Position = new PixelPoint(
                    this.Position.X + (int)delta.X,
                    this.Position.Y + (int)delta.Y
                );
            }
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton == MouseButton.Left)
            {
                isDragging = false;
            }
        }

        private async Task OnWindowOpenedAsync()
        {
            await Task.Run(() =>
            {
                var networkStatus = NetworkChecker.CheckNetworkStatus();
                Dispatcher.UIThread.InvokeAsync(() => ShowSpeechBubble(networkStatus));
            });
        }

        public void CheckNetworkStatusFromMenu(object? sender, RoutedEventArgs e)
        {
            var networkStatus = NetworkChecker.CheckNetworkStatus();
            ShowSpeechBubble(networkStatus);
            
            // 1回だけRestoreWindowSizeを呼び出す（重複を削除）
            RestoreWindowSize();
        }

        // 修正: AdjustCharacterImagePosition メソッドを変更して元のウィンドウサイズを尊重
        private void AdjustCharacterImagePosition()
        {
            if (CharacterImage.Source is Bitmap bitmap)
            {
                // ウィンドウサイズを変更せず、必要な場合だけキャラクター画像の位置を調整
                if (SpeechBubble.IsVisible)
                {
                    // 吹き出しの高さに合わせて調整（より大きなスペースを確保）
                    double messageAreaHeight = 170; // 吹き出しの高さを拡大
                    CharacterImage.Margin = new Thickness(10, messageAreaHeight, 10, 10);
                }
                else
                {
                    // 吹き出しがない場合は初期状態に戻す
                    RestoreCharacterImageSize();
                }
            }
        }

        /// <summary>
        /// お戯れ：ひろゆきのセリフを表示します
        /// </summary>
        private void ShowHiroyukiQuote(object? sender, RoutedEventArgs e)
        {
            ShowSpeechBubble("それって貴方の感想ですよね？");
        }
        
        /// <summary>
        /// お戯れ：おやじギャグをランダムに表示します
        /// </summary>
        private void ShowRandomDadJoke(object? sender, RoutedEventArgs e)
        {
            // 指定された果物のリスト
            string[] fruits = new string[]
            {
                "布団がふっとんだ",
                "トイレに行っといれ",
                "ダジャレを言うのはだれじゃ",
                "アイスクリームが愛すクリ－ム",
                "猫が転んで、ねころんだ",
                "アルミ缶の上にあるミカン",
                "タイが食べたい",
                "ダジャレを言うのはだれじゃ"
            };
            
            // ランダムで果物を選択
            Random random = new Random();
            int index = random.Next(fruits.Length);
            
            // 選択した果物を表示
            ShowSpeechBubble(fruits[index]);
        }

        private string GenerateResponse(string prompt)
        {
            return "応答を生成しました: " + prompt;
        }
        
        private void RegisterEventHandlers()
        {
            // null参照警告(CS8622)を修正
            this.PointerPressed += OnPointerPressed;
            this.PointerMoved += OnPointerMoved;
            this.PointerReleased += OnPointerReleased;

            SpeechBubble.SizeChanged += (sender, e) => AdjustCharacterImagePosition();

            var contextMenu = CreateContextMenu();
            this.PointerPressed += (sender, e) =>
            {
                if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
                {
                    contextMenu.Open(this);
                }
            };
        }

        // 修正: コンテキストメニューの作成とイベント処理を改善
        private ContextMenu CreateContextMenu()
        {
            var contextMenu = new ContextMenu();

            // 「システム」メニューを追加
            var systemMenuItem = new MenuItem { Header = "システム" };
            
            // 「日付表示」をシステムのサブメニューに追加
            var showDateMenuItem = new MenuItem { Header = "日付表示" };
            showDateMenuItem.Click += (s, e) => 
            { 
                ShowCurrentDate(s, e);
            };
            systemMenuItem.Items.Add(showDateMenuItem);

            // 「時刻表示」をシステムのサブメニューに追加
            var showTimeMenuItem = new MenuItem { Header = "時刻表示" };
            showTimeMenuItem.Click += (s, e) => 
            { 
                ShowCurrentTime(s, e);
            };
            systemMenuItem.Items.Add(showTimeMenuItem);

            // 「ネットワーク状態を確認」をシステムのサブメニューに追加
            var checkNetworkMenuItem = new MenuItem { Header = "ネットワーク状態を確認" };
            checkNetworkMenuItem.Click += (s, e) => 
            { 
                // CheckNetworkStatusFromMenuの内部ですでにRestoreWindowSizeを呼び出している
                CheckNetworkStatusFromMenu(s, e);
            };
            systemMenuItem.Items.Add(checkNetworkMenuItem);
            
            // システムメニューをコンテキストメニューに追加
            contextMenu.Items.Add(systemMenuItem);

            // 「お戯れ」メニューを追加
            var otagareMenuItem = new MenuItem { Header = "お戯れ" };
            
            // 「ひろゆき」サブメニュー項目を追加
            var hiroyukiMenuItem = new MenuItem { Header = "ひろゆき" };
            hiroyukiMenuItem.Click += ShowHiroyukiQuote;
            otagareMenuItem.Items.Add(hiroyukiMenuItem);
            
            // 「おやじギャグ」サブメニュー項目を追加
            var dadJokeMenuItem = new MenuItem { Header = "おやじギャグ" };
            dadJokeMenuItem.Click += ShowRandomDadJoke;
            otagareMenuItem.Items.Add(dadJokeMenuItem);
            
            contextMenu.Items.Add(otagareMenuItem);

            // 設定項目の前にセパレータを追加（NativeMenuItemSeparatorからSeparatorに変更）
            contextMenu.Items.Add(new Separator());

            // 「設定」メニューを追加
            var settingsMenuItem = new MenuItem { Header = "設定" };
            settingsMenuItem.Click += ShowSettingsWindow;
            contextMenu.Items.Add(settingsMenuItem);

            // 設定項目の後にセパレータを追加（NativeMenuItemSeparatorからSeparatorに変更）
            contextMenu.Items.Add(new Separator());

            var closeMenuItem = new MenuItem { Header = "閉じる" };
            closeMenuItem.Click += (s, e) => 
            {
                Console.WriteLine("アプリケーションを終了");
                Environment.Exit(0);
            };
            contextMenu.Items.Add(closeMenuItem);

            // コンテキストメニューが閉じられた後にウィンドウサイズをリセット
            // (ここで1回だけ呼び出すことで重複呼び出しを防ぐ)
            contextMenu.Closed += (s, e) => 
            {
                Dispatcher.UIThread.Post(() => RestoreWindowSize(), DispatcherPriority.Render);
            };

            return contextMenu;
        }

        private async Task SomeAsyncMethod()
        {
            // 必要な非同期処理を追加
            await Task.Delay(1000); // 例として1秒待機
        }

        private void SomeMethod()
        {
            // 非同期呼び出しの結果を待機
            _ = SomeAsyncMethod();
        }

        private void AnotherMethod()
        {
            // asyncを削除し、同期的に実行
            Task.Run(() => Console.WriteLine("同期的に実行"));
        }

        private void ShowSettingsWindow(object? sender, RoutedEventArgs e)
        {
            // CS8622: イベントハンドラの型を修正
            if (sender is not null)
            {
                try
                {
                    var settingsWindow = new SettingsWindow();
                    
                    // ShowDialogからShowに変更し、モーダルではなく独立したウィンドウとして表示
                    settingsWindow.Show();
                    
                    // SettingWindowが閉じられた際のイベントハンドラ
                    settingsWindow.Closed += (s, args) =>
                    {
                        // 設定画面が閉じられた後に設定を適用
                        ApplySettings();
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"設定画面の表示中にエラーが発生しました: {ex.Message}");
                }
            }
        }

        // 設定を適用するメソッド
        private void ApplySettings()
        {
            try
            {
                var settings = Settings.Instance;
                
                // キャラクター画像の更新
                UpdateCharacterImage(settings.CharacterImageFileName);
                
                // 吹き出しの色を更新
                SpeechBubble.Background = settings.GetSpeechBubbleBackgroundBrush();
                SpeechBubble.BorderBrush = settings.GetSpeechBubbleBorderBrush();
                ResponseText.Foreground = settings.GetSpeechBubbleTextBrush();
                
                // 再描画を強制
                SpeechBubble.InvalidateVisual();
                CharacterImage.InvalidateVisual();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"設定の適用中にエラーが発生しました: {ex.Message}");
            }
        }

        // キャラクター画像を更新するメソッド
        private void UpdateCharacterImage(string characterFileName)
        {
            try
            {
                var resourcesDir = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
                var imagePath = Path.Combine(resourcesDir, characterFileName);
                
                if (File.Exists(imagePath))
                {
                    var bitmap = new Bitmap(imagePath);
                    CharacterImage.Source = bitmap;
                    
                    // キャラクター画像が変わった場合はウィンドウサイズも調整
                    if (bitmap != null)
                    {
                        double aspectRatio = bitmap.Size.Height / bitmap.Size.Width;
                        double calculatedHeight = 300 * aspectRatio;
                        
                        initialHeight = calculatedHeight + 130;
                        this.Height = initialHeight;
                        
                        // 再配置
                        PositionWindowBottomRight();
                    }
                }
                else
                {
                    Console.WriteLine($"キャラクター画像が見つかりません: {imagePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"キャラクター画像の更新中にエラーが発生しました: {ex.Message}");
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            // ウィンドウの透明度に関する設定を明示的に行う
            this.TransparencyLevelHint = new WindowTransparencyLevel[] { 
                WindowTransparencyLevel.Transparent,
                WindowTransparencyLevel.AcrylicBlur
            };
            
            // macOS向けの透明度設定を最適化
            this.Background = null; // 完全に透明にするためにnullを設定
            this.SystemDecorations = SystemDecorations.None;
            
            // キャラクター画像とウィンドウサイズの初期化
            InitializeCharacterImageAndWindowSize();
            
            // 初期設定を読み込む
            LoadInitialSettings();

            // イベントハンドラの登録
            RegisterEventHandlers();

            // ウィンドウを最前面に表示
            this.Topmost = true;

            // 画面の右下にウィンドウを配置
            this.WindowStartupLocation = WindowStartupLocation.Manual;

            // ウィンドウが開かれた後に追加の透明度設定を適用
            this.Opened += (sender, e) => {
                PositionWindowAccordingToSettings();

                // macOSでの透明度問題に対処
                this.Background = null;
                Dispatcher.UIThread.Post(() => {
                    this.InvalidateVisual();
                    this.InvalidateMeasure();
                    this.InvalidateArrange();
                }, DispatcherPriority.Render);
                
                // ルートグリッドの背景も確実に透明に
                if (this.Content is Grid rootGrid)
                {
                    rootGrid.Background = Brushes.Transparent;
                }
            };
            
            // ポインターイベントのハンドラを登録（クリック時の背景色変更を防止）
            this.AddHandler(Avalonia.Input.InputElement.PointerEnteredEvent, 
                new EventHandler<PointerEventArgs>((s, e) => {
                    this.Background = null;
                }), handledEventsToo: true);
                
            this.AddHandler(Avalonia.Input.InputElement.PointerExitedEvent, 
                new EventHandler<PointerEventArgs>((s, e) => {
                    this.Background = null;
                }), handledEventsToo: true);
            
            // UIの初期状態を明示的に設定
            SpeechBubble.IsVisible = false;
            SpeechBubble.Opacity = 1.0;
        }
        
        // 初期設定を読み込むメソッドを追加
        private void LoadInitialSettings()
        {
            try
            {
                var settings = Settings.Instance;
                
                // 利用可能なキャラクターを検索
                settings.ScanForCharacters();
                
                // 設定を適用
                ApplySettings();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初期設定の読み込み中にエラーが発生しました: {ex.Message}");
            }
        }
        
        // 設定に従ってウィンドウの位置を調整するメソッド
        private void PositionWindowAccordingToSettings()
        {
            var settings = Settings.Instance;
            var screen = Screens.Primary;
            
            if (screen != null)
            {
                var workingArea = screen.WorkingArea;
                var padding = settings.ScreenMargin;
                
                switch (settings.StartupPosition)
                {
                    case "右下":
                        this.Position = new PixelPoint(
                            (int)(workingArea.Right - this.ClientSize.Width - padding),
                            (int)(workingArea.Bottom - this.ClientSize.Height - padding)
                        );
                        break;
                    case "右上":
                        this.Position = new PixelPoint(
                            (int)(workingArea.Right - this.ClientSize.Width - padding),
                            (int)(workingArea.Y + padding)
                        );
                        break;
                    case "左下":
                        this.Position = new PixelPoint(
                            (int)(workingArea.X + padding),
                            (int)(workingArea.Bottom - this.ClientSize.Height - padding)
                        );
                        break;
                    case "左上":
                        this.Position = new PixelPoint(
                            (int)(workingArea.X + padding),
                            (int)(workingArea.Y + padding)
                        );
                        break;
                    default:
                        // デフォルトは右下
                        PositionWindowBottomRight();
                        break;
                }
            }
        }
    }
    
    public static class NetworkChecker
    {
        public static string CheckNetworkStatus()
        {
            try
            {
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    return "ネットワークは「良好」です〜";
                }
                else
                {
                    return "ネットワークが接続されていません〜";
                }
            }
            catch (Exception ex)
            {
                return $"あれ？ネットワーク状態の確認中にエラーが発生しました: {ex.Message}";
            }
        }
    }

    public static class MessageBox
    {
        public static async Task Show(Window parent, string message, string title)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            // ReactiveCommandの使用箇所を修正
            dialog.Content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = message, Margin = new Thickness(10) },
                    new Button
                    {
                        Content = "OK",
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Margin = new Thickness(10),
                        Command = ReactiveCommand.Create(() => dialog.Close())
                    }
                }
            };

            dialog.ShowDialog(parent);
        }
    }
}