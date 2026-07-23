using SetupDoctor.Core.Diagnostics;

namespace SetupDoctor.App.ViewModels;

// チェックID＋ステータスの組み合わせを、ITが苦手な人にも伝わる一言説明に変換する。
// 文言のトーンは docs/03_ui_ux_spec.md 3.4 S-03 の例に合わせる。
public static class CheckExplanations
{
    public static string Explain(string checkId, DiagnosticStatus status) => (checkId, status) switch
    {
        ("CHK-OS-001", DiagnosticStatus.Pass) => "Windowsのバージョンは問題ありません。",
        ("CHK-OS-001", DiagnosticStatus.Fail) => "お使いのWindowsのバージョンが古く、Claude Codeが動作しない可能性があります。Windows Updateを確認してください。",

        ("CHK-ARCH-001", DiagnosticStatus.Pass) => "パソコンの種類は問題ありません。",
        ("CHK-ARCH-001", DiagnosticStatus.Fail) => "このアプリはお使いのパソコンの種類に対応していません。別の配布物が必要です。",

        ("CHK-MEM-001", DiagnosticStatus.Pass) => "メモリの搭載量は問題ありません。",
        ("CHK-MEM-001", DiagnosticStatus.Fail) => "メモリが不足しています（4GB未満）。動作が不安定になる可能性があります。",
        ("CHK-MEM-001", DiagnosticStatus.Unknown) => "メモリの搭載量を確認できませんでした。",

        ("CHK-SHELL-001", DiagnosticStatus.Pass) => "Windows PowerShellが使えます。",
        ("CHK-SHELL-001", DiagnosticStatus.Repairable) => "Windows PowerShellはパソコンに入っていますが、今の設定では見つけられません。自動で直せます。",
        ("CHK-SHELL-001", DiagnosticStatus.ITAction) => "Windows PowerShellの実行が会社のセキュリティポリシーでブロックされています。IT担当者に確認してください。",
        ("CHK-SHELL-001", DiagnosticStatus.Unknown) => "Windows PowerShellの応答確認がタイムアウトしました。",

        ("CHK-SHELL-002", DiagnosticStatus.Pass) => "PowerShell 7が使えます。",
        ("CHK-SHELL-002", DiagnosticStatus.Warning) => "PowerShell 7は入っていませんが、Windows PowerShellがあれば問題なく利用できます（任意の項目です）。",

        ("CHK-CLAUDE-001", DiagnosticStatus.Pass) => "Claude Codeが見つかりました。",
        ("CHK-CLAUDE-001", DiagnosticStatus.Repairable) => "Claude Codeはインストールされていますが、パソコンがその場所を認識できていません。自動で直せます。",
        ("CHK-CLAUDE-001", DiagnosticStatus.Fail) => "Claude Codeが見つかりませんでした。インストールされているか確認してください。",

        ("CHK-CLAUDE-002", DiagnosticStatus.Pass) => "Claude Codeが正常に起動できることを確認しました。",
        ("CHK-CLAUDE-002", DiagnosticStatus.Fail) => "Claude Codeが見つかりましたが、正常に起動しませんでした。",
        ("CHK-CLAUDE-002", DiagnosticStatus.Unknown) => "Claude Codeの起動確認がタイムアウトしました。古いClaude Desktopアプリが影響している可能性があります。",
        ("CHK-CLAUDE-002", DiagnosticStatus.NotApplicable) => "Claude Code本体が見つからなかったため、この確認はスキップされました。",

        ("CHK-CLAUDE-003", DiagnosticStatus.UserAction) => "Claude Desktop（別のアプリ）がClaude Codeより先に反応しています。Claude Desktopを最新の状態に更新してから、もう一度診断してください。",
        ("CHK-CLAUDE-003", DiagnosticStatus.NotApplicable) => "Claude Desktopとの競合は見つかりませんでした。",

        ("CHK-CLAUDE-004", DiagnosticStatus.Pass) => "Claude Codeのインストールは1つだけです。",
        ("CHK-CLAUDE-004", DiagnosticStatus.Warning) => "Claude Codeが複数の場所にインストールされています。意図しない古いバージョンが使われる可能性があります（任意の項目です）。",

        ("CHK-PATH-001", DiagnosticStatus.Pass) => "Claude Codeへの道筋（PATH）は正しく設定されています。",
        ("CHK-PATH-001", DiagnosticStatus.Repairable) => "Claude Codeはインストールされていますが、ターミナルから見つけられません。自動で直せます。",
        ("CHK-PATH-001", DiagnosticStatus.Warning) => "設定は完了していますが、開いたままのターミナルには反映されていません。新しいターミナルを開いてください。",
        ("CHK-PATH-001", DiagnosticStatus.NotApplicable) => "Claude Code本体が見つからなかったため、この確認はスキップされました。",

        ("CHK-AUTH-001", DiagnosticStatus.Pass) => "Claude Codeにログイン済みです。",
        ("CHK-AUTH-001", DiagnosticStatus.UserAction) => "Claude Codeにログインしていません。ログインが必要です。",
        ("CHK-AUTH-001", DiagnosticStatus.Unknown) => "ログイン状態を確認できませんでした。",

        ("CHK-GIT-001", DiagnosticStatus.Pass) => "Gitが使えます。",
        ("CHK-GIT-001", DiagnosticStatus.Warning) => "Gitは入っていませんが、PowerShellが使えるため問題ありません（推奨の項目です）。",
        ("CHK-GIT-001", DiagnosticStatus.Fail) => "Gitが見つからず、他に使えるシェルもありません。",

        ("CHK-GITBASH-001", DiagnosticStatus.Pass) => "Git Bashの設定は問題ありません。",
        ("CHK-GITBASH-001", DiagnosticStatus.Repairable) => "Git Bashは入っていますが、Claude Codeの設定に登録されていません。自動で直せます。",
        ("CHK-GITBASH-001", DiagnosticStatus.Warning) => "Git Bashは入っていませんが、PowerShellが使えるため問題ありません（推奨の項目です）。",
        ("CHK-GITBASH-001", DiagnosticStatus.Fail) => "Git Bashが見つからず、他に使えるシェルもありません。",

        ("CHK-WINGET-001", DiagnosticStatus.Pass) => "wingetが使えます。",
        ("CHK-WINGET-001", DiagnosticStatus.Warning) => "wingetが入っていません（任意の項目です）。",

        ("CHK-NET-001", DiagnosticStatus.Pass) => "Anthropicのサーバーに接続できました。",
        ("CHK-NET-002", DiagnosticStatus.Pass) => "Anthropicのサーバーに接続できました。",
        ("CHK-NET-003", DiagnosticStatus.Pass) => "Anthropicのサーバーに接続できました。",
        (_, DiagnosticStatus.ITAction) when checkId.StartsWith("CHK-NET-") =>
            "Anthropicのサーバーに接続できませんでした。会社のネットワーク制限が原因の可能性があります。",
        (_, DiagnosticStatus.Unknown) when checkId.StartsWith("CHK-NET-") =>
            "接続を確認できませんでした。",

        ("CHK-DOCTOR-001", DiagnosticStatus.Pass) => "補助診断（claude doctor）を実行しました。",
        ("CHK-DOCTOR-001", DiagnosticStatus.Unknown) => "補助診断（claude doctor）がタイムアウトしました。",

        // Cowork（Claude DesktopのAIエージェント機能）。Claude Code本体の準備状態には影響しません。
        ("CHK-COWORK-001", DiagnosticStatus.Pass) => "Coworkの仮想マシンサービスは動いています。",
        ("CHK-COWORK-001", DiagnosticStatus.Repairable) => "Coworkの仮想マシンサービスが停止しています。自動で直せます。",
        ("CHK-COWORK-001", DiagnosticStatus.Unknown) => "Coworkの仮想マシンサービスが見つかりませんでした。Claude DesktopがCowork対応バージョンか確認してください。",

        ("CHK-COWORK-002", DiagnosticStatus.Pass) => "Coworkに必要な仮想化機能は有効になっています。",
        ("CHK-COWORK-002", DiagnosticStatus.Repairable) => "Coworkに必要な仮想化機能（Virtual Machine Platform）が無効になっています。有効化の手順を案内します（再起動が必要です）。",
        ("CHK-COWORK-002", DiagnosticStatus.Unknown) => "お使いのWindows Home Editionでは、仮想化機能が有効でもCoworkの動作が不安定という報告があります。動かない場合はこの点を疑ってください。",

        ("CHK-COWORK-003", DiagnosticStatus.Pass) => "Coworkに必要なネットワークコンポーネントは揃っています。",
        ("CHK-COWORK-003", DiagnosticStatus.Repairable) => "Coworkに必要なネットワークコンポーネントの一部が見つかりません。修復手順を案内します。",
        ("CHK-COWORK-003", DiagnosticStatus.NotApplicable) => "仮想化機能が未確認のため、この確認はスキップされました。",

        ("CHK-COWORK-004", DiagnosticStatus.Pass) => "Coworkの仮想ディスクは正常な状態です。",
        ("CHK-COWORK-004", DiagnosticStatus.Repairable) => "Coworkの仮想ディスクが圧縮された状態になっており、起動に失敗する原因になります。自動で直せます。",
        ("CHK-COWORK-004", DiagnosticStatus.Unknown) => "Coworkの仮想ディスクがまだ作成されていません（未使用の可能性があります）。",

        ("CHK-COWORK-005", DiagnosticStatus.Pass) => "Coworkのネットワーク設定に競合は見つかりませんでした。",
        ("CHK-COWORK-005", DiagnosticStatus.ITAction) => "VPNやDockerなど他のソフトとネットワークの設定が重なっている可能性があります。それらを一時停止してCoworkの起動を試してください。",

        ("CHK-COWORK-006", DiagnosticStatus.Unknown) => "他の項目は問題ないのにCoworkが動かない場合は、組織（会社）側の管理設定で無効化されている可能性があります。管理者にご確認ください。",
        ("CHK-COWORK-006", DiagnosticStatus.NotApplicable) => "他に確認すべき項目が見つかったため、この案内は表示されません。",

        _ => status switch
        {
            DiagnosticStatus.Pass => "問題ありません。",
            DiagnosticStatus.NotApplicable => "この環境では対象外の項目です。",
            _ => "詳細は技術情報を確認してください。",
        },
    };

