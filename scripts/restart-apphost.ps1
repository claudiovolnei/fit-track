# Reinicia o AppHost do FitTrack com a versao recem-compilada.
#
# Uso:  pwsh -File restart-apphost.ps1 [-Build]
#   -Build  compila a solucao antes de subir (recomendado apos mudancas).
#
# Por que isto existe: o AppHost em execucao serve a build que estava no disco quando
# subiu. Apos um `dotnet build`, o index.html e os _framework/*.js mudam de fingerprint,
# e o processo antigo continua servindo a versao velha (404 em _framework/... e o app
# nao abre). Matar e subir de novo garante que a nova versao seja a servida.
#
# Sempre use este script (ou equivalente) apos implementar algo novo.

param(
    [switch] $Build
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
# se o script estiver em /scripts, sobe um nivel
if (Test-Path (Join-Path $root 'FitTrack.slnx')) { } else { $root = Split-Path -Parent $root }
Set-Location $root

Write-Output "=== parando AppHost/API ==="
Get-Process 'FitTrack.AppHost','FitTrack.Adapters.In.Api' -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 3

if ($Build) {
    Write-Output "=== build ==="
    dotnet build FitTrack.slnx --nologo
    if ($LASTEXITCODE -ne 0) { throw "build falhou" }
}

Write-Output "=== subindo AppHost ==="
$env:ASPIRE_ALLOW_UNSECURED_TRANSPORT = "true"
Start-Process -FilePath "dotnet" `
    -ArgumentList "run","--project","src/FitTrack.AppHost","--launch-profile","http" `
    -WorkingDirectory $root -WindowStyle Hidden

$deadline = (Get-Date).AddSeconds(180)
while ((Get-Date) -lt $deadline) {
    $w = Get-NetTCPConnection -LocalPort 5090 -State Listen -ErrorAction SilentlyContinue
    $a = Get-NetTCPConnection -LocalPort 5080 -State Listen -ErrorAction SilentlyContinue
    if ($w -and $a) { Write-Output "web (5090) + api (5080) no ar"; break }
    Start-Sleep -Seconds 5
}

if (-not (Get-NetTCPConnection -LocalPort 5090 -State Listen -ErrorAction SilentlyContinue)) {
    throw "web nao subiu em 5090"
}
if (-not (Get-NetTCPConnection -LocalPort 5080 -State Listen -ErrorAction SilentlyContinue)) {
    throw "api nao subiu em 5080"
}

Write-Output "=== pronto: http://localhost:5090  |  api http://localhost:5080 ==="