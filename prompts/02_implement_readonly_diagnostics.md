# Prompt 02 - Implement read-only diagnostics

Phase 0の成果を確認し、`docs/05_diagnostic_catalog.md` を読み、Phase 1のread-only diagnosticsを実装してください。

対象:

- CHK-OS-001
- CHK-ARCH-001
- CHK-MEM-001
- CHK-SHELL-001 / 002
- CHK-CLAUDE-001 / 002 / 004
- CHK-PATH-001
- CHK-GIT-001
- CHK-GITBASH-001
- readiness aggregation

要件:

- Windows APIとProcess実行をインターフェース化する。
- 各コマンドにtimeout/cancellationを実装する。
- stdout/stderrを分離する。
- 実環境を変更しない。
- fake executablesとtemporary directoriesでテストする。
- Gitなし＋PowerShellありを必須エラーにしないテストを入れる。
- `%USERPROFILE%\.local\bin` の正規化・重複判定をテストする。
- 仕様と異なる判断が必要ならopen_questionsへ記録する。

完了時にbuild/test結果と未実装チェックを報告してください。
