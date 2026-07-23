# CLAUDE.md

## Project role

You are implementing a Windows-only diagnostic and remediation desktop application named **Claude Code Setup Doctor**. It is an unofficial companion utility, not an Anthropic product and not a replacement UI for Claude Code.

All user-facing copy is Japanese. Code identifiers, comments, test names, and architecture documents may be English unless the existing context is Japanese.

## Source-of-truth order

When requirements conflict, use this priority:

1. `docs/07_security_privacy_enterprise.md`
2. `docs/06_remediation_spec.md`
3. `docs/05_diagnostic_catalog.md`
4. `docs/02_functional_requirements.md`
5. `docs/03_ui_ux_spec.md`
6. `docs/04_technical_architecture.md`
7. `docs/01_product_requirements.md`
8. Mock image

Do not silently resolve a material conflict. Record it in `docs/implementation_notes/open_questions.md`, choose the safer behavior, and continue with an explicit assumption.

## Non-negotiable product facts

- Git for Windows is optional on native Windows. Git absence is not a fatal error when PowerShell is available.
- Node.js is not a native-install prerequisite and must not be shown as a required MVP check.
- The native Windows Claude Code path to inspect is `%USERPROFILE%\.local\bin\claude.exe`.
- A missing `%USERPROFILE%\.local\bin` User PATH entry is a repairable condition.
- Git Bash integration should prefer the Claude setting `CLAUDE_CODE_GIT_BASH_PATH` over broad PATH modification.
- `claude auth status` is the preferred machine-readable authentication check.
- `claude doctor` is supplementary; do not parse its prose as the sole readiness decision.
- Enterprise policy, proxy, certificate, AppLocker, EDR, GPO, and managed settings changes are out of scope for automatic remediation.

## Target architecture

Create this solution layout unless a documented technical blocker is found:

```text
src/
  SetupDoctor.App/                    # WPF, views, view models, composition root
  SetupDoctor.Core/                   # domain models, checks, remediation contracts
  SetupDoctor.Infrastructure.Windows/ # process, PATH, files, registry read-only, OS APIs
  SetupDoctor.Reporting/              # JSON and text report writers

tests/
  SetupDoctor.Core.Tests/
  SetupDoctor.Infrastructure.Windows.Tests/
  SetupDoctor.IntegrationTests/

docs/
```

Dependencies must point inward. `Core` must not reference WPF or Windows-specific assemblies.

## Required abstractions

At minimum, implement interfaces equivalent to:

- `IDiagnosticCheck`
- `IDiagnosticOrchestrator`
- `IRemediationAction`
- `IRemediationOrchestrator`
- `ICommandRunner`
- `IPathEnvironmentService`
- `IClaudeSettingsService`
- `IFileBackupService`
- `INetworkProbe`
- `IReportWriter`
- `IClock`

The exact names may change, but the test boundaries must remain.

## Security rules

- Default to dry-run and read-only behavior.
- Every state-changing action requires an explicit user confirmation screen.
- Never use `cmd /c` or `powershell -Command` with interpolated user-controlled strings.
- Use `ProcessStartInfo.ArgumentList` or equivalent argument-safe APIs.
- Allow only hard-coded executable identities and arguments defined by the remediation catalog.
- Never store or display authentication tokens, API keys, cookies, complete environment variable values, or raw OAuth responses.
- Detect only the presence of sensitive variables such as `ANTHROPIC_API_KEY`; do not read them into reports.
- Back up a file before editing it and write atomically via a temporary file plus replace.
- Do not modify HKLM, machine PATH, GPO, AppLocker, EDR, certificate stores, proxy configuration, or managed Claude settings.
- Do not auto-run remote scripts such as `irm ... | iex`.
- Do not auto-uninstall duplicate installations in MVP.

## Process execution rules

- Every command has a timeout and cancellation token.
- Capture stdout and stderr separately.
- Kill only the process instance started by the app and its owned child process tree when a timeout occurs.
- Record executable path, argument names, exit code, duration, and redacted output summary.
- Do not log full PATH values by default.
- Treat localization and version-dependent output as unstable. Prefer exit codes and JSON outputs.

## UI rules

- Use the exact status vocabulary from `docs/03_ui_ux_spec.md`.
- Required, recommended, optional, and IT-managed items must be visually distinguishable.
- A green overall result is allowed only after required checks pass.
- Git absence with PowerShell available must be informational or recommended, not an error.
- A repair button must describe the concrete change; avoid a single opaque “Fix everything” action without a review step.
- The app must state “非公式ツール” in About and initial consent text.

## Testing rules

- Unit tests must not modify the real user PATH, registry, Claude settings, or installed applications.
- Use temporary directories and fake executables for discovery and process tests.
- Create tests for Japanese usernames, spaces in paths, duplicate PATH entries, case differences, missing files, locked files, malformed JSON, timeout, cancellation, and partial remediation failure.
- Add a rollback test for every remediation action.
- A remediation is complete only when the post-action diagnostic passes.

## Workflow

For each implementation phase:

1. Read the relevant specification documents.
2. Write or update `docs/implementation_notes/current_plan.md`.
3. Add failing tests for the phase.
4. Implement the smallest coherent change.
5. Run `dotnet build` and `dotnet test`.
6. Review security impact and update documentation.
7. Summarize changed files, test results, and remaining risks.

Do not push to a remote repository. Do not change product scope without documenting the decision.

## Definition of done

A phase is done only when:

- The solution builds without warnings introduced by the change.
- Relevant unit and integration tests pass.
- No real machine state was changed by automated tests.
- User-visible errors are actionable and in Japanese.
- Logs and reports are redacted.
- Documentation and acceptance criteria are updated.
