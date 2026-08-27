param(
    [Parameter(Mandatory)]
    [string]$Title,

    [ValidateSet('AUTO', 'CLINE', 'CC', 'CODEX')]
    [string]$Owner = 'AUTO',

    [string]$ProjectRoot = 'D:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022',

    [string]$TaskId
)

if (-not (Test-Path -LiteralPath $ProjectRoot -PathType Container)) {
    throw "Project root not found: $ProjectRoot"
}

$taskDirectory = Join-Path $ProjectRoot 'docs\ai-tasks'

function New-TaskSlug {
    param([Parameter(Mandatory)][string]$Value)

    $slug = $Value.ToLowerInvariant() -replace '[^a-z0-9]+', '-'
    $slug = $slug.Trim('-')

    if ([string]::IsNullOrWhiteSpace($slug)) {
        return 'task'
    }

    return $slug
}

function Get-NextTaskNumber {
    param(
        [Parameter(Mandatory)][string]$Directory,
        [Parameter(Mandatory)][string]$Prefix
    )

    if (-not (Test-Path -LiteralPath $Directory -PathType Container)) {
        return 1
    }

    $max = 0
    Get-ChildItem -LiteralPath $Directory -File -Filter "$Prefix-*.md" | ForEach-Object {
        if ($_.Name -match "^$Prefix-(\d{3})-") {
            $number = [int]$Matches[1]
            if ($number -gt $max) {
                $max = $number
            }
        }
    }

    return ($max + 1)
}

function Get-TaskOwnerFromFileName {
    param([Parameter(Mandatory)][string]$FileName)

    if ($FileName -match '^(CLINE|CC|CODEX)-') {
        return $Matches[1].ToUpperInvariant()
    }

    return $null
}

$status = 'draft'
$ownershipNoteLine = '- Ownership: normal'

if ([string]::IsNullOrWhiteSpace($TaskId)) {
    $effectiveOwner = if ($Owner -eq 'AUTO') { 'CLINE' } else { $Owner }
    $number = Get-NextTaskNumber -Directory $taskDirectory -Prefix $effectiveOwner
    $taskFileName = '{0}-{1:D3}-{2}.md' -f $effectiveOwner, $number, (New-TaskSlug -Value $Title)
} else {
    $taskFileName = $TaskId
    if (-not $taskFileName.EndsWith('.md', [StringComparison]::OrdinalIgnoreCase)) {
        $taskFileName = "$taskFileName.md"
    }

    $ownerFromName = Get-TaskOwnerFromFileName -FileName $taskFileName

    if ($ownerFromName) {
        $effectiveOwner = $ownerFromName

        if (($Owner -ne 'AUTO') -and ($Owner -ne $ownerFromName)) {
            $status = 'needs-owner-review'
            $ownershipNoteLine = "- Ownership: requested Owner '$Owner' but filename routes to '$ownerFromName'; normalized Owner to '$ownerFromName'."
            Write-Warning $ownershipNoteLine.TrimStart('- ')
        }
    } else {
        $effectiveOwner = if ($Owner -eq 'AUTO') { 'CLINE' } else { $Owner }
        $taskFileName = "$effectiveOwner-$taskFileName"
        $status = 'needs-owner-review'
        $ownershipNoteLine = "- Ownership: filename had no owner prefix; normalized filename with '$effectiveOwner-'."
        Write-Warning $ownershipNoteLine.TrimStart('- ')
    }
}

$taskPath = Join-Path $taskDirectory $taskFileName

if (Test-Path -LiteralPath $taskPath) {
    throw "Task file already exists: $taskPath"
}

New-Item -ItemType Directory -Force -Path $taskDirectory | Out-Null

$template = @"
# $Title

Owner: $effectiveOwner
Project: $ProjectRoot
Status: $status

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
$ownershipNoteLine
- Changed paths: pending
- Verification: pending
- Next task: pending
"@

Set-Content -LiteralPath $taskPath -Value $template -Encoding utf8
Write-Output $taskPath
