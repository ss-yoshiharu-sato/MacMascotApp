using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.IO;
using System.Linq;
using Avalonia.Styling;

namespace MacMascotApp
{
    public partial class SettingsWindow : Window
    {
        // null許容属性を追加
        private Settings _settings = null!;
        private Settings _originalSettings = null!;
        
        public SettingsWindow()
        {
            InitializeComponent();
            InitializeSettings();
            SetupTheme();
            
            // イベントハンドラの設定 - null参照チェック付き
            if (CancelButton != null) CancelButton.Click += CancelButton_Click;
            if (SaveButton != null) SaveButton.Click += SaveButton_Click;
            if (CloseWindowButton != null) CloseWindowButton.Click += CloseWindowButton_Click;
            if (CharacterSelector != null) CharacterSelector.SelectionChanged += CharacterSelector_SelectionChanged;
            if (BackgroundColorSelector != null) BackgroundColorSelector.SelectionChanged += ColorSelector_SelectionChanged;
            if (BorderColorSelector != null) BorderColorSelector.SelectionChanged += ColorSelector_SelectionChanged;
            if (TextColorSelector != null) TextColorSelector.SelectionChanged += ColorSelector_SelectionChanged;
            
            if (StartupPositionSelector != null)
            {
                StartupPositionSelector.SelectionChanged += (s, e) => _settings.HasChanges = true;
            }
            
            if (MarginSlider != null)
            {
                MarginSlider.PropertyChanged += (s, e) => 
                {
                    if (e.Property.Name == "Value")
                    {
                        _settings.HasChanges = true;
                        UpdateMarginValueDisplay();
                    }
                };
            }
            
            // カラーピッカーのイベント
            if (BackgroundColorPicker != null)
            {
                BackgroundColorPicker.ColorChanged += (s, e) => 
                {
                    _settings.HasChanges = true;
                    UpdateColorFromPicker(BackgroundColorPicker, BackgroundColorSelector);
                };
            }
            
            if (BorderColorPicker != null)
            {
                BorderColorPicker.ColorChanged += (s, e) => 
                {
                    _settings.HasChanges = true;
                    UpdateColorFromPicker(BorderColorPicker, BorderColorSelector);
                };
            }
            
            if (TextColorPicker != null)
            {
                TextColorPicker.ColorChanged += (s, e) => 
                {
                    _settings.HasChanges = true;
                    UpdateColorFromPicker(TextColorPicker, TextColorSelector);
                };
            }
        }

        // システムテーマに合わせた設定
        private void SetupTheme()
        {
            try
            {
                // システムテーマに追従する設定
                this.RequestedThemeVariant = ThemeVariant.Default;
                
                // ウィンドウ設定の追加
                this.TransparencyLevelHint = new WindowTransparencyLevel[] { 
                    WindowTransparencyLevel.AcrylicBlur, 
                    WindowTransparencyLevel.Blur 
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"テーマ設定中にエラーが発生しました: {ex.Message}");
            }
        }

        // 閉じるボタンの処理を追加
        private void CloseWindowButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        // 初期設定の読み込みを確実にするために修正
        private void InitializeSettings()
        {
            // 現在の設定を取得
            _originalSettings = Settings.Instance;
            
            // 設定のコピーを作成して作業用とする
            _settings = new Settings();
            
            // 設定をコピー
            CopySettings(_originalSettings, _settings);
            
            // 利用可能なキャラクターを検索
            _settings.ScanForCharacters();
            
            // UIの初期化
            InitializeCharacterSelector();
            InitializeColorSelectors();
            InitializePositionSelector();
            InitializeMarginSlider();

            // HasChangesプロパティを初期化
            _settings.HasChanges = false;
        }

        private void InitializeCharacterSelector()
        {
            if (CharacterSelector == null) return;
            
            // キャラクター選択コンボボックスを初期化
            CharacterSelector.Items.Clear();
            foreach (var character in _settings.AvailableCharacters)
            {
                CharacterSelector.Items.Add(character);
            }
            
            // 現在選択されているキャラクターを設定
            var selectedIndex = _settings.AvailableCharacters.IndexOf(_settings.CharacterImageFileName);
            if (selectedIndex >= 0)
            {
                CharacterSelector.SelectedIndex = selectedIndex;
                UpdateCharacterPreview(_settings.CharacterImageFileName);
            }
        }