    // docs/05_diagnostic_catalog.md 5.2 の Requirement 列に基づく
    public static string RequirementLevelKey(string checkId) => checkId switch
    {
        "CHK-OS-001" or "CHK-ARCH-001" or "CHK-MEM-001" => "Required",
        "CHK-SHELL-001" => "Required",
        "CHK-SHELL-002" => "Recommended",
        "CHK-CLAUDE-001" or "CHK-CLAUDE-002" or "CHK-CLAUDE-003" => "Required",
        "CHK-CLAUDE-004" => "Recommended",
        "CHK-PATH-001" => "Required",
        "CHK-AUTH-001" => "Required",
        "CHK-GIT-001" or "CHK-GITBASH-001" => "Recommended",
        "CHK-WINGET-001" => "Optional",
        "CHK-NET-001" or "CHK-NET-002" or "CHK-NET-003" => "Optional",
        "CHK-DOCTOR-001" => "Optional",
        _ => "Optional",
    };

    // 重要度順（数字が小さいほど先に表示）。docs/03 3.4「結果を重要度順に表示する」に対応。
    public static int Severity(DiagnosticStatus status) => status switch
    {
        DiagnosticStatus.Fail => 0,
        DiagnosticStatus.Unknown => 1,
        DiagnosticStatus.ITAction => 2,
        DiagnosticStatus.Repairable => 3,
        DiagnosticStatus.UserAction => 4,
        DiagnosticStatus.Warning => 5,
        _ => 9,
    };
}
