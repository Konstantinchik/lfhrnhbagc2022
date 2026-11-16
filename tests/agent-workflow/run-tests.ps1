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
    $debugScriptPath = Join-Path $ProjectRoot 'mcp-server\debug-log.ps1'
    $rootDebugScriptPath = Join-Path $ProjectRoot 'debug-log.ps1'
    $rules = Get-Content -Raw -Encoding UTF8 -LiteralPath $rulesPath
    $debugScript = Get-Content -Raw -Encoding UTF8 -LiteralPath $debugScriptPath
    $rootDebugScript = Get-Content -Raw -Encoding UTF8 -LiteralPath $rootDebugScriptPath

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
    Assert-TextContains $rules 'mcp-server\connect-to-unity.ps1' '.clinerules must name the canonical Unity MCP connection check.'
    Assert-TextContains $rules 'mcp-server\check-mcp-health.ps1' '.clinerules must name the Unity MCP health alias.'
    Assert-TextContains $rules 'Do not invent alternative helper scripts' '.clinerules must forbid invented Unity MCP helper scripts.'
    Assert-TextContains $rules 'responded but health failed' '.clinerules must explain unhealthy Unity MCP bridge responses.'
}

Invoke-Test 'Unity MCP connection wrappers are present and deterministic' {
    $mcpConnectPath = Join-Path $ProjectRoot 'mcp-server\connect-to-unity.ps1'
    $rootConnectPath = Join-Path $ProjectRoot 'connect-to-unity.ps1'
    $mcpHealthPath = Join-Path $ProjectRoot 'mcp-server\check-mcp-health.ps1'
    $rootHealthPath = Join-Path $ProjectRoot 'check-mcp-health.ps1'

    Assert-True -Condition (Test-Path -LiteralPath $mcpConnectPath -PathType Leaf) 'mcp-server\connect-to-unity.ps1 must exist.'
    Assert-True -Condition (Test-Path -LiteralPath $rootConnectPath -PathType Leaf) 'connect-to-unity.ps1 must exist.'
    Assert-True -Condition (Test-Path -LiteralPath $mcpHealthPath -PathType Leaf) 'mcp-server\check-mcp-health.ps1 must exist.'
    Assert-True -Condition (Test-Path -LiteralPath $rootHealthPath -PathType Leaf) 'check-mcp-health.ps1 must exist.'

    $mcpConnect = Get-Content -Raw -Encoding UTF8 -LiteralPath $mcpConnectPath
    $rootConnect = Get-Content -Raw -Encoding UTF8 -LiteralPath $rootConnectPath
    $mcpHealth = Get-Content -Raw -Encoding UTF8 -LiteralPath $mcpHealthPath
    $rootHealth = Get-Content -Raw -Encoding UTF8 -LiteralPath $rootHealthPath
    $skill = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $ProjectRoot '.agents\skills\darktree-unity-project\SKILL.md')

    Assert-TextContains $mcpConnect 'UNITY_EDITOR_HOST' 'Unity connection script must honor UNITY_EDITOR_HOST.'
    Assert-TextContains $mcpConnect 'UNITY_EDITOR_PORT' 'Unity connection script must honor UNITY_EDITOR_PORT.'
    Assert-TextContains $mcpConnect '7777' 'Unity connection script must default to bridge port 7777.'
    Assert-TextContains $mcpConnect 'Unity MCP Bridge OK' 'Unity connection script must print a clear success line.'
    Assert-TextContains $mcpConnect 'Unity MCP Bridge is not reachable' 'Unity connection script must print a clear unreachable line.'
    Assert-TextContains $mcpConnect 'Unity MCP Bridge responded but health failed' 'Unity connection script must print a clear unhealthy bridge line.'
    Assert-TextContains $mcpConnect 'dist\index.js' 'Unity connection script must verify the MCP server build.'
    Assert-TextContains $mcpConnect 'Read-WebErrorResponseBody' 'Unity connection script must report HTTP error bodies on Windows PowerShell 5.1.'
    Assert-TextContains $mcpConnect 'UseBasicParsing' 'Unity connection script must work in Windows PowerShell 5.1 without Internet Explorer initialization.'
    Assert-True -Condition ($mcpConnect -notmatch 'SkipHttpErrorCheck') 'Unity connection script must be compatible with Windows PowerShell 5.1.'
    Assert-TextContains $mcpConnect '$_.Exception.PSObject.Properties[''Response'']' 'connect-to-unity.ps1 must tolerate exceptions without Response.'
    Assert-TextContains $rootConnect 'mcp-server\connect-to-unity.ps1' 'Root wrapper must delegate to the canonical MCP connection script.'
    Assert-TextContains $mcpHealth 'connect-to-unity.ps1' 'MCP health alias must delegate to the canonical MCP connection script.'
    Assert-TextContains $rootHealth 'mcp-server\check-mcp-health.ps1' 'Root health alias must delegate to the MCP health alias.'
    Assert-TextContains $skill 'mcp-server\connect-to-unity.ps1' 'Project skill must name the canonical Unity MCP connection check.'
}
Invoke-Test 'Agent skills document owner-prefix workflow' {
    $skillPaths = @(
        Join-Path $ProjectRoot '.agents\skills\darktree-unity-project\SKILL.md'
        Join-Path $ProjectRoot '.agents\skills\qwen-coder-12k-workflow\SKILL.md'
        Join-Path $ProjectRoot '.agents\skills\qwen-coder-12k-workflow\references\task-file.md'
    )

    $skillText = ($skillPaths | ForEach-Object { Get-Content -Raw -Encoding UTF8 -LiteralPath $_ }) -join "`n"

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
        $autoContent = Get-Content -Raw -Encoding UTF8 -LiteralPath $autoPath
        Assert-TextMatches $autoContent '(?m)^Owner: CLINE$' 'AUTO task must write Owner: CLINE.'
        Assert-TextMatches $autoContent '(?m)^Status: draft$' 'AUTO task should be draft.'

        $ccPath = & $taskScript -ProjectRoot $tempRoot -Owner CC -Title 'Unity MCP diagnostics'
        Assert-True -Condition ((Split-Path -Leaf $ccPath) -eq 'CC-001-unity-mcp-diagnostics.md') 'Explicit CC owner should create a CC task.'
        $ccContent = Get-Content -Raw -Encoding UTF8 -LiteralPath $ccPath
        Assert-TextMatches $ccContent '(?m)^Owner: CC$' 'CC task must write Owner: CC.'

        $conflictPath = & $taskScript -ProjectRoot $tempRoot -Owner CC -TaskId 'CLINE-999-wrong-owner' -Title 'Mismatch test' 3>$null
        Assert-True -Condition ((Split-Path -Leaf $conflictPath) -eq 'CLINE-999-wrong-owner.md') 'Conflicting TaskId should keep the visible filename route.'
        $conflictContent = Get-Content -Raw -Encoding UTF8 -LiteralPath $conflictPath
        Assert-TextMatches $conflictContent '(?m)^Owner: CLINE$' 'Conflict must normalize Owner to the filename prefix.'
        Assert-TextMatches $conflictContent '(?m)^Status: needs-owner-review$' 'Conflict must be marked needs-owner-review.'
        Assert-TextContains $conflictContent "requested Owner 'CC' but filename routes to 'CLINE'" 'Conflict must record an ownership note.'

        $noPrefixPath = & $taskScript -ProjectRoot $tempRoot -Owner CODEX -TaskId 'manual-task' -Title 'Manual task' 3>$null
        Assert-True -Condition ((Split-Path -Leaf $noPrefixPath) -eq 'CODEX-manual-task.md') 'TaskId without a prefix should be normalized with the requested owner.'
        $noPrefixContent = Get-Content -Raw -Encoding UTF8 -LiteralPath $noPrefixPath
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
        Where-Object { (Get-Content -Raw -Encoding UTF8 -LiteralPath $_.FullName) -match '(?m)^Status:\s*needs-owner-review\s*$' }

    Assert-True -Condition (($unresolved | Measure-Object).Count -eq 0) ("Resolve ownership before commit: " + (($unresolved | Select-Object -ExpandProperty Name) -join ', '))
}

