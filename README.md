# MacMascotApp

MacMascotAppは、Windows用のWinMascotTemplateをMacOS向けに移植したデスクトップマスコットアプリケーションです。Avalonia UIフレームワークを使用して実装されています。

- [MinatsuT/WinMascotTemplate: Windows Mascot C# Template](https://github.com/MinatsuT/WinMascotTemplate)

## 機能

- デスクトップにキャラクターを表示
- ウィンドウ内の背景が透明になり、キャラクターのみ表示
- ドラッグ操作でキャラクターを移動可能
- 右クリックでコンテキストメニュー表示
- メニューから「閉じる」を選んでアプリケーション終了
- 起動時に画面の右下に自動配置
- 常に最前面に表示
- **AI会話機能** - テキスト入力でキャラクターと会話可能
- **吹き出し表示** - AIの応答をキャラクターの吹き出しとして表示
- **タイムアウト処理** - 応答が10秒以上かかる場合は自動的にタイムアウト

## 技術的な特徴

- [Avalonia UI](https://avaloniaui.net/)を使用したクロスプラットフォーム実装
- 透過ウィンドウの実装
- マウス操作によるウィンドウ移動
- コンテキストメニューの実装
- 画面サイズに基づく動的な配置
- リソースの動的な読み込みと管理
- 非同期処理によるAI応答生成
- タイムアウト処理による応答性の確保
- 最適化されたUIレイアウト（メッセージエリア分離）
- ダークモード対応のUI設計

## 必要条件

- [.NET SDK](https://dotnet.microsoft.com/download) 9.0以上
- MacOS 10.15以上

## インストール・実行方法

1. リポジトリをクローンまたはダウンロード
2. プロジェクトディレクトリに移動
   ```bash
   cd MacMascotApp
   ```
3. アプリをビルド
   ```bash
   dotnet build
   ```
4. アプリを実行
   ```bash
   dotnet run
   ```

## カスタマイズ方法

### キャラクター画像の変更

`Resources`ディレクトリに独自の`Character.png`ファイルを配置することで、表示されるキャラクターを変更できます。画像は透過PNG形式を推奨します。

### サイズの変更

`MainWindow.axaml`ファイル内の`Width`プロパティを編集することで、ウィンドウの幅を調整できます。

### AIモデルの変更

現在のバージョンではモック実装を使用していますが、`LlamaModelInterface`インターフェースを実装して独自のAIモデルを組み込むことができます。サポートされているモデル：

- LLamaSharpによるローカルモデル
- OpenAI APIを使用したリモートモデル
- その他のカスタムモデル実装

### レイアウト調整

`MainWindow.axaml.cs`内の以下の値を調整することでレイアウトをカスタマイズできます：

- `messageAreaHeight` - メッセージ表示エリアの高さ
- マージン設定 - 要素間の間隔
- パディング - 内部の余白

## Windows版との違い

このアプリケーションは元々Windows Forms用に開発されたWinMascotTemplateをAvalonia UIを使用してMacOS向けに移植したものです。主な違いは以下の通りです：

- Windows Forms APIからAvalonia UI APIへの置き換え
- リソース管理の違いに対応
- MacOS特有のウィンドウ処理への対応
- AI会話機能の追加（オリジナルには存在しない機能）

## ライセンス

このプロジェクトは元のWinMascotTemplateと同じライセンスの下で公開されています。