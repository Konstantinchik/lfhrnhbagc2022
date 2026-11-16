Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

& (Join-Path $PSScriptRoot 'connect-to-unity.ps1')
exit $LASTEXITCODE
