# Task File Format

Create task files in `docs/ai-tasks/` with a stable owner prefix and numeric identifier:

- `CLINE-NNN-kebab-title.md` for VS Code Cline work.
- `CC-NNN-kebab-title.md` for Claude Code terminal work.
- `CODEX-NNN-kebab-title.md` for Codex work.

The filename prefix and the `Owner:` field should match. The filename prefix is the visible routing signal. If they disagree, or if `Status: needs-owner-review` is present, clarify ownership before execution.

```markdown
# Player Spawn Placement

Owner: CLINE
Project: D:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022
Status: draft

## Objective
One sentence describing the observable result.

## Current Facts
- Only verified facts about the repository, scene, or console.

## Read First
- `Assets/.../RelevantScript.cs`
- `Assets/.../TargetScene.unity`

## Work
1. The smallest ordered changes needed for this result.

## Do Not Change
- Explicit nearby systems that are outside this task.

## Verify
- A focused compile, test, console check, or Unity MCP inspection.

## Handoff
- Changed paths: pending
- Verification: pending
- Next task: pending
```

When creating tasks with `scripts/new-qwen-task.ps1`, `-Owner AUTO` is the default. With an explicit `-TaskId`, a known filename prefix such as `CC-` or `CODEX-` wins and `Owner:` is normalized to match it. Conflicts are marked as `Status: needs-owner-review` so the workflow remains recoverable.

Use these start prompts:

```text
For Cline:
Выполни docs/ai-tasks/CLINE-NNN-kebab-title.md. Работай строго в проекте D:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022. Обнови Handoff.

For Claude Code:
Выполни docs/ai-tasks/CC-NNN-kebab-title.md. Работай строго в проекте D:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022. Обнови Handoff.

For Codex:
Выполни docs/ai-tasks/CODEX-NNN-kebab-title.md. Работай строго в проекте D:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022. Обнови Handoff.
```

For multi-task work, maintain `docs/ai-tasks/PROJECT_STATE.md` with only the current architectural facts, completed task IDs, known blockers, and the next task ID. It is not a transcript and should stay compact.
