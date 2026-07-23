#Requires -Version 5.1
# SetupDoctor.exe の SHA-256 チェックサムを生成して release/ に保存する
# 使用例: .\scripts\Generate-Checksum.ps1 -PublishDir .\publish\win-x64

param(
    [string]$PublishDir = ".\publish\win-x64",
    [string]$OutputDir  = ".\release"
)

$exe = Join-Path $PublishDir "SetupDoctor.exe"

if (-not (Test-Path $exe)) {
    Write-Error "実行ファイルが見つかりません: $exe"
    Write-Error "dotnet publish を実行してから再度実行してください。"
    exit 1
}

$null = New-Item -ItemType Directory -Path $OutputDir -Force

$hash = (Get-FileHash $exe -Algorithm SHA256).Hash.ToLowerInvariant()
$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exe).FileVersion
$filename = "SetupDoctor-v${version}-win-x64.exe"

# チェックサムファイル出力
$checksumFile = Join-Path $OutputDir "${filename}.sha256"
"${hash}  ${filename}" | Out-File -FilePath $checksumFile -Encoding utf8NoBOM

# 実行ファイルをコピー
Copy-Item $exe (Join-Path $OutputDir $filename) -Force

Write-Host "✅ リリースファイル生成完了"
Write-Host "   EXE  : $(Join-Path $OutputDir $filename)"
Write-Host "   SHA256: ${hash}"
Write-Host "   チェックサムファイル: $checksumFile"
