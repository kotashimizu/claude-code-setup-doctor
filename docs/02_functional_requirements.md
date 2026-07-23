# 02. Functional and Non-functional Requirements

## 2.1 機能要件

| ID | 要件 | 優先度 | 受入条件 |
|---|---|---:|---|
| FR-001 | 起動時にアプリの非公式性と診断範囲を表示する | Must | 初回画面またはAboutに「非公式ツール」を表示 |
| FR-002 | ネットワークを使わない基本診断を実行する | Must | OS、シェル、CLI、PATH、Gitをオフラインで判定 |
| FR-003 | OSバージョン、64-bit、メモリを判定する | Must | 公式要件に対するPass／Fail／Unknownを表示 |
| FR-004 | PowerShell、PowerShell 7、Git Bashを検出する | Must | 少なくとも1つの実行可能シェルを識別 |
| FR-005 | PATH上のすべての `claude` 候補を列挙する | Must | 優先順位と実体パスを内部結果に保持 |
| FR-006 | ネイティブ既定パスのClaude実体を確認する | Must | `%USERPROFILE%\.local\bin\claude.exe` を確認 |
| FR-007 | `claude --version` をタイムアウト付きで実行する | Must | exit code、stdout、実行時間を取得し、失敗を分類 |
| FR-008 | WindowsAppsのClaude Desktop競合を検出する | Should | `claude --version` がGUI起動／タイムアウトまたは優先パスがWindowsAppsの場合に警告 |
| FR-009 | 複数インストールを検出する | Must | 2件以上の候補をWarningとして表示 |
| FR-010 | User PATHにClaudeネイティブパスがあるか確認する | Must | 現プロセスPATHと永続User PATHを分けて判定 |
| FR-011 | `claude auth status` を実行し認証状態を判定する | Must | exit 0=認証済み、exit 1=未認証、その他=Unknown |
| FR-012 | GitおよびGit Bashの存在を確認する | Must | Git未導入でもPowerShell利用可なら必須エラーにしない |
| FR-013 | `CLAUDE_CODE_GIT_BASH_PATH` の有効性を確認する | Should | 設定値の存在、ファイル存在、実行可否を検証 |
| FR-014 | WinGetの利用可否を確認する | Should | `winget --version` または実行ファイル検出 |
| FR-015 | 利用者が選択した場合にネットワーク診断を実行する | Should | 対象ホストごとに到達／遮断／TLS問題／Unknownを表示 |
| FR-016 | `claude doctor` を補助診断として実行できる | Should | タイムアウト可能、結果は詳細ログ扱い、全体判定に単独使用しない |
| FR-017 | 修復候補を診断結果から生成する | Must | 変更対象、変更前、変更後、権限、再起動要否を表示 |
| FR-018 | 修復前に利用者の明示確認を取得する | Must | 未確認では状態変更処理を開始しない |
| FR-019 | `%USERPROFILE%\.local\bin` をUser PATHへ追加する | Must | 重複なし、空PATH対応、バックアップ、再診断 |
| FR-020 | Git BashパスをClaudeユーザー設定へ追加する | Must | JSONを保持してマージ、バックアップ、原子的保存、再診断 |
| FR-021 | `claude auth login` を可視ターミナルで開始する | Must | 認証情報をアプリが取得せず、完了後に再診断可能 |
| FR-022 | WinGetでClaude Codeインストールを開始できる | Should | コマンドとパッケージIDを表示後、利用者確認を得て実行 |
| FR-023 | 修復結果を再診断する | Must | 再診断Passの場合のみ「修復完了」 |
| FR-024 | 診断レポートを保存する | Must | JSONとテキスト、秘密情報を除外 |
| FR-025 | 診断ログを画面で確認できる | Should | コマンドの赤acted概要、exit code、時間を表示 |
| FR-026 | キャンセル可能である | Must | 長時間チェックを中止し、完了済み結果は保持 |
| FR-027 | 障害時にIT向けメッセージを表示する | Must | 自動修復対象外の理由と確認項目を表示 |

