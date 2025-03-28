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

namespace MacMascotApp
{
    public partial class MainWindow : Window
    {
        private bool isDragging = false;
        private Point dragOffset;
        private LlamaModelInterface? _llmModel; // Null許容として宣言
        private bool _isProcessingRequest = false;

        public MainWindow()
        {
            InitializeComponent();

            // プロジェクトルートディレクトリからリソースパスを生成
            var baseDirectory = Directory.GetCurrentDirectory();
            var characterImagePath = Path.Combine(baseDirectory, "Resources", "Character.png");

            if (File.Exists(characterImagePath))
            {
                var bitmap = new Bitmap(characterImagePath);
                CharacterImage.Source = bitmap;
                
                // 画像のアスペクト比を維持しつつ、ウィンドウの高さを計算
                // Width=300は既に設定されているので、それに基づいて高さを計算
                double aspectRatio = (double)bitmap.Size.Height / bitmap.Size.Width;
                double calculatedHeight = 300 * aspectRatio; // 300はウィンドウの幅
                
                // メッセージエリア（吹き出し）用のスペースを確保し、入力エリア用の余白も追加
                // 約3行分のメッセージ表示スペース（約80ピクセル）と入力エリア用の余白（約50ピクセル）
                double messageAreaHeight = 80;
                this.Height = calculatedHeight + messageAreaHeight + 50;
                
                // 吹き出しの位置を調整 - 上部に固定
                SpeechBubble.Margin = new Thickness(10, 10, 10, 0);
                SpeechBubble.MaxHeight = messageAreaHeight;
                
                // キャラクター画像の位置を調整 - 吹き出しの下に配置
                CharacterImage.Margin = new Thickness(0, messageAreaHeight, 0, 0);
            }
            else
            {
                Console.WriteLine($"Character image not found at: {characterImagePath}");
            }

            // コンテキストメニューの作成
            var contextMenu = new ContextMenu();
            var closeMenuItem = new MenuItem { Header = "閉じる" };
            closeMenuItem.Click += (s, e) => 
            {
                Console.WriteLine("アプリケーションを終了します。");
                Environment.Exit(0);
            };
            contextMenu.Items.Add(closeMenuItem);

            // ウィンドウのイベントハンドラを設定
            this.PointerPressed += OnPointerPressed;
            this.PointerMoved += OnPointerMoved;
            this.PointerReleased += OnPointerReleased;

            this.PointerPressed += (sender, e) =>
            {
                if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
                {
                    contextMenu.Open(this);
                }
            };

            // ウィンドウを最前面に表示
            this.Topmost = true;

            // 画面の右下にウィンドウを配置
            this.WindowStartupLocation = WindowStartupLocation.Manual;

            // ウィンドウが開かれた時に位置を設定
            this.Opened += (sender, e) => 
            {
                PositionWindowBottomRight();
                
                // LLMモデルの初期化（バックグラウンドで実行）
                Task.Run(InitializeLLMModelAsync);
            };
            
            // 送信ボタンのクリックイベント
            SendButton.Click += async (s, e) => await SendMessageAsync();
            
            // テキストボックスでエンターキー押下時のイベント
            InputTextBox.KeyDown += async (s, e) => 
            {
                if (e.Key == Key.Enter)
                {
                    await SendMessageAsync();
                }
            };
        }
        
