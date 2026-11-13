# 构建 CuteSakikoMod 的「加载器 + 按游戏版本变体」包。
#
# 结构：
#   CuteSakikoMod.dll             ← 加载器（loader/CuteSakikoMod.Loader.csproj）
#   CuteSakikoMod.json
#   CuteSakikoMod.pck             ← 共用一份（由正常构建导出，脚本不动）
#   audio\                        ← 共用（由正常构建复制，脚本不动）
#   csk-variants.manifest         ← 变体清单（sha256）
#   compat/<版本>/CuteSakikoMod.Compat.dll + compat-target.txt
#
# 用法：
#   pwsh -File tools\build-bundle.ps1 -GameVersions '0.107.1,0.111.0,0.113.0' -Deploy
param(
    [string[]]$GameVersions = @('0.107.1', '0.111.0', '0.113.0'),
    [string]$OutDir = 'E:\SLJT2\.build-check\mainmod-bundle',
    [switch]$Deploy,
    [string]$ModsDir = 'E:\SteamLibrary\steamapps\common\Slay the Spire 2\mods'
)

$ErrorActionPreference = 'Stop'
$GameVersions = @($GameVersions | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$root = Split-Path -Parent $PSScriptRoot
$bundle = $OutDir
$compatRoot = Join-Path $bundle 'compat'

function Resolve-RefsDir([string]$Version) {
    $short = $Version -replace '\.0$', ''
    $pattern = '^Sts2Recover-' + [regex]::Escape($short)
    foreach ($dir in Get-ChildItem 'E:\SLJT2\anti' -Directory | Where-Object { $_.Name -match $pattern }) {
        $refs = Join-Path $dir.FullName 'game-refs'
        if (Test-Path (Join-Path $refs 'sts2.dll')) { return $refs }
    }
    throw "找不到 $Version 的 game-refs（anti\Sts2Recover-*）"
}

function Resolve-RitsuLib([string]$Version) {
    if ([version]$Version -ge [version]'0.113.0') {
        return @{
            Id = 'STS2.RitsuLib'
            Version = '0.6.11'
            Runtime = 'E:\SLJT2\Rider\RiderProjects\CuteSakikoMod\packages\sts2.ritsulib\0.6.11\lib\net9.0\STS2-RitsuLib.Runtime.dll'
        }
    }
    $compatVersion = if ([version]$Version -ge [version]'0.111.0') { '0.111.0' } else { '0.107.1' }
    $pkgName = "sts2.ritsulib.compat.$compatVersion"
    $runtime = "E:\SLJT2\Rider\RiderProjects\CuteSakikoMod\packages\$pkgName\0.6.11\lib\net9.0\STS2-RitsuLib.Runtime.dll"
    return @{
        Id = "STS2.RitsuLib.Compat.$compatVersion"
        Version = '0.6.11'
        Runtime = $runtime
    }
}

New-Item -ItemType Directory -Force -Path $compatRoot | Out-Null

foreach ($version in $GameVersions) {
    $refs = Resolve-RefsDir $version
    $rl = Resolve-RitsuLib $version
    if (-not (Test-Path $rl.Runtime)) {
        throw "缺少 RitsuLib Runtime：$($rl.Runtime)"
    }

    $target = Join-Path $compatRoot $version
    New-Item -ItemType Directory -Force -Path $target | Out-Null
    Write-Host "[bundle] 构建本体 $version 变体 ..." -ForegroundColor Cyan
    & dotnet build (Join-Path $root 'CuteSakikoMod.csproj') -c Release -v m `
        -o $target `
        -p:AssemblyName=CuteSakikoMod `
        -p:Sts2GameVersion=$version `
        -p:Sts2Path=$refs `
        -p:Sts2DataDir=$refs `
        -p:RitsuLibPackageId=$($rl.Id) `
        -p:RitsuLibPackageVersion=$($rl.Version) `
        -p:SkipPck=true -p:SkipDeploy=true | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "变体 $version 构建失败" }

    Set-Content -Path (Join-Path $target 'compat-target.txt') -Value $version -NoNewline
    Get-ChildItem $target -Filter '*.deps.json' | Remove-Item -Force -ErrorAction SilentlyContinue
    Get-ChildItem $target -Filter '*.pdb' | Remove-Item -Force -ErrorAction SilentlyContinue
}

Write-Host '[bundle] 构建加载器 ...' -ForegroundColor Cyan
$loaderOut = Join-Path $OutDir 'loader'
& dotnet build (Join-Path $root 'loader\CuteSakikoMod.Loader.csproj') -c Release -v m -o $loaderOut | Out-Host
if ($LASTEXITCODE -ne 0) { throw '加载器构建失败' }
Copy-Item (Join-Path $loaderOut 'CuteSakikoMod.Loader.dll') (Join-Path $bundle 'CuteSakikoMod.dll') -Force

Copy-Item (Join-Path $root 'CuteSakikoMod.json') (Join-Path $bundle 'CuteSakikoMod.json') -Force

$variants = @()
foreach ($target in Get-ChildItem $compatRoot -Directory | Sort-Object Name) {
    $dll = Join-Path $target.FullName 'CuteSakikoMod.dll'
    if (-not (Test-Path $dll)) { continue }
    $variants += [ordered]@{
        compatTarget = $target.Name
        assembly = 'CuteSakikoMod'
        sha256 = (Get-FileHash $dll -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}
$manifest = [ordered]@{ schema = 1; variants = $variants }
$manifest | ConvertTo-Json -Depth 6 | Set-Content -Path (Join-Path $bundle 'csk-variants.manifest') -Encoding UTF8

Write-Host "[bundle] 清单：$($variants.Count) 个变体" -ForegroundColor Green
$variants | ForEach-Object { Write-Host ("  {0}  {1}" -f $_.compatTarget, $_.sha256) }

if ($Deploy) {
    $dest = Join-Path $ModsDir 'CuteSakikoMod'
    New-Item -ItemType Directory -Force -Path $dest | Out-Null
    # 只覆盖加载器相关文件；pck / audio 由正常构建维护，脚本不动。
    Copy-Item (Join-Path $bundle 'CuteSakikoMod.dll') $dest -Force
    Copy-Item (Join-Path $bundle 'CuteSakikoMod.json') $dest -Force
    Copy-Item (Join-Path $bundle 'csk-variants.manifest') $dest -Force
    Copy-Item (Join-Path $bundle 'compat') $dest -Recurse -Force
    Write-Host "[bundle] 已部署到 $dest" -ForegroundColor Green
}
