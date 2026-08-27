# Agent Workflow TDD Tests

These tests guard the repository-level AI workflow before committing confirmed changes.

Run from the project root:

```powershell
.\tests\agent-workflow\run-tests.ps1
```

The suite currently checks:

- `.clinerules` contains the project root and task-owner routing rules.
- `.clinerules` requires Russian dialogue and explicit project-root commands.
- `.clinerules` forbids fabricated command output and placeholder file names.
- Filesystem listings format directory names with a trailing `\`, for example `Assets\`.
- `.agents/skills` document the `CLINE`, `CC`, and `CODEX` task prefixes.
- `new-qwen-task.ps1` creates recoverable task handoffs:
  - default `AUTO` routes to `CLINE`;
  - explicit owners route to their prefix;
  - owner/name conflicts do not fail creation;
  - conflicts are marked `Status: needs-owner-review`.
- Existing `docs/ai-tasks/*.md` files do not contain unresolved `Status: needs-owner-review` entries.

Use this before committing agent workflow changes. If a test fails, either fix the workflow or intentionally resolve the task ownership state before committing.
