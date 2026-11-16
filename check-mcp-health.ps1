Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

& (Join-Path $PSScriptRoot 'mcp-server\check-mcp-health.ps1')
exit $LASTEXITCODE
