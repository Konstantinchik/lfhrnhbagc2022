Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ProjectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$McpServerRoot = $PSScriptRoot

$hostName = $env:UNITY_EDITOR_HOST
if ([string]::IsNullOrWhiteSpace($hostName)) {
    $hostName = 'localhost'
}

$port = $env:UNITY_EDITOR_PORT
if ([string]::IsNullOrWhiteSpace($port)) {
    $port = '7777'
}

$baseUrl = "http://${hostName}:${port}"
$healthUrl = "$baseUrl/health"
$serverEntry = Join-Path $McpServerRoot 'dist\index.js'

function Convert-ResponseContentToText {
    param([Parameter(Mandatory)]$Content)

    if ($Content -is [byte[]]) {
        return [System.Text.Encoding]::UTF8.GetString($Content)
    }

    return [string]$Content
}

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

function Write-UnhealthyBridge {
    param(
        [Parameter(Mandatory)][int]$StatusCode,
        [Parameter(Mandatory)][string]$StatusDescription,
        [Parameter()][string]$Body = ''
    )

    Write-Host "Unity MCP Bridge responded but health failed: HTTP $StatusCode $StatusDescription" -ForegroundColor Red
    if (-not [string]::IsNullOrWhiteSpace($Body)) {
        Write-Host $Body
    }
    Write-Host "Open Unity Editor with project: $ProjectRoot" -ForegroundColor Yellow
    Write-Host 'Unity menu: Window -> MCP Bridge -> Restart Server' -ForegroundColor Yellow
    Write-Host "Expected endpoint: $healthUrl" -ForegroundColor Yellow
}

Write-Host 'Unity MCP connection check' -ForegroundColor Cyan
Write-Host "Project root: $ProjectRoot"
Write-Host "MCP server: $serverEntry"
Write-Host "Bridge health: $healthUrl"
Write-Host ''

if (-not (Test-Path -LiteralPath $serverEntry -PathType Leaf)) {
    Write-Host 'MCP server build is missing.' -ForegroundColor Red
    Write-Host "Run: npm --prefix `"$McpServerRoot`" install"
    Write-Host "Run: npm --prefix `"$McpServerRoot`" run build"
    exit 1
}

try {
    $response = Invoke-WebRequest -Uri $healthUrl -TimeoutSec 10 -UseBasicParsing
    $body = Convert-ResponseContentToText -Content $response.Content

    if ([int]$response.StatusCode -eq 200) {
        Write-Host 'Unity MCP Bridge OK' -ForegroundColor Green
        $body
        exit 0
    }

    Write-UnhealthyBridge -StatusCode ([int]$response.StatusCode) -StatusDescription ([string]$response.StatusDescription) -Body $body
    exit 1
} catch {
    $webResponse = $null
    if ($_.Exception.PSObject.Properties['Response']) {
        $webResponse = $_.Exception.Response
    }

    if ($null -ne $webResponse) {
        $body = Read-WebErrorResponseBody -Response $webResponse
        Write-UnhealthyBridge -StatusCode ([int]$webResponse.StatusCode) -StatusDescription ([string]$webResponse.StatusDescription) -Body $body
        exit 1
    }

    Write-Host "Unity MCP Bridge is not reachable: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Open Unity Editor with project: $ProjectRoot" -ForegroundColor Yellow
    Write-Host 'Unity menu: Window -> MCP Bridge -> Start Server' -ForegroundColor Yellow
    Write-Host "Expected endpoint: $healthUrl" -ForegroundColor Yellow
    exit 1
}
