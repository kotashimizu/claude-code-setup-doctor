# 10. Release and Distribution

## 10.1 Build configuration

- Target framework: `net10.0-windows`
- Runtime identifiers: `win-x64`, later `win-arm64`
- Configuration: Release
- Self-contained: true
- PublishSingleFile: true
- Trimming: disabled for MVP unless full WPF validation is complete
- ReadyToRun: optional after measurement
- Debug symbols: embedded or separately retained for support

Example:

```powershell
dotnet restore
dotnet test -c Release
dotnet publish src/SetupDoctor.App/SetupDoctor.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:DebugType=embedded
```

## 10.2 Versioning

Use Semantic Versioning:

- MAJOR: report schema, remediation behavior, or compatibility breaking changes
- MINOR: new diagnostic／remediation features
- PATCH: fixes and wording changes

Include in the report:

- app version
- report schema version
- diagnostic catalog version
- build commit hash when available

## 10.3 Release artifacts

- `ClaudeCodeSetupDoctor-win-x64.exe`
- `ClaudeCodeSetupDoctor-win-x64.sha256`
- `README-ja.md`
- `LICENSE`
- `THIRD-PARTY-NOTICES.txt`
- optional `SBOM.spdx.json`

Do not bundle Claude Code, Git, PowerShell, or Anthropic credentials.

## 10.4 Code signing

Pilot builds may be unsigned with an explicit warning. External or enterprise distribution should use an Authenticode code-signing certificate.

Release checklist:

- Publisher name matches project owner
- Timestamped signature
- Signature verification on clean Windows VM
- Hash generated after signing
- Malware scan
- SmartScreen behavior documented

## 10.5 Distribution choices

### Portable EXE / ZIP

Pros:

- simplest pilot
- no installation required
- easy rollback

Cons:

- unsigned warnings
- no managed uninstall
- update distribution is manual

### MSIX

Pros:

- enterprise deployment
- clean install/uninstall
- signing and update mechanisms

Cons:

- packaging complexity
- some enterprise environments restrict sideloading

### MSI

Pros:

- conventional enterprise tooling

Cons:

- additional installer toolchain and maintenance

MVPはportableを採用し、企業展開が確定した段階でMSIX／MSIを選定します。

## 10.6 Support bundle

利用者が明示的に保存した場合だけ、次を含むZIPを生成できます。

- sanitized JSON report
- sanitized text report
- app version
- check IDs and result codes
- local log excerpt after redaction

含めないもの:

- settings.json原本
- PATH全文
- OAuth／APIキー
- email／user name
- source code
- arbitrary event logs

## 10.7 Release validation

- .NET runtime未導入VMで起動
- standard userで起動
- offline基本診断
- PATH修復とrollback
- settings JSON修復とrollback
- Japanese path
- WindowsApps conflict case
- network timeout
- EDR-like access denied
- report redaction

## 10.8 Deprecation policy

公式Claude Codeのコマンドや既定パスが変わった場合:

1. `docs/11_source_references.md` を更新
2. diagnostic catalog versionを上げる
3. compatibility testsを追加
4. 旧挙動をUnknown／manual guidanceへ安全にフォールバック
5. 自動修復は公式仕様を再確認するまで無効化可能にする
