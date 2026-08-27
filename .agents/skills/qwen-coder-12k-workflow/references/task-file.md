# Task File Format

Create task files in `docs/ai-tasks/` with a stable identifier, for example `20260827-01-player-spawn.md`.

```markdown
# Player Spawn Placement

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

For multi-task work, maintain `docs/ai-tasks/PROJECT_STATE.md` with only the current architectural facts, completed task IDs, known blockers, and the next task ID. It is not a transcript and should stay compact.
