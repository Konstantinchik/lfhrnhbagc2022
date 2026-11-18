---
name: darktree-unity-project
description: "Maintain the DarkTreeFPS2022 Unity project, including its local Unity MCP workflow. Use for gameplay, scenes, assets, editor scripts, and project automation in this repository."
---

# DarkTree Unity Project

Work in `D:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022` unless the user explicitly names a different target.

## Project Rules

- Treat this as a Unity 2022.3.15f1 project. Preserve Unity `.meta` files and asset GUID relationships; never regenerate or delete them to fix an import issue.
- Inspect the surrounding scripts, scene, and existing project documentation before changing gameplay behavior. Keep changes scoped to the requested feature or defect.
- Treat command and filesystem output as observed facts only after a tool or command returns. Never fabricate directory listings, placeholder paths, or example file names as if they were real project state.
- When presenting filesystem names, append `\` to directory names and leave file names unchanged. For example: `Assets\`, `ProjectSettings\`, `.clinerules`.
- For one-off Unity Console messages, use MCP `unity_debug_log` or command `DebugLog`; do not create scripts, GameObjects, or assets for `Debug.Log(...)` requests.
- Use `docs/ai-tasks/` for durable AI task handoffs. `CLINE-` belongs to Cline, `CC-` belongs to Claude Code, and `CODEX-` belongs to Codex; the task file's `Owner:` field should match the filename prefix. If `Status: needs-owner-review` is present, confirm ownership before execution.
- The project has an HTTP Unity Editor bridge on port `7777` and a Node stdio MCP server in `mcp-server`. For editor or scene changes, first check the bridge connection with `D:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022\mcp-server\connect-to-unity.ps1`. Read [the MCP reference](references/unity-mcp.md) when using that workflow.
- Prefer C# and serialized Unity asset changes for durable gameplay behavior. Use MCP for editor operations such as scene inspection, object creation, import, animation retargeting, and reading Unity console output.
- Do not assume Unity is running. If the bridge is unavailable, report that editor-side commands cannot be verified; continue with safe code inspection or edits where possible.

## Working Flow

1. Read project-local MCP documentation and the relevant scripts or assets. Use `rg` to locate types, prefabs, scenes, and references.
2. For Unity Editor changes, verify `unity_check_connection`, inspect the current scene, then make the smallest requested mutation.
3. For C# changes, account for Unity serialization and lifecycle methods. Avoid renaming serialized fields, types, or assets without an explicit migration plan.
4. For gameplay bug fixes and algorithm changes, use TDD whenever the behavior can be tested. Store test code in `Assets/Tests/`, not beside gameplay scripts or in `Library/PackageCache`. **`Dummy` in the `Demo Zone` scene is the project's permanent, authorized test object:** preserve it in the scene, save and commit its test-component setup as needed, and attach test `MonoBehaviour` components to it for any gameplay/system verification. This is an explicit exception to the normal preference for disposable test objects; do not remove or repurpose `Dummy` without the user's direction. Persistent test components must be inert in player builds or explicitly editor-only, and must not interfere with normal gameplay. The harness/session must capture **all** Unity Console messages (`Debug.Log`, `Debug.LogWarning`, `Debug.LogError`, and other log types) with timestamps and stack traces to a readable file under `Temp/UnityTestLogs/`; use `Application.logMessageReceived` and report the exact log path in the test output. Include explicit test start/end markers and a clear `PASS` or `FAIL`. For gameplay-event verification, instrument the actual event trigger/outcome across the relevant mechanics (e.g. zone enter/exit and damage, pickup, fire/hit/reload, NPC detection/state change, inventory/build action, save/load), recording useful context and state before/after. Log event transitions/outcomes, not every `Update` frame; avoid duplicate/high-volume logs and keep diagnostics Editor-only or behind an explicit diagnostic switch where appropriate. After the user runs Play Mode in the already-open Editor, read and interpret the log file directly instead of relying on live Console access, screenshots, or asking the user to relay messages. Keep generated log files out of version control (`Temp/` is disposable); the persistent `Dummy` and its intentional test component are allowed scene content. Do not create/destroy gameplay objects in a runtime test unless their lifecycle is isolated and proven not to affect the scene. Do not start a second Unity Editor/batch process while the project is open. If an isolated NUnit/EditMode test is used, ensure its assembly setup is valid and do not put editor-only/NUnit scripts in runtime compilation folders. If Unity or the bridge cannot run the test or the log cannot be read, report the exact blocker and do not claim verification.
5. Verify according to the changed surface: Unity console logs and scene state for editor actions; the MCP server build for MCP changes; focused project tests or compilation when available.
6. State what was changed, what was verified, and any editor-side condition that still needs the open Unity project.

## Local Utilities

`D:\FREE_AGENT_LM\scripts\check-env.ps1` checks LM Studio, the MCP server build, and the Unity bridge. Use it for environment diagnosis, not as a prerequisite for unrelated source-code work.
`D:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022\mcp-server\connect-to-unity.ps1` is the canonical Unity MCP bridge check for Cline, Claude Code, and Codex. `D:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022\mcp-server\check-mcp-health.ps1` is a compatibility alias that delegates to it. If it reports that the bridge is unreachable, do not claim editor-side MCP work succeeded.
