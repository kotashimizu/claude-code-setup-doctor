# Open Questions and Specification Conflicts

仕様間の矛盾または未確定事項を記録します。
解決済み項目は `## Resolved` セクションへ移動し、根拠を残します。

---

## Open

### Q-001: CHK-SHELL-001「Required conditional」の条件が未定義

- **箇所**: `docs/05_diagnostic_catalog.md` CHK-SHELL-001
- **問題**: Requirement が "Required conditional" と記述されているが、どの状態でRequiredになるかが明示されていない。
- **暫定判断**: シェルが1つも存在しない状況でRequiredとして扱い、PowerShell利用可能な場合はRecommendedへ格下げする。CHK-GIT-001の"Fail: absent only when PowerShell is also unavailable and no Git Bash exists"という記述と整合させる。
- **確認が必要な箇所**: `SetupDoctor.Core/Diagnostics/` の PowerShell チェック実装

### Q-002: FR-008（Should）vs CHK-CLAUDE-003（Required when conflict detected）の優先度不一致

- **箇所**: `docs/02_functional_requirements.md` FR-008 / `docs/05_diagnostic_catalog.md` CHK-CLAUDE-003
- **問題**: FR-008 は "Should" 優先度だが、CHK-CLAUDE-003 は "Required when conflict detected" と記述している。
- **暫定判断**: CLAUDE.md の Source-of-truth 順位に従い、05（優先度4）> 02（優先度4）の関係は同順位だが、CHK側のより具体的な条件付き必須定義を優先する。競合が検出された場合はRequired扱いとし、競合なしの場合は判定スキップ（NotApplicable）。
- **確認が必要な箇所**: 全体判定アルゴリズムの NotApplicable ハンドリング

### Q-003: `claude auth status` タイムアウトが既定5秒と矛盾

- **箇所**: `docs/02_functional_requirements.md` NFR-002 / `docs/05_diagnostic_catalog.md` CHK-AUTH-001
- **問題**: NFR-002 は個別コマンドの既定タイムアウトを5秒とするが、CHK-AUTH-001 は10秒を明示している。
- **暫定判断**: CHK-AUTH-001 の個別指定を優先。OAuth認証のレスポンス待ちを考慮した意図的な延長とみなす。
- **確認が必要な箇所**: `ICommandRunner` の CommandRequest モデルにタイムアウトの個別指定を持たせること

### Q-004: CHK-ARCH-001 に対応する修復IDが未定義

- **箇所**: `docs/05_diagnostic_catalog.md` CHK-ARCH-001
- **問題**: Remediation として "正しい配布物を案内" と記述されているが、対応する REM-ID が付いていない。
- **暫定判断**: `REM-GUIDE-ARCH-MISMATCH` を仮定義し、ガイダンス表示専用（状態変更なし）のアクションとして実装する。実際の修復は行わず、正しいアーキテクチャの配布物URL・ファイル名を表示するのみ。
- **確認が必要な箇所**: `docs/06_remediation_spec.md` への追記要否

### Q-005: 実装コードの格納場所が明示されていない

- **箇所**: 全仕様書
- **問題**: 仕様書は `/projects/claude-code-setup-doctor-docs/` にある。実装コード（C#）を同ディレクトリ内 `src/` に置くのか、別リポジトリとするのか記述がない。
- **暫定判断**: `docs/implementation_notes/` がこのリポジトリ内に存在することから、同リポジトリ内の `src/` に実装を置く方針とする。
- **確認が必要な箇所**: 志水との合意

### Q-007: 「非公式」表記の重複と About 画面未実装

- **箇所**: `src/SetupDoctor.App/MainWindow.xaml`（タイトルバー）、`src/SetupDoctor.App/Views/StartView.xaml`
- **問題**: プロジェクトCLAUDE.mdは「非公式ツールであることをAboutと初回同意画面に明示する」ことを要求している。実装ではタイトルバーの「（非公式）」とその直後の「・非公式ツール」が同じ画面内で重複表示されており、志水の実機確認で冗長と指摘された。また専用のAbout画面（docs/03 S-01が言及するバージョン・プライバシー・About導線）はまだ実装されていない。
- **暫定判断**: 商標・提携誤認を避ける要件は維持しつつ、表示は1箇所に集約する。タイトルバーからは「（非公式）」表記を外し、スタート画面の同意文（AppDisclaimer）を「このツールはAnthropic公式製品ではありません。個人が開発した非公式のコンパニオンユーティリティです。」という明確な1文に強化し、これを唯一の公式な開示箇所とする。About画面自体は本フェーズでは実装しない。
- **確認が必要な箇所**: 専用About画面（バージョン情報・プライバシー説明・非公式である旨の再掲）を今後のフェーズで実装するか、志水の最終判断が必要。

### Q-008: REM-PATH-POWERSHELL が未実装

- **箇所**: `docs/06_remediation_spec.md` §6.5 REM-PATH-POWERSHELL
- **問題**: 修復UIを結果画面に接続する際、実装済みの修復アクションは `REM-PATH-CLAUDE-LOCALBIN` と `REM-CLAUDE-GITBASH-SETTING` の2つのみ。CHK-SHELL-001（Windows PowerShellが既定パスに存在するがPATHにない）がRepairable状態になった場合に対応する `REM-PATH-POWERSHELL` アクションが未実装のため、この項目は「自動で直す」レビュー画面で「この項目は現在、自動修復に対応していません」という案内のみ表示され、実際の自動修復は行われない。
- **暫定判断**: 志水から報告された「レポート保存が機能しない／戻れない」「修復ボタンが存在しない」という不具合修正を優先し、新規の修復アクション実装はスコープ外とした。CHK-SHELL-001がRepairableになるのはWindows既定のPowerShellパスが何らかの理由でPATHから外れている稀なケースであり、影響は限定的と判断。
- **確認が必要な箇所**: `REM-PATH-POWERSHELL` を `AddClaudeToPathAction` と同様の実装（対象パスのみ異なる）で追加するかどうかの志水の判断。

- **箇所**: `docs/09_implementation_roadmap.md` Phase 0 Deliverables
- **問題**: "CI build/test workflow" が Phase 0 成果物に含まれるが、GitHub Actions・Azure DevOps等のプラットフォームが指定されていない。
- **暫定判断**: GitHub Actions を前提とした `.github/workflows/ci.yml` を作成する。不要な場合は後で削除可能。
- **確認が必要な箇所**: リポジトリのホスティング先

---

## Resolved

（現時点では解決済み項目なし）
