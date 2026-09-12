#Requires -Version 5.1
<#
.SYNOPSIS
  Gera publish self-contained + pacote Velopack (portable/instalador) para GitHub Releases.

.DESCRIPTION
  - Inclui apenas app-settings.example.json. O app grava as configuracoes em
    %AppData%\ConferenciaNFs, fora da pasta que o Velopack substitui a cada update.
  - Opcional: -CreateGitHubRelease sobe a release com gh.

.EXAMPLE
  .\scripts\release.ps1
  .\scripts\release.ps1 -Version 1.0.1 -CreateGitHubRelease
#>
param(
    [string] $Version = "",
    [switch] $CreateGitHubRelease
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$proj = Join-Path $root "ConferenciaNFs\ConferenciaNFs.csproj"
$publishDir = Join-Path $root "artifacts\publish"
$releasesDir = Join-Path $root "artifacts\Releases"
$icon = Join-Path $root "ConferenciaNFs\Assets\app.ico"
$packId = "ConferenciaNFs"
$mainExe = "ConferenciaNFs.exe"

function Get-ProjectVersion {
    [xml] $xml = Get-Content $proj
    $ver = $xml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($ver)) {
        throw "Version nao encontrada em $proj"
    }
    return $ver.Trim()
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = Get-ProjectVersion
}

Write-Host "=== ConferenciaNFs release $Version ===" -ForegroundColor Cyan

if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
if (Test-Path $releasesDir) { Remove-Item $releasesDir -Recurse -Force }
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
New-Item -ItemType Directory -Path $releasesDir -Force | Out-Null

Write-Host "Publicando (self-contained win-x64)..."
dotnet publish $proj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:Version=$Version `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish falhou." }

# Nao distribuir settings locais de dev. O pacote leva so o .example: o app grava em
# %AppData%\ConferenciaNFs e usa o exemplo apenas como ponto de partida na 1a execucao.
# Embutir app-settings.json aqui era o que zerava a configuracao a cada atualizacao.
$exampleSettings = Join-Path $root "ConferenciaNFs\app-settings.example.json"
$publishSettings = Join-Path $publishDir "app-settings.json"
$publishExample = Join-Path $publishDir "app-settings.example.json"

if (Test-Path $publishSettings) {
    Remove-Item $publishSettings -Force
}

if (-not (Test-Path $exampleSettings)) {
    throw "app-settings.example.json nao encontrado."
}

Copy-Item $exampleSettings $publishExample -Force
Write-Host "Pacote sem app-settings.json (configuracoes ficam em %AppData%\ConferenciaNFs)."

$vpk = Get-Command vpk -ErrorAction SilentlyContinue
if (-not $vpk) {
    Write-Host "Instalando ferramenta global vpk..."
    dotnet tool install -g vpk
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" +
                [System.Environment]::GetEnvironmentVariable("Path", "User")
}

Write-Host "Empacotando com Velopack..."
$packArgs = @(
    "pack",
    "--packId", $packId,
    "--packVersion", $Version,
    "--packDir", $publishDir,
    "--mainExe", $mainExe,
    "--packTitle", "Conferencia NFs",
    "--outputDir", $releasesDir
)
if (Test-Path $icon) {
    $packArgs += @("--icon", $icon)
}

& vpk @packArgs
if ($LASTEXITCODE -ne 0) { throw "vpk pack falhou." }

Write-Host ""
Write-Host "Pacotes em: $releasesDir" -ForegroundColor Green
Get-ChildItem $releasesDir | Select-Object Name, Length, LastWriteTime | Format-Table -AutoSize

if ($CreateGitHubRelease) {
    $gh = Get-Command gh -ErrorAction SilentlyContinue
    if (-not $gh) { throw "GitHub CLI (gh) nao encontrado." }

    $tag = "v$Version"
    Write-Host "Criando GitHub Release $tag ..."
    $assets = Get-ChildItem $releasesDir -File | ForEach-Object { $_.FullName }

    $notes = @"
## ConferenciaNFs $Version

Instale com o Setup gerado pelo Velopack (ou use o portable, se disponivel nesta pasta).

1. Baixe o instalador desta release
2. Execute e conclua a instalacao
3. As configuracoes ficam em ``%AppData%\ConferenciaNFs\app-settings.json`` e passam a sobreviver as atualizacoes

Com o Setup/Portable Velopack, o app verifica novas releases no GitHub ao abrir.
"@

    gh release create $tag @assets --title "ConferenciaNFs $Version" --notes $notes
    if ($LASTEXITCODE -ne 0) { throw "gh release create falhou." }
    Write-Host "Release publicada: https://github.com/MadTitanPHDev/Conferencia/releases/tag/$tag" -ForegroundColor Green
}

Write-Host "Concluido."
