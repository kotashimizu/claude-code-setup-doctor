# 11. Official Source References

**Verified:** 2026-07-22  
**Policy:** 仕様が変わり得る値は、実装・リリース前に公式ページを再確認する。

## 11.1 Anthropic official sources

| Topic | Fact used | Source |
|---|---|---|
| System requirements | Windows 10 1809+ / Server 2019+, 4 GB+, x64 or ARM64, supported shells | https://code.claude.com/docs/en/setup |
| Windows Git behavior | Git for Windows is optional; PowerShell is used when Git Bash is absent | https://code.claude.com/docs/en/setup |
| Native install path and PATH repair | Windows native binary at `%USERPROFILE%\.local\bin\claude.exe`; add directory to User PATH when missing | https://code.claude.com/docs/en/troubleshoot-install |
| Git Bash explicit setting | `CLAUDE_CODE_GIT_BASH_PATH` in user settings | https://code.claude.com/docs/en/troubleshoot-install |
| Desktop alias conflict | Older Desktop can register WindowsApps `Claude.exe` ahead of CLI | https://code.claude.com/docs/en/troubleshoot-install |
| Authentication command | `claude auth status` returns JSON; exit 0 logged in, 1 not logged in | https://code.claude.com/docs/en/cli-reference |
| Diagnostic command | `claude doctor` is available as a setup check | https://code.claude.com/docs/en/troubleshooting |
| WinGet package | `winget install Anthropic.ClaudeCode` | https://code.claude.com/docs/en/setup |
| Network domains | API and authentication host requirements | https://code.claude.com/docs/en/network-config |
| Desktop relationship | Desktop uses the same underlying engine and shares configuration/project memory | https://code.claude.com/docs/en/desktop |
| Settings locations | User `~/.claude/settings.json`, managed Windows locations and registry | https://code.claude.com/docs/en/settings |
| Windows code signature | Windows Claude binary signed by “Anthropic, PBC” | https://code.claude.com/docs/en/setup |

## 11.2 Microsoft official sources

| Topic | Fact used | Source |
|---|---|---|
| .NET lifecycle | .NET 10 is LTS and active as of 2026-07 | https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core |
| Self-contained single-file | `PublishSingleFile`, `SelfContained`, runtime-specific publishing | https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview |
| WinGet support | WinGet supported on Windows 10 1809+ and modern Windows versions | https://learn.microsoft.com/en-us/windows/package-manager/winget/ |

## 11.3 Facts vs design decisions

### Verified facts

- Git is optional on native Windows when PowerShell is available.
- Native Claude Code uses `%USERPROFILE%\.local\bin\claude.exe`.
- `claude auth status` provides a machine-readable status and exit code.
- `CLAUDE_CODE_GIT_BASH_PATH` is an official configuration option for Git Bash discovery.
- Node.js is not listed as a native-install system requirement.

### Product decisions

- WPF and .NET 10 are project architecture choices, not Anthropic requirements.
- Basic diagnostics run offline by default.
- Machine PATH and managed settings are never auto-modified.
- WinGet is preferred over automatically piping a remote PowerShell installer.
- Git absence is displayed as a recommendation when PowerShell works.
- Diagnostic report omits user identity and full PATH.

## 11.4 Items requiring re-verification before release

- Minimum supported Windows build
- Minimum RAM
- Native install directory
- WinGet package ID
- `claude auth status` output and exit codes
- `claude doctor` behavior
- Desktop alias conflict guidance
- Git Bash environment variable name
- Required network domains
- Current .NET LTS and support dates