        private void InitializeColorSelectors()
        {
            if (BackgroundColorSelector == null || 
                BackgroundColorPicker == null ||
                BorderColorSelector == null ||
                BorderColorPicker == null ||
                TextColorSelector == null ||
                TextColorPicker == null) return;
                
            // 背景色の設定
            SetSelectedColorInComboBox(BackgroundColorSelector, _settings.SpeechBubbleBackgroundColor);
            BackgroundColorPicker.Color = ColorFromHexString(_settings.SpeechBubbleBackgroundColor);
            
            // 枠線色の設定
            SetSelectedColorInComboBox(BorderColorSelector, _settings.SpeechBubbleBorderColor);
            BorderColorPicker.Color = ColorFromHexString(_settings.SpeechBubbleBorderColor);
            
            // テキスト色の設定
            SetSelectedColorInComboBox(TextColorSelector, _settings.SpeechBubbleTextColor);
            TextColorPicker.Color = ColorFromHexString(_settings.SpeechBubbleTextColor);
        }

        private void InitializePositionSelector()
        {
            if (StartupPositionSelector == null) return;
            
            // 起動位置の設定
            for (int i = 0; i < StartupPositionSelector.Items.Count; i++)
            {
                if (StartupPositionSelector.Items[i] is ComboBoxItem item && 
                    item.Content?.ToString() == _settings.StartupPosition)
                {
                    StartupPositionSelector.SelectedIndex = i;
                    break;
                }
            }
            
            // 選択項目がなければデフォルト（右下）を選択
            if (StartupPositionSelector.SelectedIndex < 0)
            {
                StartupPositionSelector.SelectedIndex = 0;
            }
        }

        private void InitializeMarginSlider()
        {
            if (MarginSlider == null) return;
            
            // マージンスライダーの設定
            MarginSlider.Value = _settings.ScreenMargin;
            UpdateMarginValueDisplay();
        }

        private void UpdateMarginValueDisplay()
        {
            // マージン値の表示を更新
            if (MarginValue != null && MarginSlider != null)
            {
                MarginValue.Text = $"{(int)MarginSlider.Value}";
            }
        }

        private void UpdateCharacterPreview(string characterFileName)
        {
            try
            {
                if (CharacterPreview == null) return;
                
                var resourcesDir = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
                var imagePath = Path.Combine(resourcesDir, characterFileName);
                
                if (File.Exists(imagePath))
                {
                    CharacterPreview.Source = new Bitmap(imagePath);
                }
                else
                {
                    Console.WriteLine($"キャラクター画像が見つかりません: {imagePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"キャラクタープレビューの更新中にエラーが発生しました: {ex.Message}");
            }
        }

        private void CharacterSelector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (CharacterSelector?.SelectedItem is string selectedCharacter)
            {
                UpdateCharacterPreview(selectedCharacter);
                _settings.HasChanges = true;
            }
        }

        private void ColorSelector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string colorHex = selectedItem.Tag?.ToString() ?? "#FFFFFF";
                Color color = ColorFromHexString(colorHex);
                
                // 対応するColorPickerを更新
                if (comboBox == BackgroundColorSelector && BackgroundColorPicker != null)
                {
                    BackgroundColorPicker.Color = color;
                }
                else if (comboBox == BorderColorSelector && BorderColorPicker != null)
                {
                    BorderColorPicker.Color = color;
                }
                else if (comboBox == TextColorSelector && TextColorPicker != null)
                {
                    TextColorPicker.Color = color;
                }
                
                _settings.HasChanges = true;
            }
        }

        private void UpdateColorFromPicker(ColorPicker? picker, ComboBox? comboBox)
        {
            if (picker == null || comboBox == null) return;
            
            // ColorPickerで選択された色をHex文字列に変換
            string colorHex = ColorToHexString(picker.Color);
            
            // 対応するComboBoxでプリセットがあるか探す
            bool foundMatch = false;
            foreach (var item in comboBox.Items)
            {
                if (item is ComboBoxItem comboItem && 
                    comboItem.Tag?.ToString()?.ToUpperInvariant() == colorHex.ToUpperInvariant())
                {
                    comboBox.SelectedItem = comboItem;
                    foundMatch = true;
                    break;
                }
            }
            
            // プリセットにない場合、カスタム色として扱う
            if (!foundMatch)
            {
                // "カスタム"項目がなければ追加
                ComboBoxItem? customItem = null;
                foreach (var item in comboBox.Items)
                {
                    if (item is ComboBoxItem comboItem && comboItem.Content?.ToString() == "カスタム")
                    {
                        customItem = comboItem;
                        break;
                    }
                }
                
                if (customItem == null)
                {
                    customItem = new ComboBoxItem { Content = "カスタム" };
                    comboBox.Items.Add(customItem);
                }
                
                customItem.Tag = colorHex;
                comboBox.SelectedItem = customItem;
            }
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object? sender, RoutedEventArgs e)
        {
            // UIから設定を更新
            SaveSettingsFromUI();
            
            // 設定を保存
            _settings.Save();
            
            // 元の設定オブジェクトに値をコピー
            CopySettings(_settings, _originalSettings);
            
            Close();
        }

