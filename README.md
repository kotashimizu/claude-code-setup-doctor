# Claude Code Setup Doctor - Implementation Document Pack

Windows端末でClaude Codeを利用できる状態か診断し、利用者の明示的な承認後に、安全な範囲だけ修復する非公式ラッパーアプリの実装資料です。

> **重要な前提修正**
>
> - 現行のClaude Codeでは、Git for Windowsは必須ではありません。Git Bashを使う場合に推奨され、未導入時はPowerShellが使用されます。
> - ネイティブ版Claude CodeではNode.jsは必須ではありません。Node.js確認は、npm版を明示的にサポートする場合だけ必要です。
> - PATH問題の主対象は、ネイティブインストール先 `%USERPROFILE%\.local\bin` がUser PATHにないケースです。
> - Gitが見つからない場合、一般PATHを書き換えるより、Claude Codeの `CLAUDE_CODE_GIT_BASH_PATH` を設定する方が影響範囲を限定できます。

このパックは2026-07-22時点のAnthropicおよびMicrosoft公式資料を基準にしています。変更され得るコマンド、既定パス、対応OSは `docs/11_source_references.md` で再確認してください。

## 最初に読む順番

1. `START_HERE.md`
2. `CLAUDE.md`
3. `docs/00_project_overview.md`
4. `docs/01_product_requirements.md`
5. `docs/02_functional_requirements.md`
6. `docs/04_technical_architecture.md`
7. `docs/05_diagnostic_catalog.md`
8. `docs/06_remediation_spec.md`
9. `docs/07_security_privacy_enterprise.md`
10. `docs/08_test_plan.md`
11. `docs/09_implementation_roadmap.md`

## 推奨する開始方法

このフォルダを空のGitリポジトリに配置し、リポジトリ直下でClaude Codeを起動します。

```powershell
claude
```

最初の指示には `prompts/01_bootstrap_repository.md` の内容を使用します。実装フェーズごとに `prompts/` 内の指示を順番に渡してください。

## 想定成果物

- Windows 10 1809以降／Windows 11向けWPFデスクトップアプリ
- .NET 10 LTS、C#、MVVM
- 管理者権限なしで起動できる自己完結型配布物
- オフライン診断、任意のネットワーク診断、承認制の修復
- JSONおよび人間向けテキストの診断レポート
- 単体テスト、統合テスト、Windows実機／VM検証手順

## MVPで扱う項目

- OS、CPUアーキテクチャ、メモリ
- PowerShell／Git Bashの利用可否
- Claude Code CLIの検出、バージョン、PATH、競合インストール
- ログイン状態
- Git for Windowsの有無（推奨項目として表示）
- Claude Codeのネットワーク到達性（利用者が実行した場合のみ）
- 安全な修復候補の提示、実行、再診断、ロールバック情報の保存

## MVPの対象外

- Claudeアカウントや契約の購入・付与
- 企業のプロキシ、証明書、EDR、AppLocker、GPOの自動変更
- 管理者ポリシー、レジストリポリシー、`C:\Program Files\ClaudeCode\managed-settings.json` の変更
- 任意コマンド実行機能
- Gitリポジトリの作成、コード編集、Claude Codeセッション管理
- テレメトリ送信

## モックについて

`docs/assets/setup-doctor-mockup.png` は画面構成の参考です。画像内の「Node.js」はMVPでは「シェル」に置き換え、Gitは必須ではなく推奨項目として表示します。最終的な文言と状態定義は `docs/03_ui_ux_spec.md` を正とします。
