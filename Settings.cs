using System;
using System.IO;
using System.Text.Json;
using Avalonia.Media;
using System.Collections.Generic;
using System.Linq;

namespace MacMascotApp
{
    /// <summary>
    /// アプリケーションの設定を管理するクラス
    /// </summary>
    public class Settings
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MacMascotApp",
            "settings.json");

        private static Settings? _instance;

        /// <summary>
        /// シングルトンインスタンスを取得します
        /// </summary>
        public static Settings Instance
        {
            get
            {
                _instance ??= Load();
                return _instance;
            }
        }

        /// <summary>
        /// 選択されたキャラクター画像のファイル名
        /// </summary>
        public string CharacterImageFileName { get; set; } = "Character.png";

        /// <summary>
        /// 利用可能なキャラクター画像のリスト
        /// </summary>
        public List<string> AvailableCharacters { get; set; } = new List<string> { "Character.png" };

        /// <summary>
        /// 吹き出しの背景色
        /// </summary>
        public string SpeechBubbleBackgroundColor { get; set; } = "#FFFFFF";

        /// <summary>
        /// 吹き出しの枠線の色
        /// </summary>
        public string SpeechBubbleBorderColor { get; set; } = "#000000";

        /// <summary>
        /// 吹き出しのテキスト色
        /// </summary>
        public string SpeechBubbleTextColor { get; set; } = "#000000";

        /// <summary>
        /// 起動時の配置位置（左上, 左下, 右上, 右下）
        /// </summary>
        public string StartupPosition { get; set; } = "右下";

        /// <summary>
        /// 画面からの余白（ピクセル）
        /// </summary>
        public int ScreenMargin { get; set; } = 20;

        /// <summary>
        /// 設定が変更されたかどうかを示すプロパティ
        /// </summary>
        public bool HasChanges { get; set; } = false;

        /// <summary>
        /// 設定をファイルに保存します
        /// </summary>
        public void Save()
        {
            try
            {
                // 設定ディレクトリが存在しない場合は作成
                string dirPath = Path.GetDirectoryName(SettingsFilePath) ?? string.Empty;
                if (!string.IsNullOrEmpty(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                // 設定をJSONに変換して保存
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SettingsFilePath, json);

                Console.WriteLine($"設定を保存しました: {SettingsFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"設定の保存中にエラーが発生しました: {ex.Message}");
            }
        }

        /// <summary>
        /// 設定をファイルから読み込みます
        /// </summary>
        private static Settings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<Settings>(json);
                    if (settings != null)
                    {
                        Console.WriteLine("設定ファイルを読み込みました");
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"設定の読み込み中にエラーが発生しました: {ex.Message}");
            }

            Console.WriteLine("デフォルト設定を使用します");
            return new Settings();
        }

        /// <summary>
        /// 吹き出しの背景色をBrushとして取得します
        /// </summary>
        public IBrush GetSpeechBubbleBackgroundBrush()
        {
            try
            {
                return SolidColorBrush.Parse(SpeechBubbleBackgroundColor);
            }
            catch
            {
                return Brushes.White;
            }
        }

        /// <summary>
        /// 吹き出しの枠線色をBrushとして取得します
        /// </summary>
        public IBrush GetSpeechBubbleBorderBrush()
        {
            try
            {
                return SolidColorBrush.Parse(SpeechBubbleBorderColor);
            }
            catch
            {
                return Brushes.Black;
            }
        }

        /// <summary>
        /// 吹き出しのテキスト色をBrushとして取得します
        /// </summary>
        public IBrush GetSpeechBubbleTextBrush()
        {
            try
            {
                return SolidColorBrush.Parse(SpeechBubbleTextColor);
            }
            catch
            {
                return Brushes.Black;
            }
        }

        /// <summary>
        /// 利用可能なキャラクター画像を検索します
        /// </summary>
        public void ScanForCharacters()
        {
            try
            {
                var resourcesDir = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
                if (Directory.Exists(resourcesDir))
                {
                    var pngFiles = Directory.GetFiles(resourcesDir, "*.png");
                    AvailableCharacters.Clear();

                    // CS8620: NULL許容型の違いを解消
                    AvailableCharacters.AddRange(pngFiles.Select(file => Path.GetFileName(file) ?? string.Empty));

                    // 少なくとも1つはキャラクターがあるか確認
                    if (AvailableCharacters.Count == 0)
                    {
                        AvailableCharacters.Add("Character.png");
                    }

                    // 選択しているキャラクターが有効なものかチェック
                    if (!AvailableCharacters.Contains(CharacterImageFileName))
                    {
                        CharacterImageFileName = AvailableCharacters[0];
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"利用可能なキャラクターの検索中にエラーが発生しました: {ex.Message}");
            }
        }
    }
}