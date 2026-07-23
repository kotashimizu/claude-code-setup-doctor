# Prompt 05 - Security and release review

実装全体をレビューし、MVP release candidateを作ってください。

レビュー観点:

1. `docs/07_security_privacy_enterprise.md` の全項目
2. 任意コマンド実行経路がないか
3. PATHハイジャックと相対パス
4. settings JSON破損
5. secrets／PII leakage
6. access denied時の誤った自動昇格
7. Gitを必須扱いしていないか
8. Node.jsを必須表示していないか
9. Desktop alias conflictの扱い
10. cancellationとtimeout

作業:

- 重要度付きfindingsを作る。
- Critical/Highを修正する。
- 全testを実行する。
- win-x64 self-contained single-fileをpublishする。
- SHA-256を作る。
- `docs/release_notes/` に既知制限を書く。
- 配布物へClaude Code本体を同梱していないことを確認する。

リモートへのpushや公開は行わないでください。
