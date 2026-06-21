# Code Catalog

ダウンロード/自作した .NET・Node.js・Python などのソースコードフォルダを一元管理するための、
クロスプラットフォーム(Windows / macOS / Linux)デスクトップアプリです。

- Avalonia UI (.NET 10) で作成、WPFではなくクロスプラットフォームGUI
- 指定フォルダ配下を再帰スキャンし、`.git` / `.sln` / `.csproj` / `package.json` などを目印にプロジェクトフォルダを自動検出
- プロジェクトごとにタグを付けて検索可能
- フォルダを開く / VS Codeで開く / ターミナルで開く、をワンクリックで実行
- ビルド・実行をアプリ内から直接実行し、出力をリアルタイム表示
- GitHubリポジトリのクローンに対応
- データはローカルのJSONファイルに保存(`%AppData%\CodeCatalog\catalog.json` など)。外部サーバー通信なし

## 必要なもの

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- (任意) VS Codeで開くボタンを使うなら、VS Codeの `code` コマンドをPATHに通しておく
  (VS Code内で `Shift+Ctrl+P` → 「シェル コマンド: PATH に 'code' コマンドをインストールします」)

## ビルド・実行方法

```bash
cd CodeCatalog/CodeCatalog
dotnet restore
dotnet run
```

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
    │   └── ProjectEntry.cs              … 1プロジェクト分のデータ
    ├── Services/
    │   ├── ProjectScanner.cs            … フォルダ再帰スキャン・プロジェクト検出
    │   ├── CatalogStore.cs              … JSONへの保存/読み込み
    │   ├── FolderOpener.cs              … エクスプローラー/VSCode/ターミナルを開く
    │   ├── BuildRunner.cs               … ビルドコマンドの生成・非同期実行
    │   ├── ProjectRunner.cs             … 実行コマンドの生成
    │   ├── BuildChecker.cs              … ビルド済みかどうかの簡易チェック
    │   └── ToolChecker.cs               … 必要なツールがPATHにあるか確認
    ├── ViewModels/
    │   ├── MainWindowViewModel.cs
    │   └── ProjectEntryViewModel.cs
    └── Views/
        ├── MainWindow.axaml / .axaml.cs
        ├── CloneDialog.axaml / .axaml.cs … GitHubクローンダイアログ
        └── RunDialog.axaml / .axaml.cs   … 詳細実行ダイアログ
```

## 使い方

1. 「プログラム追加」で、ソースコードが散らばっている親フォルダを登録する
2. 登録フォルダ配下が自動で探索されてプロジェクト一覧に追加される
3. 一覧からプロジェクトを選び、詳細画面でタグを設定
4. 検索ボックスで名前・タグ・種別を横断検索
5. 詳細画面で Debug / Release 構成を切り替え、「ビルド」「実行」ボタンでそのまま操作できる
6. ビルド出力は詳細画面の下部にリアルタイム表示される
7. 「フォルダを開く」「VS Code」「ターミナル」ボタンでそのプロジェクトにすぐアクセス
8. 「GitHubからクローン」でリポジトリURLを指定してクローン＆自動登録できる

## 対応プロジェクト種別

| マーカーファイル | 種別ラベル |
|---|---|
| `*.sln` | Visual Studio Solution |
| `*.csproj` | .NET |
| `package.json` | Node.js |
| `pyproject.toml` / `requirements.txt` | Python |
| `pom.xml` | Java (Maven) |
| `build.gradle` | Java/Kotlin (Gradle) |
| `Cargo.toml` | Rust |
| `go.mod` | Go |

## カスタマイズしたい点があれば

- 検出するマーカー(言語の種類)を増やす: `Services/ProjectScanner.cs` の `Markers` 配列に追加
- スキャンを除外するフォルダ名を増やす: 同ファイルの `SkipDirNames` 配列に追加
