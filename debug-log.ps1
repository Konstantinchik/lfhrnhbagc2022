param(
    [Parameter(Mandatory)][string]$Message,
    [ValidateSet('log', 'warning', 'error')][string]$Level = 'log'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

& (Join-Path $PSScriptRoot 'mcp-server\debug-log.ps1') -Message $Message -Level $Level
exit $LASTEXITCODE
