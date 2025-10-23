<# 使い方例（ビルド後イベントから）:
powershell -ExecutionPolicy Bypass -File "$(SolutionDir)Sign-SetupArtifacts.ps1" ^
  -OutDir "$(ProjectDir)$(Configuration)" ^
  -Thumbprint 89AA6D9BABBAAE6672A34DE9F07E47359389ACF2 ^
  -UseMachineStore

※ 出力先が Release 固定なら -OutDir "$(ProjectDir)Release" でもOK
#>

param(
  [Parameter(Mandatory = $true)]
  [string]$OutDir,

  # 証明書の拇印（スペース無し）
  [Parameter(Mandatory = $true)]
  [string]$Thumbprint,

  # signtool のパス（環境に合わせて必要なら変更）
  [string]$SignTool = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe",

  # タイムスタンプ
  [string]$TimeStampUrl = "http://timestamp.digicert.com",

  # 証明書ストア名
  [string]$StoreName = "My",

  # ローカルコンピューターストアを使う場合は指定（EVトークンなど）
  [switch]$UseMachineStore
)

function Write-Info($msg)  { Write-Host "[INFO]  $msg" -ForegroundColor Cyan }
function Write-Error2($msg){ Write-Host "[ERROR] $msg" -ForegroundColor Red }

if (-not (Test-Path $OutDir)) {
  Write-Error2 "出力フォルダが見つかりません: $OutDir"
  exit 1
}

if (-not (Test-Path $SignTool)) {
  Write-Error2 "signtool が見つかりません: $SignTool"
  exit 1
}

function Sign-OneFile {
  param([string]$Path)

  if (-not (Test-Path $Path)) { return }

  Write-Info "署名中: $Path"

  $args = @(
    'sign',
    '/fd','SHA256',
    '/td','SHA256',
    '/tr', $TimeStampUrl,
    '/sha1', $Thumbprint,
    '/s', $StoreName
  )
  if ($UseMachineStore) { $args += '/sm' }
  $args += $Path

  & "$SignTool" @args
  if ($LASTEXITCODE -ne 0) {
    Write-Error2 "signtool sign 失敗: $Path"
    exit $LASTEXITCODE
  }

  # 検証（失敗しても止めたい場合は exit に変更）
  & "$SignTool" verify /pa /v "$Path" | Out-Null
  if ($LASTEXITCODE -ne 0) {
    Write-Error2 "signtool verify 警告/失敗: $Path"
  } else {
    Write-Info "検証OK: $Path"
  }
}

# --- 署名対象（必要に応じて拡張可）---
# MSI（必須）
Get-ChildItem -Path $OutDir -Filter *.msi -ErrorAction SilentlyContinue | ForEach-Object {
  Sign-OneFile $_.FullName
}

# ブートストラッパ（存在する場合）
Get-ChildItem -Path $OutDir -Filter Setup*.exe -ErrorAction SilentlyContinue | ForEach-Object {
  Sign-OneFile $_.FullName
}

# 必要なら CAB なども（コメント解除）
# Get-ChildItem -Path $OutDir -Filter *.cab -ErrorAction SilentlyContinue | ForEach-Object {
#   Sign-OneFile $_.FullName
# }

Write-Info "署名完了。"