Invoke-Test 'Unity MCP health endpoint avoids Unity API on listener threads' {
    $bridgePath = Join-Path $ProjectRoot 'Assets\DarkTree FPS\Scripts\Editor\MCPBridge\MCPBridgeServer.cs'
    $bridge = Get-Content -Raw -Encoding UTF8 -LiteralPath $bridgePath
    $healthStart = $bridge.IndexOf('private static string HandleHealthCheck()')
    $healthEnd = $bridge.IndexOf('private static string HandleExecuteCommand', $healthStart)
    Assert-True -Condition ($healthStart -ge 0) 'MCPBridgeServer.cs must contain HandleHealthCheck.'
    Assert-True -Condition ($healthEnd -gt $healthStart) 'MCPBridgeServer.cs must keep HandleHealthCheck before HandleExecuteCommand.'
    $healthBlock = $bridge.Substring($healthStart, $healthEnd - $healthStart)
    Assert-TextContains $healthBlock 'DarkTreeFPS MCP Bridge' 'Health check must identify the bridge without using Unity project APIs.'
    Assert-True -Condition ($healthBlock -notmatch 'Application\.') 'Health check must not call UnityEngine.Application from the HTTP listener thread.'
}
Invoke-Test 'Unity Console logging uses direct DebugLog command' {
    $executorPath = Join-Path $ProjectRoot 'Assets\DarkTree FPS\Scripts\Editor\MCPBridge\UnityCommandExecutor.cs'
    $mcpIndexPath = Join-Path $ProjectRoot 'mcp-server\src\index.ts'
    $rulesPath = Join-Path $ProjectRoot '.clinerules'
    $debugScriptPath = Join-Path $ProjectRoot 'mcp-server\debug-log.ps1'
    $rootDebugScriptPath = Join-Path $ProjectRoot 'debug-log.ps1'

    $executor = Get-Content -Raw -Encoding UTF8 -LiteralPath $executorPath
    $mcpIndex = Get-Content -Raw -Encoding UTF8 -LiteralPath $mcpIndexPath
    $rules = Get-Content -Raw -Encoding UTF8 -LiteralPath $rulesPath
    $debugScript = Get-Content -Raw -Encoding UTF8 -LiteralPath $debugScriptPath
    $rootDebugScript = Get-Content -Raw -Encoding UTF8 -LiteralPath $rootDebugScriptPath

    Assert-TextContains $executor 'case "DebugLog":' 'Unity command executor must route DebugLog.'
    Assert-TextContains $executor 'Debug.Log(message);' 'DebugLog command must call Debug.Log(message).'
    Assert-TextContains $executor 'Debug.LogWarning(message);' 'DebugLog command must support warnings.'
    Assert-TextContains $executor 'Debug.LogError(message);' 'DebugLog command must support errors.'
    Assert-TextContains $mcpIndex 'unity_debug_log' 'MCP server must expose unity_debug_log.'
    Assert-TextContains $mcpIndex 'executeCommand(''DebugLog''' 'unity_debug_log must delegate to DebugLog.'
    Assert-TextContains $rules 'Do not create `MonoBehaviour` scripts' '.clinerules must forbid scripts for one-time log messages.'
    Assert-TextContains $rules 'Debug.Log(' '.clinerules must document the direct Debug.Log intent mapping.'
    Assert-TextContains $rules 'LogMessage.cs' '.clinerules must explicitly reject the script created for the bad log workflow.'
    Assert-True -Condition (Test-Path -LiteralPath $debugScriptPath -PathType Leaf) 'mcp-server\debug-log.ps1 must exist.'
    Assert-True -Condition (Test-Path -LiteralPath $rootDebugScriptPath -PathType Leaf) 'debug-log.ps1 must exist.'
    Assert-TextContains $debugScript 'DebugLog' 'debug-log.ps1 must call the DebugLog command.'
    Assert-TextContains $debugScript 'Unknown command: DebugLog' 'debug-log.ps1 must explain reload when Unity has an old executor loaded.'
    Assert-TextContains $debugScript 'Read-WebErrorResponseBody' 'debug-log.ps1 must print HTTP error bodies.'
    Assert-TextContains $rootDebugScript 'mcp-server\debug-log.ps1' 'Root debug-log.ps1 must delegate to the MCP debug log wrapper.'
}
Invoke-Test 'Unity MCP execute does not report false success on timeout' {
    $bridgePath = Join-Path $ProjectRoot 'Assets\DarkTree FPS\Scripts\Editor\MCPBridge\MCPBridgeServer.cs'
    $bridge = Get-Content -Raw -Encoding UTF8 -LiteralPath $bridgePath
    $executeStart = $bridge.IndexOf('private static string HandleExecuteCommand')
    $executeEnd = $bridge.IndexOf('private static string HandleGetSceneInfo', $executeStart)

    Assert-True -Condition ($executeStart -ge 0) 'MCPBridgeServer.cs must contain HandleExecuteCommand.'
    Assert-True -Condition ($executeEnd -gt $executeStart) 'MCPBridgeServer.cs must keep HandleExecuteCommand before HandleGetSceneInfo.'

    $executeBlock = $bridge.Substring($executeStart, $executeEnd - $executeStart)
    Assert-TextContains $executeBlock 'if (command == "DebugLog")' 'DebugLog must bypass delayed main-thread execution.'
    Assert-TextContains $executeBlock 'object directResult = UnityCommandExecutor.Execute(command, args);' 'DebugLog must execute directly.'
    Assert-TextContains $executeBlock 'bool completed = false;' 'Delayed Unity commands must track completion separately from null result.'
    Assert-TextContains $executeBlock 'finally' 'Delayed Unity commands must mark completion in finally.'
    Assert-TextContains $executeBlock 'Unity command timed out' 'Delayed Unity commands must fail explicitly on timeout.'
    Assert-True -Condition ($executeBlock -notmatch 'while \(result == null && error == null && timeout > 0\)') 'Delayed Unity commands must not use null result as completion signal.'
}
Invoke-Test 'Git diff has no whitespace errors' {
    $git = Get-Command git -ErrorAction SilentlyContinue
    Assert-True -Condition ($null -ne $git) 'git must be available.'

    $output = & git -c "safe.directory=$ProjectRoot" -C $ProjectRoot diff --check 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw ($output -join "`n")
    }
}

Write-Host ""
Write-Host "Agent workflow tests: $passed passed, $failed failed."

if ($failed -gt 0) {
    exit 1
}
