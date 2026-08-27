param(
    [Parameter(Mandatory)]
    [string]$Title,

    [string]$ProjectRoot = 'D:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022',

    [string]$TaskId = (Get-Date -Format 'yyyyMMdd-HHmmss')
)

if (-not (Test-Path -LiteralPath $ProjectRoot -PathType Container)) {
    throw "Project root not found: $ProjectRoot"
}

$taskDirectory = Join-Path $ProjectRoot 'docs\ai-tasks'
$taskPath = Join-Path $taskDirectory "$TaskId-task.md"

if (Test-Path -LiteralPath $taskPath) {
    throw "Task file already exists: $taskPath"
}

New-Item -ItemType Directory -Force -Path $taskDirectory | Out-Null

$template = @"
# $Title

## Objective

## Current Facts
-

## Read First
-

## Work
1.

## Do Not Change
-

## Verify
-

## Handoff
- Changed paths: pending
- Verification: pending
- Next task: pending
"@

Set-Content -LiteralPath $taskPath -Value $template -Encoding utf8
Write-Output $taskPath