        private void SaveSettingsFromUI()
        {
            // キャラクター画像の設定を保存
            if (CharacterSelector?.SelectedItem is string selectedCharacter)
            {
                _settings.CharacterImageFileName = selectedCharacter;
                _settings.HasChanges = true;
            }
            
            // 色の設定を保存
            if (BackgroundColorSelector?.SelectedItem is ComboBoxItem bgItem)
            {
                _settings.SpeechBubbleBackgroundColor = bgItem.Tag?.ToString() ?? "#FFFFFF";
                _settings.HasChanges = true;
            }
            
            if (BorderColorSelector?.SelectedItem is ComboBoxItem borderItem)
            {
                _settings.SpeechBubbleBorderColor = borderItem.Tag?.ToString() ?? "#000000";
                _settings.HasChanges = true;
            }
            
            if (TextColorSelector?.SelectedItem is ComboBoxItem textItem)
            {
                _settings.SpeechBubbleTextColor = textItem.Tag?.ToString() ?? "#000000";
                _settings.HasChanges = true;
            }
            
            // 起動位置の設定を保存
            if (StartupPositionSelector?.SelectedItem is ComboBoxItem posItem)
            {
                _settings.StartupPosition = posItem.Content?.ToString() ?? "右下";
                _settings.HasChanges = true;
            }
            
            // マージンの設定を保存
            if (MarginSlider != null)
            {
                _settings.ScreenMargin = (int)MarginSlider.Value;
                _settings.HasChanges = true;
            }
        }

        // Settingsオブジェクト間で値をコピーするヘルパーメソッド
        private void CopySettings(Settings source, Settings target)
        {
            target.CharacterImageFileName = source.CharacterImageFileName;
            target.AvailableCharacters = source.AvailableCharacters.ToList();
            target.SpeechBubbleBackgroundColor = source.SpeechBubbleBackgroundColor;
            target.SpeechBubbleBorderColor = source.SpeechBubbleBorderColor;
            target.SpeechBubbleTextColor = source.SpeechBubbleTextColor;
            target.StartupPosition = source.StartupPosition;
            target.ScreenMargin = source.ScreenMargin;
        }

        // 色変換ヘルパーメソッド
        private void SetSelectedColorInComboBox(ComboBox? comboBox, string colorHex)
        {
            if (comboBox == null) return;
            
            foreach (var item in comboBox.Items)
            {
                if (item is ComboBoxItem comboItem && 
                    comboItem.Tag?.ToString()?.ToUpperInvariant() == colorHex.ToUpperInvariant())
                {
                    comboBox.SelectedItem = comboItem;
                    return;
                }
            }
            
            // マッチするアイテムがなければカスタムカラーとして追加
            var customItem = new ComboBoxItem { Content = "カスタム", Tag = colorHex };
            comboBox.Items.Add(customItem);
            comboBox.SelectedItem = customItem;
        }

        private Color ColorFromHexString(string hex)
        {
            try
            {
                if (string.IsNullOrEmpty(hex))
                {
                    return Colors.White;
                }
                
                if (hex.StartsWith("#"))
                {
                    hex = hex.Substring(1);
                }
                
                if (hex.Length == 6)
                {
                    byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                    byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                    byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                    return Color.FromRgb(r, g, b);
                }
                else if (hex.Length == 8)
                {
                    byte a = Convert.ToByte(hex.Substring(0, 2), 16);
                    byte r = Convert.ToByte(hex.Substring(2, 2), 16);
                    byte g = Convert.ToByte(hex.Substring(4, 2), 16);
                    byte b = Convert.ToByte(hex.Substring(6, 2), 16);
                    return Color.FromArgb(a, r, g, b);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"色の変換中にエラーが発生しました: {ex.Message}");
            }
            
            return Colors.White;
        }

        private string ColorToHexString(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}