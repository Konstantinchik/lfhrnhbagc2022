---
name: qwen-coder-12k-workflow
description: "Prepare and run compact Cline tasks for Qwen2.5-Coder-14B in a 12K context window. Use when long repository work needs durable Markdown handoffs rather than one growing chat."
---

# Qwen Coder 12K Workflow

Use this workflow for `qwen2.5-coder-14b-instruct` in Cline, where the agent instructions and MCP schema already consume a substantial part of the 12K context window.

## Core Approach

- Make Markdown task files the durable source of work state; chat is only the execution surface.
- For DarkTreeFPS2022, store task files in `docs/ai-tasks/`. Keep each file focused on one observable result and its verification.
- Route task files by owner prefix:
  - `CLINE-`: execute in the current VS Code Cline chat.
  - `CC-`: handoff for Claude Code in the terminal.
  - `CODEX-`: handoff for Codex.
- Split at real boundaries: investigation, one code change, one scene mutation, one test or regression fix. Do not split merely to make a checklist longer.
- Start a fresh Cline task for each task file. Give Cline the task file and only the source paths it names. Do not ask it to rediscover or summarize the whole repository.
- Do not attach screenshots to Cline. Convert visual observations into short text facts before preparing the task.

## Task Ownership

The filename prefix and the `Owner:` field inside the task file should match. Treat the filename prefix as the visible routing signal. If a mismatch exists or the task has `Status: needs-owner-review`, pause execution and ask the user to confirm ownership instead of guessing.

- Use `CLINE-NNN-kebab-title.md` when Cline should perform the work.
- Use `CC-NNN-kebab-title.md` when Cline should prepare a Claude Code handoff. Cline may edit the task file but should not perform the work unless the user explicitly reassigns it.
- Use `CODEX-NNN-kebab-title.md` when the work should be sent to Codex. Cline may edit the task file but should not perform the work unless the user explicitly reassigns it.
- Keep reassignment explicit: rename the file, update `Owner:`, and record the change in `Handoff`.
- `scripts/new-qwen-task.ps1` uses `-Owner AUTO` by default. If a supplied `-TaskId` already starts with `CLINE-`, `CC-`, or `CODEX-`, the script normalizes `Owner:` to that prefix. Conflicts are created as `Status: needs-owner-review` rather than failing the task creation.

## Task Discipline

1. Inspect enough code and project state to write facts, not guesses.
2. Create a task file with the format in [task-file.md](references/task-file.md). Use `scripts/new-qwen-task.ps1` when a new file is needed.
3. Keep the initial prompt short: identify the task file, name the requested owner, and state the immediate action. Let the file carry the details.
4. Require a focused verification result. If a task cannot be verified in the current environment, record the exact remaining editor or runtime check.
5. Finish by updating the task's Handoff section with changed paths, verified facts, and the next task. For work spanning multiple tasks, maintain the small `PROJECT_STATE.md` described in the reference.

## Context Boundaries

- Preserve headroom for tool calls and model output. A task that names many unrelated systems, asks for broad research, and requests implementation should be split before it reaches Cline.
- Include exact paths, identifiers, and constraints needed for the next action; omit old reasoning, full logs, source dumps, and MCP tool schemas.
- When a response fails or reaches a limit, do not retry with a larger prompt. Record the useful state in the task file, reduce the next unit of work, and continue in a fresh task.
