---
name: darktree-unity-project
description: "Maintain the DarkTreeFPS2022 Unity project, including its local Unity MCP workflow. Use for gameplay, scenes, assets, editor scripts, and project automation in this repository."
---

# DarkTree Unity Project

Work in `D:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022` unless the user explicitly names a different target.

## Project Rules

- Treat this as a Unity 2022.3.15f1 project. Preserve Unity `.meta` files and asset GUID relationships; never regenerate or delete them to fix an import issue.
- Inspect the surrounding scripts, scene, and existing project documentation before changing gameplay behavior. Keep changes scoped to the requested feature or defect.
- The project has an HTTP Unity Editor bridge on port `7777` and a Node stdio MCP server in `mcp-server`. For editor or scene changes, first check the bridge connection. Read [the MCP reference](references/unity-mcp.md) when using that workflow.
- Prefer C# and serialized Unity asset changes for durable gameplay behavior. Use MCP for editor operations such as scene inspection, object creation, import, animation retargeting, and reading Unity console output.
- Do not assume Unity is running. If the bridge is unavailable, report that editor-side commands cannot be verified; continue with safe code inspection or edits where possible.

## Working Flow

1. Read project-local MCP documentation and the relevant scripts or assets. Use `rg` to locate types, prefabs, scenes, and references.
2. For Unity Editor changes, verify `unity_check_connection`, inspect the current scene, then make the smallest requested mutation.
3. For C# changes, account for Unity serialization and lifecycle methods. Avoid renaming serialized fields, types, or assets without an explicit migration plan.
4. Verify according to the changed surface: Unity console logs and scene state for editor actions; the MCP server build for MCP changes; focused project tests or compilation when available.
5. State what was changed, what was verified, and any editor-side condition that still needs the open Unity project.

## Local Utilities

`D:\FREE_AGENT_LM\scripts\check-env.ps1` checks LM Studio, the MCP server build, and the Unity bridge. Use it for environment diagnosis, not as a prerequisite for unrelated source-code work.
