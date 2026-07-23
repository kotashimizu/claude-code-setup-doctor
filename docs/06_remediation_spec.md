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

## 6.11 Cowork修復（Claude Desktop、Windows専用、非公式情報に基づく）

> 対象のCHK-COWORK-*はdocs/05_diagnostic_catalog.md §5.2.1参照。サービス名・パスは非公式情報のため、実装は固定値依存を避けフォールバック設計とする。

### REM-COWORK-START-SERVICE

#### 目的

`CoworkVMService`が停止している場合に起動する。

#### 前提条件

- サービスが存在する（`Get-Service`で確認可能）。
- StartTypeがDisabledでない。

#### 実行

- サービス起動はWindowsの既定権限モデル上、通常ユーザーには許可されないため、単一コマンドのみをUAC昇格（`runas`）付きで実行する。
- アプリ本体は非昇格のまま起動を維持し、この操作単体にのみ昇格を求める。
- 昇格プロンプトはOS標準のものを使用し、アプリが認証情報を扱わない。

#### 検証

- 昇格プロセスの終了コードを確認後、`CHK-COWORK-001`を再実行しStatus=Runningになったか確認する。

#### ロールバック

- サービス停止（`Stop-Service`、同様に昇格が必要）。ただし通常運用では停止せず放置してよいため、明示的なロールバックボタンは提供しない。

#### 禁止

- StartTypeの変更（Disabled⇔Auto等）。ユーザーの意図的な設定を上書きする恐れがあるため行わない。

### REM-COWORK-DECOMPRESS-VHDX

#### 目的

VMバンドルフォルダにNTFS圧縮属性が付与されている場合に解除する（Claude Desktop v1.24012.0で修正済みの既知バグへの対処。旧バージョン利用者向け）。

#### Target

`%LOCALAPPDATA%\Packages\Claude_*\LocalCache\Roaming\Claude\vm_bundles\`（MSIX版）または`%APPDATA%\Claude\vm_bundles\`（非MSIX版）

#### 実行

1. `CoworkVMService`を停止（昇格必要）。
2. `compact /u /s:"<vm_bundlesフォルダ>"`を実行し圧縮属性を解除（昇格必要）。
3. サービスを再起動（昇格必要）。
4. 上記3ステップは1回のUAC昇格プロンプトでまとめて実行する（昇格済みの単一ヘルパープロセス内で連続実行し、ユーザーへの昇格要求を1回に抑える）。

#### 検証

- フォルダのCompressed属性が解除されたことを確認。
- `CoworkVMService`がRunning状態に戻ったことを確認。

#### 禁止

- vm_bundles以外のフォルダへの圧縮属性操作。
- Windows全体のNTFS圧縮設定変更。

### REM-COWORK-ENABLE-VMP（ガイダンスのみ、自動実行しない）

#### 目的

Virtual Machine Platformが無効な場合の有効化を案内する。

#### 動作

- 自動で機能を有効化しない。再起動が必須かつマシン全体の仮想化スタックに影響するため、既存プロジェクトの「Machine全体への変更は慎重に扱う」方針に準じ、コピー可能なコマンドをそのまま提示するだけに留める。
- 提示コマンド例:
  ```powershell
  Enable-WindowsOptionalFeature -Online -FeatureName VirtualMachinePlatform -All -NoRestart
  bcdedit /set hypervisorlaunchtype auto
  ```
- Windows Home Editionの場合、機能自体は有効化できても動作が不安定という矛盾する報告がある旨を併記する。
- 実行後は手動での再起動と再診断を促す。

### REM-COWORK-REPAIR-HCS（ガイダンスのみ、自動実行しない）

#### 目的

hns/vmcompute/vfpextが欠落している場合のOS修復手順を案内する。

#### 動作

- DISM・SFCはOS本体への深い修復であり、既存プロジェクトの「OS本体の修復・企業ポリシー領域は自動修復の対象外」という方針に該当するため自動実行しない。
- 提示コマンド例:
  ```powershell
  DISM /Online /Cleanup-Image /RestoreHealth
  sfc /scannow
  ```
  実行後、Virtual Machine Platformの無効化→再有効化と再起動を案内する。
- 上記を実施しても解消しない場合は、Windows Update側の不整合の可能性がある旨を明記し、それ以上の自動修復は行わない。

## 6.12 Coworkネットワーク競合・組織無効化（自動修復なし）

- CHK-COWORK-005（ネットワークサブネット競合）: NAT設定・仮想スイッチの直接操作は行わない。競合している可能性のあるプロセス（VPNクライアント、Docker Desktop等）の名称のみ提示し、「一時停止して再試行」という手動対応を案内する。
- CHK-COWORK-006（組織による無効化の間接シグナル）: ローカル診断からは判定不能なため常にInformational。「組織管理者にOrganization settings > Capabilities > Coworkの設定をご確認ください」という案内のみ。

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
