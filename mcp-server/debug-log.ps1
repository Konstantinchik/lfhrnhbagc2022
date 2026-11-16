param(
    [Parameter(Mandatory)][string]$Message,
    [ValidateSet('log', 'warning', 'error')][string]$Level = 'log'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$hostName = $env:UNITY_EDITOR_HOST
if ([string]::IsNullOrWhiteSpace($hostName)) {
    $hostName = 'localhost'
}

$port = $env:UNITY_EDITOR_PORT
if ([string]::IsNullOrWhiteSpace($port)) {
    $port = '7777'
}

$executeUrl = "http://${hostName}:${port}/execute"
$payload = @{
    command = 'DebugLog'
    args = @{
        message = $Message
        level = $Level
    }
} | ConvertTo-Json -Depth 5

function Read-WebErrorResponseBody {
    param([Parameter(Mandatory)]$Response)

    $stream = $Response.GetResponseStream()
    if ($null -eq $stream) {
        return ''
    }

    $reader = New-Object System.IO.StreamReader($stream)
    try {
        return $reader.ReadToEnd()
    } finally {
        $reader.Dispose()
        $stream.Dispose()
    }
}

try {
    Invoke-RestMethod -Uri $executeUrl -Method Post -ContentType 'application/json; charset=utf-8' -Body ([System.Text.Encoding]::UTF8.GetBytes($payload)) -TimeoutSec 15 | ConvertTo-Json -Depth 8
} catch {
    Write-Host "Unity DebugLog request failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($null -ne $_.Exception.Response) {
        $body = Read-WebErrorResponseBody -Response $_.Exception.Response
        if (-not [string]::IsNullOrWhiteSpace($body)) {
            Write-Host $body
        }
    }
    Write-Host 'If Unity reports Unknown command: DebugLog, exit Play Mode or restart Unity so editor scripts reload.' -ForegroundColor Yellow
    exit 1
}
