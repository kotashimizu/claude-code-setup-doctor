# Prompt 01 - Bootstrap repository

このリポジトリの `CLAUDE.md`、`START_HERE.md`、`docs/00_project_overview.md`、`docs/01_product_requirements.md`、`docs/02_functional_requirements.md`、`docs/04_technical_architecture.md`、`docs/07_security_privacy_enterprise.md`、`docs/08_test_plan.md` を読んでください。

今回は **Phase 0だけ** を実装してください。

必須作業:

1. 仕様間の矛盾または未確定事項を `docs/implementation_notes/open_questions.md` に整理する。
2. `docs/implementation_notes/current_plan.md` にPhase 0の実装計画を書く。
3. .NET 10のsolutionと次のprojectを作成する。
   - `SetupDoctor.App` WPF
   - `SetupDoctor.Core` class library
   - `SetupDoctor.Infrastructure.Windows` class library
   - `SetupDoctor.Reporting` class library
   - unit/integration test projects
4. 依存方向を仕様どおりに設定する。
5. Coreのenum、基本record、result aggregationの骨格とテストを作る。
6. Fake clock、fake command runnerのテスト基盤を作る。
7. `dotnet build` と `dotnet test` を実行する。
8. 変更ファイル、テスト結果、残るリスクを報告する。

禁止:

- PATH、レジストリ、Claude設定の実変更
- WinGetや外部インストーラーの実行
- Phase 1以降の機能実装
- リモートへのpush
