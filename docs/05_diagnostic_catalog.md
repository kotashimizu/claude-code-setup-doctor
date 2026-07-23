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
