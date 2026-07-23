# 05. Diagnostic Catalog

## 5.1 判定原則

- RequiredのFailはReadyを阻止する。
- RecommendedのFailはWarningへ変換できる。
- Optionalは利用者が実行した場合だけ全体情報へ追加する。
- ITManagedは自動修復せず、ITActionとして扱う。
- UnknownをPassへ変換しない。
- 公式出力がJSONまたはexit codeを提供する場合、表示文言の解析より優先する。

## 5.2 チェック一覧

### CHK-OS-001: Windows version

- Requirement: Required
- Logic: OSがWindowsか、バージョン／buildを取得
- Pass: Windows 10 build 17763以降またはWindows Server 2019以降
- Fail: 要件未満
- Safe metadata: product family, build, edition category
- Remediation: なし
- Overall: Unsupported

### CHK-ARCH-001: 64-bit OS and supported architecture

- Requirement: Required
- Logic: `Environment.Is64BitOperatingSystem`、OSArchitecture
- Pass: x64またはArm64、64-bit
- Fail: x86／32-bit
- Note: x86 PowerShellから実行していてもOSが64-bitなら、x64アプリ自体は通常起動しないため配布物の取り違えとして扱う
- Remediation: 正しい配布物を案内

### CHK-MEM-001: Installed memory

- Requirement: Required
- Logic: Windows APIまたはCIMから物理メモリを取得
- Pass: 4 GB以上
- Fail: 4 GB未満
- Unknown: API取得不能
- Remediation: なし

### CHK-SHELL-001: Windows PowerShell

- Requirement: Required conditional
- Discovery:
  - `GetSystemDirectory()` + `WindowsPowerShell\v1.0\powershell.exe`
  - PATH上の `powershell.exe`
- Pass: 実行ファイルが存在し、`-NoProfile -Command "$PSVersionTable.PSVersion.ToString()"` が成功
- Repairable: 既定場所に存在するがPATHにない
- ITAction: 実行がポリシーでブロック
- Remediation: REM-PATH-POWERSHELL

### CHK-SHELL-002: PowerShell 7

- Requirement: Recommended
- Discovery: PATH上の `pwsh.exe`
- Pass: バージョン取得成功
- Warning: 未導入。Windows PowerShellがあれば問題なし
- Remediation: MVPではインストールしない

### CHK-CLAUDE-001: Claude candidate discovery

- Requirement: Required
- Inputs:
  - `where.exe claude`
  - current PATH scan
  - `%USERPROFILE%\.local\bin\claude.exe`
- Pass: 1件以上の候補
- Repairable: 既定パスに存在するがPATH候補なし
- Fail: 候補なし
- Remediation: REM-PATH-CLAUDE-LOCALBINまたはREM-INSTALL-CLAUDE-WINGET

### CHK-CLAUDE-002: Claude version execution

- Requirement: Required
- Command: selected candidate + `--version`
- Timeout: 5 seconds
- Pass: exit 0、非空バージョン文字列
- Fail: 実行エラー
- Unknown: timeout／unexpected output
- Safe metadata: semantic-looking version text, executable masked path, exit code
- Remediation: install/update guidance

### CHK-CLAUDE-003: WindowsApps / Desktop alias conflict

- Requirement: Required when conflict detected
- Detection:
  - first PATH candidate is under `%LOCALAPPDATA%\Microsoft\WindowsApps`
  - `--version` starts a long-running GUI-like process or returns no CLI output
  - native CLI exists later in PATH
- Status: UserAction or ITAction, not auto-repaired
- Message: Claude Desktopを最新化し、再診断
- Remediation: REM-GUIDE-DESKTOP-UPDATE

### CHK-CLAUDE-004: Multiple installations

- Requirement: Recommended
- Pass: 1 candidate
- Warning: 2+ candidates
- Metadata: masked candidate list and priority
- Remediation: none in MVP; do not auto-uninstall

### CHK-PATH-001: Claude native directory in User PATH

- Requirement: Required when native binary exists
- Target: `%USERPROFILE%\.local\bin`
- Pass: normalized User PATH contains target
- Repairable: missing
- Warning: current process PATH missing but User PATH contains it; requires new terminal
- Remediation: REM-PATH-CLAUDE-LOCALBIN

### CHK-AUTH-001: Authentication status

- Requirement: Required for readiness
- Command: `claude auth status`
- Timeout: 10 seconds
- Parse: JSON when available; exit code is authoritative
- Pass: exit 0
- UserAction: exit 1
- Unknown: other exit code, parse failure, timeout
- Privacy: do not persist email, account ID, tokens, or raw JSON
- Remediation: REM-AUTH-LOGIN

### CHK-GIT-001: Git command

