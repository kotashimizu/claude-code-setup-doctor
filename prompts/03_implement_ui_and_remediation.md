# Prompt 03 - Implement UI and safe remediation

`docs/03_ui_ux_spec.md`、`docs/06_remediation_spec.md`、`docs/07_security_privacy_enterprise.md` を読み、Phase 2とPhase 3を順番に実装してください。

Phase 2:

- Start / Scanning / Results画面
- progress/cancel
- 状態語と日本語リソース
- 詳細表示
- accessibility properties

Phase 3:

- remediation preview
- explicit confirmation
- REM-PATH-CLAUDE-LOCALBIN
- REM-PATH-POWERSHELL
- REM-CLAUDE-GITBASH-SETTING
- backup, atomic write, rollback
- post-remediation verification

安全要件:

- Machine PATH、HKLM、Managed settingsを変更しない。
- 単体テストで実User PATHと実settings.jsonを変更しない。
- 文字列連結したshell commandを使わない。
- 不正JSONを上書きしない。
- 修復成功は再診断Passで確定する。

各Phaseの終了時にbuild/testを実行し、UIスクリーンショットまたは実行確認方法を記録してください。