        /// <summary>
        /// LLMモデルを初期化します
        /// </summary>
        private async Task InitializeLLMModelAsync()
        {
            try
            {
                // 現実的な実装では、ここにLLamaSharpなどのモデル初期化コードを記述
                // この例では、シンプルなモックモデルを使用
                _llmModel = new MockLlamaModel();
                await _llmModel.InitializeAsync();
                
                // UI初期化完了を通知
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ShowResponse("準備ができました！何か質問してください。");
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ShowResponse($"モデル初期化エラー: {ex.Message}");
                });
            }
        }
        
        /// <summary>
        /// メッセージを送信し、応答を表示します
        /// </summary>
        private async Task SendMessageAsync()
        {
            if (_isProcessingRequest || string.IsNullOrWhiteSpace(InputTextBox.Text))
                return;
                
            _isProcessingRequest = true;
            string userMessage = InputTextBox.Text;
            InputTextBox.Text = string.Empty;
            
            ShowResponse("考え中...");
            
            try
            {
                if (_llmModel == null)
                {
                    ShowResponse("AIモデルの読み込み中です。少々お待ちください。");
                    _isProcessingRequest = false;
                    return;
                }
                
                // タイムアウト処理の修正
                using var cancellationTokenSource = new System.Threading.CancellationTokenSource();
                cancellationTokenSource.CancelAfter(10000); // 10秒後にキャンセル
                
                try
                {
                    // タスク実行とタイムアウト管理
                    var responseTask = _llmModel.GenerateResponseAsync(userMessage);
                    var timeoutTask = Task.Delay(10000, cancellationTokenSource.Token);
                    
                    var completedTask = await Task.WhenAny(responseTask, timeoutTask);
                    
                    if (completedTask == responseTask)
                    {
                        // 応答が正常に取得できた場合
                        string response = await responseTask;
                        ShowResponse(response);
                        
                        // タイムアウトタスクをキャンセル
                        cancellationTokenSource.Cancel();
                    }
                    else
                    {
                        // タイムアウトが発生した場合
                        ShowResponse("応答時間が長すぎるため、回答できません。別の質問をお試しください。");
                    }
                }
                catch (OperationCanceledException)
                {
                    // キャンセル発生時（タイムアウト）
                    ShowResponse("応答時間が長すぎるため、回答できません。別の質問をお試しください。");
                }
                catch (Exception ex)
                {
                    ShowResponse($"応答の生成中にエラーが発生しました: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                ShowResponse($"エラーが発生しました: {ex.Message}");
            }
            finally
            {
                _isProcessingRequest = false;
            }
        }
        
        /// <summary>
        /// 応答テキストを表示します
        /// </summary>
        private void ShowResponse(string text)
        {
            ResponseText.Text = text;
            SpeechBubble.IsVisible = true;
            
            // 5秒間表示した後、自動的に非表示にする場合はここにコードを追加
            // Task.Delay(5000).ContinueWith(_ => Dispatcher.UIThread.InvokeAsync(() => SpeechBubble.IsVisible = false));
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

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
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
    }
    
    /// <summary>
    /// LLMモデルのインターフェース
    /// </summary>
    public interface LlamaModelInterface
    {
        Task InitializeAsync();
        Task<string> GenerateResponseAsync(string prompt);
    }
    
    /// <summary>
    /// モック実装（デモ用）
    /// 実際の実装では、LLamaSharpなどを使用したモデルを実装します
    /// </summary>
    public class MockLlamaModel : LlamaModelInterface
    {
        private readonly Random _random = new Random();
        
        public Task InitializeAsync()
        {
            // 実際のモデル初期化では数秒かかるため、遅延をシミュレート
            return Task.Delay(2000);
        }
        
        public async Task<string> GenerateResponseAsync(string prompt)
        {
            // 実際の推論をシミュレートするための遅延
            await Task.Delay(1000);
            
            // シンプルなレスポンスパターン
            if (prompt.Contains("こんにちは") || prompt.Contains("挨拶"))
            {
                return "こんにちは！お手伝いできることはありますか？";
            }
            else if (prompt.Contains("名前"))
            {
                return "私はマスコットAIアシスタントです。よろしくお願いします！";
            }
            else if (prompt.Contains("天気"))
            {
                return "窓の外を見てください。私からは見えないんです...";
            }
            else if (prompt.Contains("機能") || prompt.Contains("できること"))
            {
                return "現在はシンプルな会話ができます。将来的にはもっと多くの機能が追加される予定です！";
            }
            else
            {
                string[] responses = new[]
                {
                    "なるほど、興味深いですね。",
                    "もう少し詳しく教えていただけますか？",
                    "その質問にはまだ回答できません。別の質問はありますか？",
                    "了解しました！何か他にお手伝いできることはありますか？",
                    // 「考えさせてください...」は削除して代わりに以下を使用
                    "それについては十分な情報を持っていません。別の話題はどうですか？"
                };
                
                return responses[_random.Next(responses.Length)];
            }
        }
    }
}