- Requirement: Recommended
- Command: `git --version`
- Pass: exit 0
- Warning: absent while PowerShell exists
- Fail: absent only when PowerShell is also unavailable and no Git Bash exists
- Remediation: install guidance only in MVP

### CHK-GITBASH-001: Git Bash executable

- Requirement: Recommended
- Discovery:
  - configured `CLAUDE_CODE_GIT_BASH_PATH`
  - derive from `where.exe git`
  - `C:\Program Files\Git\bin\bash.exe`
- Pass: bash file exists and `--version` succeeds
- Repairable: bash exists but Claude setting absent／invalid
- Warning: absent while PowerShell available
- Remediation: REM-CLAUDE-GITBASH-SETTING

### CHK-ENV-001: Conflicting ANTHROPIC_API_KEY presence

- Requirement: Optional / diagnostic clue
- Logic: presence only, never value
- Warning: key exists and auth behavior suggests subscription OAuth mismatch
- Message: 古いAPIキーが認証方式を上書きする可能性
- Remediation: manual guidance only; do not delete environment variables

### CHK-WINGET-001: WinGet availability

- Requirement: Optional
- Command: `winget --version`
- Pass: command available
- Warning: unavailable; installation action disabled
- Remediation: Microsoft App Installer guidance only

### CHK-NET-001: Download host

- Requirement: Optional, user initiated
- Host: `downloads.claude.ai`
- Probe: HTTPS HEAD/GET with short timeout; do not download binary
- Pass: valid HTTPS response including 2xx/3xx
- ITAction: DNS, timeout, TLS trust, proxy rejection
- Note: 403 may be regional or policy-related; do not infer one cause as fact

### CHK-NET-002: API host

- Requirement: Optional, user initiated
- Host: `api.anthropic.com`
- Probe: TLS/HTTP connectivity; authentication response is acceptable evidence of reachability
- Pass: network and TLS connection established
- ITAction: DNS, timeout, TLS, proxy failure

### CHK-NET-003: Authentication hosts

- Requirement: Optional, user initiated
- Hosts: `claude.ai`, `claude.com`, `platform.claude.com`
- Pass: connection established
- ITAction: blocked or certificate error

### CHK-DOCTOR-001: Claude doctor

- Requirement: Optional
- Command: `claude doctor`
- Timeout: 30 seconds
- Result: attach output summary and exit code
- Rule: never make this the only readiness check; output text is version-dependent
- Privacy: redact paths, emails, token-like strings

### CHK-SECURITY-001: Endpoint security interference clue

- Requirement: ITManaged
- Trigger:
  - executable exists and can be read
  - direct launch fails with access denied or child process blocked
  - Git Bash path is correct but Claude cannot spawn it
- Status: ITAction
- Remediation: none
- Report: executable names, error codes, timestamps; no bypass instructions

## 5.2.1 Cowork診断（Claude Desktop、Windows専用、対象外扱い）

> **出典に関する注記**: 以下のCHK-COWORK-*は、Anthropic公式ドキュメントに加え、GitHub issue・第三者技術ブログから収集した非公式情報に基づく（2026-07-23調査時点）。サービス名・レジストリキー・ファイルパスはAnthropicの実装変更で変わる可能性があるため、判定ロジックは固定文字列への依存を避け、存在確認・ワイルドカード検索でのフォールバックを優先する。全体判定アルゴリズム（5.3節）には含めない独立カテゴリとして扱う（Claude Code CLIの準備状態とは無関係のため）。GitHub issue番号は2026年上半期だけで#37000台→#78000台へ大幅に入れ替わっており、issue自体を判定根拠にしない。

### CHK-COWORK-001: CoworkVMServiceの稼働状態

- Requirement: Optional（Cowork利用者にとっては実質Recommended）
- Logic: `Get-Service`でサービス名`CoworkVMService`（または`*Cowork*VMService*`ワイルドカード）のStatus/StartTypeを確認。読み取り専用。
- Pass: Status = Running
- Repairable: Status = Stopped かつ Disabledでない
- Fail: 起動試行後もRunningにならない
- Unknown: サービス自体が存在しない（Claude Desktop未インストール、またはCowork非搭載バージョン）
- Remediation: REM-COWORK-START-SERVICE
- 出典: 第三者ブログ（Bryce Watson診断ガイド）、GitHub issue #57371, #57221（信頼度: 中）

### CHK-COWORK-002: Virtual Machine Platform / Hyper-V 有効化とEdition整合性