## 2.2 非機能要件

### NFR-001 安全性

- 起動時に管理者権限を要求しない。
- Machine PATH、HKLM、管理ポリシーを変更しない。
- 状態変更はAllowlistされた修復アクションだけに限定する。
- 修復は実行前レビュー、バックアップ、原子的保存、再診断を必須とする。

### NFR-002 性能目標

以下は設計目標であり、サービス保証値ではありません。

- オフライン基本診断: 通常10秒以内
- 個別コマンド既定タイムアウト: 5秒
- `claude doctor`: 30秒、利用者が延長または中止可能
- ネットワークプローブ: ホストごとに5秒
- UIスレッドをブロックしない

### NFR-003 信頼性

- 1チェックの失敗で全診断を中断しない。
- 各チェックは独立結果を返す。
- 例外はResult型へ変換し、画面でUnknownとして扱う。
- 修復途中の失敗は、完了済み操作と未実行操作を区別する。

### NFR-004 互換性

- Windows 10 1809以降、Windows 11、Windows Server 2019以降を判定対象とする。
- x64をMVP配布対象、ARM64を後続対象とする。
- 日本語ユーザー名、空白、括弧、非ASCII文字を含むパスを扱う。
- PowerShell 5.1とPowerShell 7の双方を検出する。

### NFR-005 保守性

- 診断項目と修復アクションをIDで管理する。
- 公式仕様で変わり得る値は設定カタログへ分離する。
- OS操作を抽象化し、テスト用実装を差し替え可能にする。
- UIロジックと診断ロジックを分離する。

### NFR-006 アクセシビリティ

- 色だけで状態を伝えない。
- アイコン、状態語、説明文を併用する。
- Tab順序、フォーカス表示、Enter／Space操作を提供する。
- 200%表示で主要操作が欠けない。
- スクリーンリーダー向けAutomationPropertiesを設定する。

### NFR-007 プライバシー

- テレメトリはMVPで送信しない。
- レポートにユーザー名、メールアドレス、トークン、APIキー、完全PATHを含めない。
- ユーザープロファイル部分は `%USERPROFILE%` に正規化する。
- 環境変数は名前と存在有無だけ記録する。

### NFR-008 配布

- .NETランタイム未導入端末で起動できる自己完結型を作る。
- x64とARM64は別配布物とする。
- パイロットはZIP／単一EXE、正式配布はコード署名とMSIX/MSIを検討する。

## 2.3 受入シナリオ

### AC-001 正常環境

Given: 対応Windows、PowerShell、Claude Code、認証が有効  
When: 基本診断を実行  
Then: 全体状態がReadyまたはReadyWithRecommendationsになる。

### AC-002 Claude PATH不足

Given: `%USERPROFILE%\.local\bin\claude.exe` は存在するがUser PATHにない  
When: 診断後にPATH修復を承認  
Then: User PATHへ重複なく追加され、再診断でCLIがPassになる。

### AC-003 Gitなし

Given: Git for Windowsなし、PowerShellあり、Claude Code利用可能  
When: 診断  
Then: 必須項目はPass、Gitは推奨として表示され、全体状態はReadyWithRecommendations以下にならない。

### AC-004 Git Bashカスタムパス

Given: Git Bashが標準外パスに存在し、Claude設定がない  
When: Git Bash設定修復を承認  
Then: `~/.claude/settings.json` に設定がマージされ、既存キーは保持される。

### AC-005 Desktop競合

Given: PATHの先頭がWindowsAppsの古い `Claude.exe`  
When: `claude --version` を確認  
Then: Desktop競合と判定し、自動PATH変更ではなくDesktop更新案内を表示する。

### AC-006 企業制限

Given: EDRにより `claude.exe` から `bash.exe` の起動がブロック  
When: 診断  
Then: ITActionRequiredとして、実行パスと失敗種別を赤actedレポートへ記録する。
