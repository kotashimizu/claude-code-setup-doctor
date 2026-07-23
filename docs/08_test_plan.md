# 08. Test Plan

## 8.1 テスト戦略

```text
        Manual / VM acceptance
       Integration on Windows
      Component and contract tests
     Unit tests for pure logic
```

Coreロジックは大量の単体テスト、Windows APIとプロセスは契約テスト、実環境差はVM手動テストで検証します。

## 8.2 単体テスト

### Path normalization

- 大文字小文字差
- 末尾バックスラッシュ
- 引用符付きPATH
- `%USERPROFILE%` と展開済みパス
- 空エントリ
- 重複
- UNC path
- 日本語／空白／括弧
- null User PATH

### Readiness aggregation

- Required Passのみ
- Recommended Warning
- Required Repairable
- Required UserAction
- ITAction優先
- Unsupported優先
- Unknown混在
- Gitなし＋PowerShellありがReadyを阻止しない

### JSON merge

- settings.jsonなし
- 空オブジェクト
- envなし
- env既存
- 同キー同値
- 同キー異値
- 不正JSON
- 読み取り専用
- ファイルロック
- UTF-8 BOMあり／なし
- 既存未知キー保持

### Redaction

- Windows user profile paths
- メールアドレス
- JWT形式
- Bearer token
- API key-like strings
- JSON secret keys
- PATH全体

### Command request validation

- 相対実行ファイル拒否
- 改行を含む引数
- 制御文字
- Allowlist外 executable
- timeout
- cancellation

## 8.3 コンポーネントテスト

- Fake `claude.exe` が `--version` を返す
- Fake `claude.exe` が5秒以上ハングする
- Fake `claude.exe` がexit 1
- Fake `claude.exe` がauth JSONを返す
- GUI風プロセスがstdoutなしで継続する
- 複数候補の優先順位
- Git custom path
- bash missing
- WindowsApps path classification

テスト用実行ファイルはテスト出力ディレクトリに生成し、実PATHを変更せず、テスト専用環境辞書を `ICommandRunner` に渡します。

## 8.4 Windows統合テスト

自動化可能な項目:

- `EnvironmentVariableTarget.User` を実ユーザーで変更しない契約テスト
- テスト用レジストリハイブまたは抽象サービス
- 一時settings.jsonへの原子的書き込み
- `WM_SETTINGCHANGE` wrapperの呼び出し検証
- Process tree cancellation
- Authenticode情報読み取り

実端末変更を伴うテストは、破棄可能な専用VMでのみ実行します。

## 8.5 VM／実機マトリクス

| Scenario | OS/Environment | Expected |
|---|---|---|
| Clean, no Claude | supported Windows x64 | Repairable / install action |
| Native Claude, PATH OK | supported Windows | Ready after auth |
| Native Claude, PATH missing | supported Windows | REM-PATH offered |
| Git absent, PowerShell present | supported Windows | ReadyWithRecommendations max |
| Git custom location | supported Windows | Git Bash setting offered |
| PowerShell PATH missing | supported Windows | PowerShell PATH repair offered |
| Both shell mechanisms unavailable | restricted VM | ITAction or Fail |
| Multiple Claude installs | native + WinGet/npm | Warning, no auto-uninstall |
| Desktop alias conflict | old alias simulation | UserAction guidance |
| Non-admin user | standard account | diagnostics and user repairs work |
| Japanese user profile | non-ASCII username | no path corruption |
| Corporate proxy | proxy test lab | network classified, no auto-change |
| TLS inspection | custom CA lab | certificate error, ITAction |
| EDR/AppLocker block | policy test VM | ITAction, no bypass |
| Offline | network disabled | basic diagnostics complete |
| Low memory | constrained VM | Unsupported or warning per detected RAM |

## 8.6 UIテスト

- キーボードのみで開始、結果確認、修復レビュー、保存まで完了
- 125%、150%、200%スケール
- 1366x768最小想定画面
- 長い日本語文言の折り返し
- High Contrast
- スクリーンリーダー名
- Cancel後の状態
- Partial failure表示
- 詳細ログのコピー

## 8.7 修復テスト

各修復に次のテストを必須とします。

1. Previewの変更前後が正しい。
2. 未確認ではExecuteされない。
3. Backupが先に作られる。
4. Executeが対象以外を変更しない。
5. VerificationがPassなら成功。
6. VerificationがFailなら失敗。
7. Rollbackで元に戻る。
8. 途中キャンセルで破損しない。

## 8.8 セキュリティテスト

- Path injection
- Fake executable earlier in PATH
- Symlink／junctionで想定外場所を指す候補
- Malformed JSON
- Secret leakage snapshot test
- Command argument injection
- Access denied
- Locked settings file
- Network certificate validation disabledでないこと
- Unsigned or unexpected publisher metadataの扱い

## 8.9 リリース判定

MVPリリースには以下が必要です。

- Release build成功
- 全単体テスト成功
- Windows統合テスト成功
- 代表VMシナリオ成功
- レポート赤action検査成功
- UI 200%検査成功
- PATH／settingsロールバック検証
- 既知の制限をREADMEへ記載
- 配布物ハッシュ生成
