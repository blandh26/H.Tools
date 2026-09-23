<div align="center">

<img src="src/HTools.App/Assets/toolbox.svg" width="96" alt="H.Tools" />

# H.Tools

[简体中文](README.md) | [English](README.en.md) | **日本語** | [한국어](README.ko.md)

日常業務や開発・デバッグでよく使う小さなツールを 1 つのウィンドウにまとめた、Windows 向けの軽量デスクトップツールボックスです。

![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D6)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)
![Avalonia](https://img.shields.io/badge/Avalonia-12-8B44AC)

</div>

## ✨ 機能一覧

### よく使うツール

| ツール | 説明 |
| --- | --- |
| 📋 **マルチクリップボード** | よく使う内容（テキスト / 画像）を 10 件保存し、グローバルホットキー `Ctrl+1` 〜 `Ctrl+0` ですばやく貼り付け |
| 🖱 **マウスエフェクト** | マウスをすばやく振ると梅の花の軌跡が表示されます。アプリセンターからワンクリックでオン / オフ |
| ✂ **スクリーンショット** | 任意の範囲をドラッグ選択、またはウィンドウにホバーしてクリックで選択。矩形・楕円（枠線 / 塗りつぶし）、直線、矢印、ペン、テキスト、モザイクで注釈。コピー、PNG 保存、画面への固定表示に対応。マルチモニター・異なる DPI スケーリングにも対応 |
| 📊 **システムモニター** | CPU・メモリ・ネットワークのリアルタイムグラフ、CPU / GPU 温度、プロセッサー・マザーボード・グラフィックス・メモリ・ストレージ・ネットワークアダプターの情報を表示 |

### 開発ツール

| ツール | 説明 |
| --- | --- |
| 📮 **Mock クライアント** | Postman 風のリクエスト画面。HTTP リクエストを手動で作成して応答を確認。直近 100 件の履歴を保持 |
| **Mock サーバー** | 固定の応答を返すサーバーを起動し、受信したリクエストをすべて記録 |
| **Mock API サーバー** | HTTP メソッド + パスでルールを定義し、ステータスコード・Content-Type・本文を返します |
| **静的ファイルサーバー** | ローカルフォルダーを HTTP で公開 |
| **ファイルアップロードサーバー** | アップロード用 Web フォームを提供（`curl -F` にも対応）。指定フォルダーに保存 |
| **Webhook 受信器** | 外部システムからの Webhook リクエスト（ヘッダー + 本文）を受信して記録 |
| **リバースプロキシ** | リクエストを転送先 URL に転送し、通信内容を記録 |
| **遅延シミュレーション** | 転送前に人為的な遅延を加え、低速ネットワークをテスト |

### アプリの特長

- **アプリセンター**：全ツールをカード表示。検索、ピン留め、ドラッグで並べ替え
- **カスタムツール**：ローカルプログラム（.exe）や URL を追加し、内蔵ツールと一緒に管理
- **多言語対応**：简体中文 / English / 日本語 / 한국어。切り替えは再起動不要で即時反映
- **ダーク / ライトテーマ**：タイトルバーからワンクリックで切り替え
- **最前面表示**、**Windows 起動時に自動実行**、**システムトレイに最小化**
- **設定の永続化**：すべての設定を LiteDB データベース `%LOCALAPPDATA%\HTools\settings.db` に保存

## 🖥 動作環境

- Windows 10 / 11（x64）
- ソースからビルドするには [.NET 10 SDK](https://dotnet.microsoft.com/download) が必要です

> 一部のハードウェア温度センサーは、管理者として実行するか、マザーボード / グラフィックスのメーカー製ドライバーがないと読み取れません。

## 🚀 ビルドと実行

```bash
git clone https://github.com/blandh26/H.Tools.git
cd H.Tools
dotnet run --project src/HTools.App
```

単体テストの実行：

```bash
dotnet test
```

単体で動作する実行ファイルとして発行：

```bash
dotnet publish src/HTools.App -c Release -r win-x64 --self-contained
```

## 📁 プロジェクト構成

```
src/
├─ HTools.App       Avalonia UI（ビュー、ビューモデル、アプリサービス）
├─ HTools.Core      モデル、多言語サービスと言語ファイル、LiteDB 設定ストア
├─ HTools.Windows   Win32 連携：グローバルホットキー、スクリーンショット、クリップボード、SVG 描画
└─ HTools.Server    Kestrel ベースの各種ローカルサーバーモジュール
tests/
└─ HTools.Core.Tests  xUnit 単体テスト
```

## 🧩 技術スタック

- [Avalonia UI 12](https://avaloniaui.net/) + Fluent テーマ
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- ASP.NET Core Kestrel（ローカルサーバーモジュール）
- [LiteDB](https://www.litedb.org/)（設定の保存）
- [Svg.NET](https://github.com/svg-net/SVG)（アイコン描画）
- [LibreHardwareMonitorLib](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor)、System.Management（ハードウェア情報）

サードパーティのライセンス情報は [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) を参照してください。

## 🌐 言語の追加

言語ファイルは `src/HTools.Core/Resources/Lang/` にあります（JSON 形式、埋め込みリソース）。既存のファイルをコピーしてすべての値を翻訳し、`LocalizationService` の `SupportedLanguages` に登録してください。

## 📮 お問い合わせ

- メール：[blandh26@gmail.com](mailto:blandh26@gmail.com)
- Web サイト：[www.kimchicoder.com](https://www.kimchicoder.com)
