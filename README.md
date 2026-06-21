# Code Catalog

ダウンロード/自作した .NET・Node.js・Python などのソースコードフォルダを一元管理するための、
クロスプラットフォーム(Windows / macOS / Linux)デスクトップアプリです。

- Avalonia UI (.NET 8) で作成、WPFではなくクロスプラットフォームGUI
- 指定フォルダ配下を再帰スキャンし、`.git` / `.sln` / `.csproj` / `package.json` などを目印に
  プロジェクトフォルダを自動検出
- プロジェクトごとにタグ・メモ・お気に入りを付けて検索可能
- フォルダを開く / VS Codeで開く / ターミナルで開く、をワンクリックで実行
- データはローカルのJSONファイルに保存(`%AppData%\CodeCatalog\catalog.json` など)。外部サーバー通信なし

## ⚠️ 重要な注意

このコードはサンドボックス環境(インターネット制限あり、NuGetにアクセス不可)で作成したため、
**実際にビルド・実行して動作確認はできていません。** 設計・構文は注意深く書きましたが、
お手元の環境で `dotnet build` した際に細かいエラーが出る可能性があります。
もしエラーが出たら、エラーメッセージを貼っていただければ一緒に直します。

## 必要なもの

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- (任意) VS Codeで開くボタンを使うなら、VS Codeの `code` コマンドをPATHに通しておく
  (VS Code内で `Shift+Ctrl+P` → 「シェル コマンド: PATH に 'code' コマンドをインストールします」)

## ビルド・実行方法

```bash
cd CodeCatalog/CodeCatalog
dotnet restore
dotnet run
```

初回は Avalonia / CommunityToolkit.Mvvm パッケージのダウンロードが入ります。

## 配布用ビルド(自己完結exe)を作る場合

```bash
# Windows向け
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# macOS向け (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true

# Linux向け
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
```

## フォルダ構成

```
CodeCatalog/
└── CodeCatalog/
    ├── CodeCatalog.csproj
    ├── Program.cs
    ├── App.axaml / App.axaml.cs
    ├── Models/
    │   └── ProjectEntry.cs        … 1プロジェクト分のデータ
    ├── Services/
    │   ├── ProjectScanner.cs      … フォルダ再帰スキャン・プロジェクト検出
    │   ├── CatalogStore.cs        … JSONへの保存/読み込み
    │   └── FolderOpener.cs        … エクスプローラー/VSCode/ターミナルを開く
    ├── ViewModels/
    │   ├── MainWindowViewModel.cs
    │   └── ProjectEntryViewModel.cs
    └── Views/
        ├── MainWindow.axaml
        └── MainWindow.axaml.cs
```

## 使い方

1. 「フォルダ追加」で、デスクトップやダウンロードフォルダなど、ソースコードが散らばっている
   親フォルダを登録する(例: `C:\Users\you\Desktop`)
2. 「スキャン実行」を押すと、登録フォルダ配下を自動で探索してプロジェクト一覧に追加
3. 一覧からプロジェクトを選び、右側パネルでタグ・メモ・お気に入りを設定
4. 検索ボックスで名前・タグ・メモ・種別を横断検索
5. 「フォルダを開く」「VS Code」「ターミナル」ボタンでそのプロジェクトにすぐアクセス

## カスタマイズしたい点があれば

- 検出するマーカー(言語の種類)を増やす: `Services/ProjectScanner.cs` の `Markers` 配列に追加
- スキャンを除外するフォルダ名を増やす: 同ファイルの `SkipDirNames` 配列に追加
- 既存のプロジェクトを実フォルダ構成に合わせて自動で移動・整理する機能なども追加可能です
