# 06. Remediation Specification

## 6.1 安全な修復ライフサイクル

すべての修復は次の状態遷移に従います。

```text
Diagnosed -> Previewed -> Confirmed -> BackedUp -> Executed -> Verified
                                         |           |
                                         |           -> Failed -> RollbackAvailable
                                         -> Cancelled
```

実行コマンドがexit 0でも、対応チェックの再診断がPassにならない限りVerifiedにしません。

## 6.2 共通プレビュー項目

- 修復ID
- 対象となる問題
- 変更対象
- 変更前の安全な要約
- 変更後の安全な要約
- 必要権限
- 新しいターミナル／Claude再起動の要否
- バックアップ先
- ロールバック可否
- 想定される企業ポリシー上の制約

## 6.3 REM-PATH-CLAUDE-LOCALBIN

### 目的

ネイティブClaude Codeが `%USERPROFILE%\.local\bin\claude.exe` に存在するが、User PATHにディレクトリがない場合に追加する。

### 前提条件

- 対象ファイルが存在する。
- OSがWindows 64-bit。
- User PATHを読み書きできる。
- 同等の正規化済みエントリが存在しない。

### 変更

- Target: User environment variable `PATH`
- Add: expanded `%USERPROFILE%\.local\bin`
- Do not modify: Machine PATH

### アルゴリズム

1. User PATHを取得する。nullは空文字として扱う。
2. `;` で分割し、空要素を除去する。
3. 引用符、末尾 `\`、環境変数展開を比較用に正規化する。
4. 重複がなければ末尾へ追加する。
5. 変更前の値をアプリのローカルバックアップへ暗号化せず保存するが、レポートには出さない。
6. User PATHを書き込む。
7. `WM_SETTINGCHANGE` を送る。
8. 新しいプロセス環境を構築し、`claude --version` を再実行する。
9. 既存ターミナルの再起動を案内する。

### ロールバック

バックアップしたUser PATHを復元し、再度 `WM_SETTINGCHANGE` を送る。

### 禁止

- Machine PATHへの追加
- 既存エントリの並べ替え
- 関係のない重複エントリ削除
- PATH全体をログ／レポートへ出力

## 6.4 REM-CLAUDE-GITBASH-SETTING

### 目的

Git Bashが存在するがClaude Codeが見つけられない場合、Claudeユーザー設定へパスを登録する。

### Target

`%USERPROFILE%\.claude\settings.json`

### 変更内容

```json
{
  "env": {
    "CLAUDE_CODE_GIT_BASH_PATH": "C:\\Program Files\\Git\\bin\\bash.exe"
  }
}
```

### 前提条件

- `bash.exe` が絶対パスに存在する。
- `bash.exe --version` が成功する。
- 対象がローカル固定ディスク上の実行ファイルである。
- settings.jsonが存在する場合、JSONとして正しい。

### マージ規則

- 既存のトップレベルキーを保持する。
- 既存 `env` オブジェクトを保持する。
- 同キーだけを追加／更新する。
- 既存値が異なる場合、変更前後をプレビューする。
- コメント付きJSONは標準JSONとして扱えないため、自動変更せず案内する。

### バックアップ

`settings.json.setup-doctor-backup-YYYYMMDD-HHMMSS`

### 検証

- 保存後にJSON再パース
- 値が一致
- Claude Codeを再起動後、Git Bashチェックを再実行

### 禁止

- Project settings、Local settings、Managed settingsの変更
- Gitインストールフォルダ全体のPATH追加を既定にすること
- EDRブロックの回避

## 6.5 REM-PATH-POWERSHELL

### 目的

Windows PowerShellが既定場所に存在するがPATHにない場合、User PATHへ既定ディレクトリを追加する。

Target:

`C:\Windows\System32\WindowsPowerShell\v1.0\`

### 条件

- 実ファイルが存在する。
- Machine PATHにない、User PATHにもない。
- PowerShellの直接起動が成功する。

Claude PATH修復と同じ安全アルゴリズムを使用します。

## 6.6 REM-AUTH-LOGIN

### 目的

未認証利用者に公式ログインフローを開始させる。

### 実行

- 可視ターミナルを開く。
- 選択済みの正しい `claude.exe` に `auth login` を渡す。
- 必要に応じてユーザーがSSO等を選ぶ。
- アプリはstdoutの認証内容を保存しない。

### 完了判定

利用者がアプリへ戻って「ログイン後に再診断」を押し、`claude auth status` がexit 0になった場合のみ完了。

## 6.7 REM-INSTALL-CLAUDE-WINGET

### 目的

Claude Code未導入かつWinGetが利用可能な場合、公式パッケージのインストールを開始する。

### Command

Executable:

```text
winget.exe
```

Arguments:

```text
install --id Anthropic.ClaudeCode -e --accept-package-agreements --accept-source-agreements
```

実装では引数配列を使用し、単一シェル文字列にしません。

### 条件

- `winget --version` が成功。
- 利用者がパッケージIDと実行内容を確認。
- インターネット利用を明示。

### 注意

- 企業ポリシーでWinGetが無効な場合、自動回避しない。
- 管理者権限の要求有無はWinGet／パッケージ側に委ね、アプリは自動昇格しない。
- `irm ... | iex` をアプリから自動実行しない。

### 検証

インストール終了後、候補探索と `claude --version` を再実行する。

## 6.8 REM-GUIDE-DESKTOP-UPDATE

### 目的

古いClaude DesktopのWindowsAppsエイリアスがCLIより優先されている可能性がある場合に、安全な案内を表示する。

### 動作

- 自動でWindowsAppsを編集しない。
- App Execution Aliasを無効化しない。
- Claude Desktopの更新を案内する。
- ネイティブCLIパスが存在する場合、その場所をマスク表示する。
- 更新後の再診断を促す。

## 6.9 IT対応へ送る条件

次の場合は修復ボタンを出さず、ITActionへ移行します。

- Access denied／AppLocker／Software Restriction Policyの兆候
- EDRが子プロセス起動を遮断
- TLS inspectionによる証明書エラー
- 企業プロキシが必要だが設定不明
- Managed settingsの制約
- Machine PATHやHKLMの変更が必要
- PowerShellとGit Bashの双方が管理ポリシーで利用不可

## 6.10 部分失敗

複数修復を選択した場合でも、各アクションを独立トランザクションとして扱います。

- A成功、B失敗の場合、Aを自動で戻さない。
- 画面に成功／失敗／未実行を分けて表示する。
- ロールバック可能な項目には個別「元に戻す」を出す。
- 全体再診断を行い、現状を事実として表示する。
