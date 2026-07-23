# Prompt 04 - Authentication, reporting, optional network

次を実装してください。

- CHK-AUTH-001 using `claude auth status`
- REM-AUTH-LOGIN as a visible terminal flow
- CHK-WINGET-001 and confirmed WinGet install action
- JSON/text reporting using `specs/diagnostic-result.schema.json`
- redaction engine and leakage tests
- user-initiated network probes
- optional `claude doctor` with timeout and cancellation
- IT handoff messages

制約:

- 認証raw JSON、email、tokenを保存しない。
- API key環境変数は存在有無だけ扱う。
- ネットワーク診断は明示操作時だけ実行する。
- TLS検証を無効にしない。
- 企業プロキシ、CA、EDR、GPOを自動変更しない。
- `irm | iex` を実行しない。

完了時にreport schema validation、secret leakage tests、offline testの結果を提示してください。
