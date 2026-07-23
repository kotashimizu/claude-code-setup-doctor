# 09. Implementation Roadmap

## Phase 0: Repository bootstrap

### Deliverables

- .NET 10 solution and projects
- Dependency direction enforcement
- CI build/test workflow
- Formatting and analyzers
- Core enums and result models
- Fake clock, fake command runner
- `docs/implementation_notes/` templates

### Exit criteria

- Clean cloneから`dotnet build`と`dotnet test`が成功
- WPF shell appが起動
- CoreがWPF／Windows assemblyに依存しない

## Phase 1: Read-only diagnostics core

### Deliverables

- OS, architecture, memory checks
- PowerShell / pwsh discovery
- Claude candidate discovery
- User PATH parsing
- Claude version command
- Git and Git Bash discovery
- Readiness aggregator
- JSON diagnostic session model

### Exit criteria

- Fake environmentで主要分岐を単体テスト
- 実環境を変更せずコンソール／test harnessで結果を確認

## Phase 2: WPF diagnostic UI

### Deliverables

- Start, scanning, results screens
- Progress and cancellation
- Status cards and details
- Localization resources
- Accessibility properties
- Mock-aligned visual design

### Exit criteria

- 基本診断がUIから完了
- Git absent + PowerShell presentが正しく推奨表示
- 200%表示で主要操作が欠けない

## Phase 3: Safe remediation

### Deliverables

- Remediation plan generation
- Review/confirmation screen
- Claude User PATH repair
- PowerShell User PATH repair
- Claude Git Bash setting repair
- Backup and rollback
- Post-remediation verification

### Exit criteria

- 各修復のunit/integration/rollback testが通る
- 未確認状態で変更が起きない
- Partial failureが正しく表示される

## Phase 4: Authentication, WinGet, reports

### Deliverables

- `claude auth status`
- visible `claude auth login` launcher
- WinGet detection and install action
- JSON/text report writers
- redaction engine
- copy/save UX

### Exit criteria

- 認証情報を保存しない
- report schema validationが通る
- secret leakage testsが通る

## Phase 5: Optional network and doctor diagnostics

### Deliverables

- user-initiated network probe
- host-by-host classification
- `claude doctor` with timeout/cancel
- IT handoff messaging

### Exit criteria

- offline basic scan unaffected
- TLS validation remains enabled
- network failure does not crash overall scan

## Phase 6: Packaging and pilot release

### Deliverables

- self-contained win-x64 single-file publish
- version metadata
- license, notices, unofficial disclaimer
- SHA-256 checksums
- release checklist
- pilot feedback form template

### Exit criteria

- clean supported VM without .NET runtimeで起動
- standard userで診断とUser領域修復が完了
- SmartScreen／EDRの挙動を既知制限へ記載

## Backlog after MVP

- win-arm64 publish
- signed MSIX/MSI
- enterprise silent diagnostic mode
- managed settings read-only summary
- English localization
- policy-configurable diagnostic catalog
- automatic app update mechanism
- support bundle with screenshot and event logs（opt-in）

## 推奨Issue分割

- `CORE-001` result models and readiness aggregation
- `CORE-002` path normalization
- `WIN-001` process runner
- `WIN-002` executable discovery
- `DIAG-001` system checks
- `DIAG-002` shell checks
- `DIAG-003` Claude checks
- `DIAG-004` auth checks
- `UI-001` shell/navigation
- `UI-002` results list
- `REM-001` remediation preview
- `REM-002` User PATH update
- `REM-003` settings JSON merge
- `REPORT-001` schema and redaction
- `NET-001` optional probes
- `REL-001` publish and checksums
