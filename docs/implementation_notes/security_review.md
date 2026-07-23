# セキュリティレビュー — Claude Code Setup Doctor

## レビュー日: 2026-07-23

---

## 1. 仕様適合確認

| 要件 | 実装状態 | 根拠ファイル |
|------|----------|-------------|
| ProcessStartInfo.ArgumentList を使用（シェル文字列連結禁止） | ✅ 適合 | WindowsCommandRunner.cs:23 — foreach で ArgumentList.Add |
| cmd /c 禁止 | ✅ 適合 | UseShellExecute=false、cmd.exe を executable に渡すコードなし |
| 機密変数（ANTHROPIC_API_KEY）は存在確認のみ | ✅ 適合 | AuthStatusCheck は exit code のみ保存。raw JSON を保持しない |
| authentication token / cookie を保存・表示しない | ✅ 適合 | AuthStatusCheck.cs:47–53 参照 |
| HKLM / Machine PATH / GPO 変更禁止 | ✅ 適合 | AddClaudeToPathAction は EnvironmentVariableTarget.User のみ |
| ファイル変更前バックアップ + 原子的書き込み | ✅ 適合 | WindowsClaudeSettingsService.cs, WindowsFileBackupService.cs |
| タイムアウト・CancellationToken 付き実行 | ✅ 適合 | 全 Check クラスで CommandRequest.Timeout を指定 |
| stdout/stderr 分離キャプチャ | ✅ 適合 | WindowsCommandRunner.cs:39–41 |
| タイムアウト時にプロセスツリー全体を kill | ✅ 適合 | WindowsCommandRunner.cs:50 — process.Kill(entireProcessTree: true) |
| PATH の完全値をログ出力しない | ✅ 適合 | ClaudeUserPathCheck は targetMasked で %USERPROFILE% のまま記録 |
| レポートでリダクション | ✅ 適合 | RedactionEngine.cs — email/token/USERPROFILE をマスク |

---

## 2. 発見された潜在的問題

### M-001 (Medium): ScanningView の BoolToColorConverter 未定義
- **場所**: ScanningView.xaml
- **内容**: `Converter={StaticResource BoolToColorConverter}` を参照しているが、App.xaml のリソース辞書に定義がない。
- **影響**: WPF ビルド時 XamlParseException。
- **対策**: BoolToColorConverter を削除するかシンプルな Style.Trigger に置き換える。

### L-001 (Low): WindowsNetworkProbe でのエラー詳細の範囲
- **場所**: WindowsNetworkProbe.cs
- **内容**: `ex.Message` を `ErrorDetail` に格納している。HttpRequestException.Message にはホスト名が含まれる可能性があるが、ユーザー個人情報ではないため許容範囲内。
- **対策**: 現状のまま。レポート出力時は RedactionEngine を通す。

### L-002 (Low): WindowsClaudeSettingsService でバックアップタイムスタンプ重複
- **場所**: WindowsClaudeSettingsService.cs:47
- **内容**: `DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss")` は秒精度のため、1秒以内の連続実行でバックアップファイルが衝突する。
- **対策**: `File.Copy(sourcePath, backupPath, overwrite: false)` で overwrite:false にしており、既存なら IOException になる。WindowsFileBackupService に統一すれば解決する（中期対策）。

---

## 3. テストカバレッジ確認

| 仕様要件テストシナリオ | テスト存在 |
|------------------------|-----------|
| 日本語ユーザー名でのパス正規化 | ✅ PathNormalizerTests.Append_JapaneseUsername |
| スペース入りパス | ✅ PathNormalizerTests.Split_SpaceInPath |
| 重複PATHエントリ | ✅ PathNormalizerTests.Append_NoDuplicates |
| 大文字小文字の差異 | ✅ PathNormalizerTests.Contains_CaseInsensitive |
| タイムアウト | ✅ ShellCheckTests.PS_Unknown_WhenTimedOut |
| キャンセル | ✅ RemediationOrchestratorTests.Execute_ThrowsOnCancellation |
| 部分修復失敗 | ✅ RemediationOrchestratorTests.Execute_Continues_After_Single_Action_Fails |
| AuthStatusCheck が raw JSON/token を保存しない | ✅ AuthStatusCheck コードレビューで確認（単体テスト追加推奨） |
| リダクション | ✅ RedactionEngineTests (6テスト) |

---

## 4. 未対応リスク（要追跡）

- **R-001**: 実環境 PATH テスト — WindowsPathEnvironmentService の実パス変更テストは CI (windows-latest) で実行されるが、テスト分離のために FakePathEnvironmentService を使用している。実機での動作は手動検証が必要。
- **R-002**: マルフォームド JSON テスト — WindowsClaudeSettingsService の malformed JSON ハンドリングテストは現時点で Integration Test に委ねている。
- **R-003**: AppLocker/EDR 環境でのプロセス起動拒否 — 現在のコードは exit code 5 を PowerShell ポリシーブロックとして検出するが、EDR によるブロックは exit code が異なる可能性がある。
