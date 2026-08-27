Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ProjectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$passed = 0
$failed = 0

function Assert-True {
    param(
        [Parameter(Mandatory)][bool]$Condition,
        [Parameter(Mandatory)][string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Assert-TextContains {
    param(
        [Parameter(Mandatory)][string]$Text,
        [Parameter(Mandatory)][string]$Needle,
        [Parameter(Mandatory)][string]$Message
    )

    Assert-True -Condition $Text.Contains($Needle) -Message $Message
}

function Assert-TextMatches {
    param(
        [Parameter(Mandatory)][string]$Text,
        [Parameter(Mandatory)][string]$Pattern,
        [Parameter(Mandatory)][string]$Message
    )

    Assert-True -Condition ($Text -match $Pattern) -Message $Message
}

function Invoke-Test {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][scriptblock]$Body
    )

    try {
        & $Body
        $script:passed += 1
        Write-Host "[PASS] $Name" -ForegroundColor Green
    } catch {
        $script:failed += 1
        Write-Host "[FAIL] $Name" -ForegroundColor Red
        Write-Host "       $($_.Exception.Message)" -ForegroundColor Red
    }
}

Invoke-Test 'Cline rules pin project root and task ownership' {
    $rulesPath = Join-Path $ProjectRoot '.clinerules'
    $rules = Get-Content -Raw -LiteralPath $rulesPath

    Assert-TextContains $rules 'D:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022' '.clinerules must pin the project root.'
    Assert-TextContains $rules 'Answer in Russian' '.clinerules must require Russian dialogue.'
    Assert-TextContains $rules 'Set-Location -LiteralPath' '.clinerules must require explicit project-root shell location.'
    Assert-TextContains $rules 'Do not run a bare `dir`' '.clinerules must reject bare directory commands.'
    Assert-TextContains $rules 'Never invent, simulate, summarize from memory, or use placeholder names for command output.' '.clinerules must forbid fabricated command output.'
    Assert-TextContains $rules 'pending user approval' '.clinerules must distinguish pending commands from completed output.'
    Assert-TextContains $rules 'имя_папки_1' '.clinerules must explicitly reject placeholder folder names.'
    Assert-TextContains $rules 'файл_1.txt' '.clinerules must explicitly reject placeholder file names.'
    Assert-TextContains $rules 'append `\` to directory names' '.clinerules must require a trailing backslash for directory names.'
    Assert-TextContains $rules '@{Name=''Name'';Expression={ if ($_.PSIsContainer)' '.clinerules must use a deterministic formatted listing command.'
    Assert-TextContains $rules 'docs\ai-tasks\' '.clinerules must name the durable task folder.'
    Assert-TextContains $rules 'CLINE-' '.clinerules must document CLINE ownership.'
    Assert-TextContains $rules 'CC-' '.clinerules must document CC ownership.'
    Assert-TextContains $rules 'CODEX-' '.clinerules must document CODEX ownership.'
    Assert-TextContains $rules 'needs-owner-review' '.clinerules must pause ambiguous ownership.'
}

Invoke-Test 'Agent skills document owner-prefix workflow' {
    $skillPaths = @(
        Join-Path $ProjectRoot '.agents\skills\darktree-unity-project\SKILL.md'
        Join-Path $ProjectRoot '.agents\skills\qwen-coder-12k-workflow\SKILL.md'
        Join-Path $ProjectRoot '.agents\skills\qwen-coder-12k-workflow\references\task-file.md'
    )

    $skillText = ($skillPaths | ForEach-Object { Get-Content -Raw -LiteralPath $_ }) -join "`n"

    Assert-TextContains $skillText 'CLINE-NNN-kebab-title.md' 'Task format must include CLINE file naming.'
    Assert-TextContains $skillText 'CC-NNN-kebab-title.md' 'Task format must include CC file naming.'
    Assert-TextContains $skillText 'CODEX-NNN-kebab-title.md' 'Task format must include CODEX file naming.'
    Assert-TextContains $skillText 'Owner:' 'Task format must include an Owner field.'
    Assert-TextContains $skillText 'needs-owner-review' 'Task format must describe recoverable ownership conflicts.'
    Assert-TextContains $skillText 'append `\` to directory names' 'Skills must require a trailing backslash for directory names.'
}

Invoke-Test 'Task generator creates recoverable owner-routed handoffs' {
    $taskScript = Join-Path $ProjectRoot '.agents\skills\qwen-coder-12k-workflow\scripts\new-qwen-task.ps1'
    $tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('darktree-agent-workflow-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null

    try {
        $autoPath = & $taskScript -ProjectRoot $tempRoot -Title 'Auto default task'
        Assert-True -Condition ((Split-Path -Leaf $autoPath) -eq 'CLINE-001-auto-default-task.md') 'AUTO owner should create a CLINE task by default.'
        $autoContent = Get-Content -Raw -LiteralPath $autoPath
        Assert-TextMatches $autoContent '(?m)^Owner: CLINE$' 'AUTO task must write Owner: CLINE.'
        Assert-TextMatches $autoContent '(?m)^Status: draft$' 'AUTO task should be draft.'

        $ccPath = & $taskScript -ProjectRoot $tempRoot -Owner CC -Title 'Unity MCP diagnostics'
        Assert-True -Condition ((Split-Path -Leaf $ccPath) -eq 'CC-001-unity-mcp-diagnostics.md') 'Explicit CC owner should create a CC task.'
        $ccContent = Get-Content -Raw -LiteralPath $ccPath
        Assert-TextMatches $ccContent '(?m)^Owner: CC$' 'CC task must write Owner: CC.'

        $conflictPath = & $taskScript -ProjectRoot $tempRoot -Owner CC -TaskId 'CLINE-999-wrong-owner' -Title 'Mismatch test' 3>$null
        Assert-True -Condition ((Split-Path -Leaf $conflictPath) -eq 'CLINE-999-wrong-owner.md') 'Conflicting TaskId should keep the visible filename route.'
        $conflictContent = Get-Content -Raw -LiteralPath $conflictPath
        Assert-TextMatches $conflictContent '(?m)^Owner: CLINE$' 'Conflict must normalize Owner to the filename prefix.'
        Assert-TextMatches $conflictContent '(?m)^Status: needs-owner-review$' 'Conflict must be marked needs-owner-review.'
        Assert-TextContains $conflictContent "requested Owner 'CC' but filename routes to 'CLINE'" 'Conflict must record an ownership note.'

        $noPrefixPath = & $taskScript -ProjectRoot $tempRoot -Owner CODEX -TaskId 'manual-task' -Title 'Manual task' 3>$null
        Assert-True -Condition ((Split-Path -Leaf $noPrefixPath) -eq 'CODEX-manual-task.md') 'TaskId without a prefix should be normalized with the requested owner.'
        $noPrefixContent = Get-Content -Raw -LiteralPath $noPrefixPath
        Assert-TextMatches $noPrefixContent '(?m)^Owner: CODEX$' 'No-prefix task must write the normalized owner.'
        Assert-TextMatches $noPrefixContent '(?m)^Status: needs-owner-review$' 'No-prefix normalization must request ownership review.'
    } finally {
        $resolvedTemp = (Resolve-Path -LiteralPath $tempRoot).Path
        $tempBase = [System.IO.Path]::GetTempPath()
        if ($resolvedTemp.StartsWith($tempBase, [System.StringComparison]::OrdinalIgnoreCase)) {
            Remove-Item -LiteralPath $resolvedTemp -Recurse -Force
        }
    }
}

Invoke-Test 'No unresolved task ownership review is committed' {
    $taskDir = Join-Path $ProjectRoot 'docs\ai-tasks'

    if (-not (Test-Path -LiteralPath $taskDir -PathType Container)) {
        return
    }

    $unresolved = Get-ChildItem -LiteralPath $taskDir -File -Filter '*.md' |
        Where-Object { (Get-Content -Raw -LiteralPath $_.FullName) -match '(?m)^Status:\s*needs-owner-review\s*$' }

    Assert-True -Condition (($unresolved | Measure-Object).Count -eq 0) ("Resolve ownership before commit: " + (($unresolved | Select-Object -ExpandProperty Name) -join ', '))
}

Invoke-Test 'Git diff has no whitespace errors' {
    $git = Get-Command git -ErrorAction SilentlyContinue
    Assert-True -Condition ($null -ne $git) 'git must be available.'

    $output = & git -C $ProjectRoot diff --check 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw ($output -join "`n")
    }
}

Write-Host ""
Write-Host "Agent workflow tests: $passed passed, $failed failed."

if ($failed -gt 0) {
    exit 1
}