- Requirement: Optional
- Logic: レジストリ`EditionID`とビルド番号（≥22000でWindows 11相当と独自判定。Anthropic公式readiness checkerの「Windows 11をWindows 10と誤判定する」既知バグを踏まえ、公式ツールの判定を信用しない）。`Get-WindowsOptionalFeature -FeatureName VirtualMachinePlatform`のState確認。
- Pass: VirtualMachinePlatform = Enabled
- Repairable: Disabled（管理者権限があれば機能有効化可能）
- Fail: BIOS/UEFIレベルで仮想化無効
- Unknown: Windows Home Editionは機能が有効でも動作が不安定という矛盾する報告が複数あるため、楽観的にPass判定しない
- Remediation: REM-COWORK-ENABLE-VMP（ガイダンスのみ、自動実行しない。再起動必須のため）
- 出典: Anthropic公式ヘルプ（Deploy Claude Desktop for Windows）、GitHub issue #31991, #50621ほか（信頼度: 公式部分は高、Home Edition挙動は中）

### CHK-COWORK-003: HCS関連コンポーネント（hns / vmcompute / vfpext）

- Requirement: Optional
- Logic: `Get-Service hns`, `Get-Service vmcompute`の存在確認。vfpextはドライバのため`sc query vfpext`で登録確認。
- Pass: 3つとも登録済み
- Repairable: VirtualMachinePlatform有効なのに欠落（DISM/SFCで復旧を試みる価値あり）
- Fail: 修復試行後も解消しない（ビルド不整合の疑い、Windows Update側の問題である可能性）
- Remediation: REM-COWORK-REPAIR-HCS（ガイダンスのみ、自動実行しない。OS深部の修復のため）
- 出典: Anthropic公式ヘルプ（HCSエラー文言 "Missing HCS services: HNS, vmcompute, vfpext" を確認）、GitHub issue #77277, #78858, #78866（信頼度: エラー文言は公式で高、原因分析は中）

### CHK-COWORK-004: Cowork VM仮想ディスク（vhdx）のNTFS圧縮属性

- Requirement: Optional
- Logic: VMバンドルフォルダ（`%LOCALAPPDATA%\Packages\Claude_*\LocalCache\Roaming\Claude\vm_bundles\`または`%APPDATA%\Claude\vm_bundles\`）とその配下のCompressed属性を確認。
- Pass: Compressed属性なし
- Repairable: 属性あり
- Unknown: vm_bundlesフォルダが見つからない（Cowork未初期化）
- Remediation: REM-COWORK-DECOMPRESS-VHDX
- 備考: Claude Desktop v1.24012.0（2026-07-21）で公式修正済みの既知バグ。旧バージョン利用者・圧縮フォルダがキャッシュされたままの環境向け。
- 出典: Anthropic公式changelog（v1.24012.0修正内容と一致）、GitHub issue #39010（信頼度: 高）

### CHK-COWORK-005: Coworkネットワーク（172.16.0.0/24）のサブネット競合

- Requirement: Optional, user initiated
- Logic: ネットワークアダプタ一覧から172.16.0.0/24・172.17.0.0/24の重複、Docker Desktop・VPNアダプタの有無を確認。`Get-HnsNetwork`等Hyper-V管理コマンドレット依存の確認は行わない（Home環境等で利用不可の場合があるため）。
- Pass: 重複なし
- ITAction: 重複あり、またはネットワーク競合の兆候を検出
- Remediation: なし（NAT設定の直接操作は対象外。競合プロセス名を提示し手動対応を案内するのみ）
- 出典: GitHub issue #28516、第三者ブログ（Elliot Segler）（信頼度: 中）

### CHK-COWORK-006: 組織（Team/Enterprise）によるCowork無効化の間接シグナル

- Requirement: Optional
- Logic: CHK-COWORK-001〜005が全てPassにもかかわらずCoworkが機能しない場合の消去法的ヒント。ローカルWindows診断からは組織設定を直接確認する手段がないため常にUnknown。
- Status: 常にUnknown（Informational）
- Remediation: なし。「Organization settings > Capabilities > Cowork の設定を組織管理者にご確認ください」という案内文のみ。
- 出典: Anthropic公式ヘルプ（Use Claude Cowork on Team and Enterprise plans）。ただしエンドユーザー側の具体的なエラー文言は未確認（信頼度: 存在確認は高、UI文言は未確認）

## 5.3 全体判定アルゴリズム

Pseudo rules:

```text
if OS or architecture or memory required check fails:
    Unsupported
else if any ITManaged check is ITAction and blocks a required capability:
    ITActionRequired
else if any required check is Repairable:
    Repairable
else if any required check is UserAction:
    UserActionRequired
else if any required check is Fail or Unknown:
    Unknown or ITActionRequired based on evidence
else if all required checks pass and any recommended check warns:
    ReadyWithRecommendations
else:
    Ready
```

Git absence alone must never produce Repairable or Fail when PowerShell is usable.
