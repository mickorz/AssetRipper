---
name: writing-plans
description: Use when you have a spec or requirements for a multi-step task, before touching code
---

# Writing Plans

## Overview

Write comprehensive implementation plans assuming the engineer has zero context for our codebase and questionable taste. Document everything they need to know: which files to touch for each task, code, testing, docs they might need to check, how to test it. Give them the whole plan as bite-sized tasks. DRY. YAGNI. TDD. Frequent commits.

Assume they are a skilled developer, but know almost nothing about our toolset or problem domain. Assume they don't know good test design very well.

**Announce at start:** "I'm using the writing-plans skill to create the implementation plan."

**Context:** This should be run in a dedicated worktree (created by brainstorming skill).

**Save plans to:** `docs/plans/YYYY-MM-DD-<feature-name>.md`

## Plan Size Check (MUST DO FIRST)

Before writing any plan content, assess the scope. Count the **independent modules** involved (e.g., each Provider, each route group, each distinct subsystem). Then apply:

| Modules | Action |
|---------|--------|
| 1-3 | Single plan file, proceed normally |
| 4-6 | **WARN user, suggest split into separate plan files per module** |
| 7+ | **REQUIRE split, refuse to write a single monolithic plan** |

**Split naming convention:**
```
docs/plans/
  YYYY-MM-DD-<feature>-01-<module1>.md
  YYYY-MM-DD-<feature>-02-<module2>.md
  YYYY-MM-DD-<feature>-03-<module3>.md
  ...
```

**When suggesting a split:**
1. List the proposed split with file names and what each covers
2. Ask user to confirm or adjust the split
3. After confirmation, write each plan file independently (one Write call per file)
4. Each split file is a complete plan with its own header and tasks
5. Create an index file `YYYY-MM-DD-<feature>-00-index.md` listing all sub-plans with execution order

**Why split?** A single plan covering 4+ modules tends to exceed 2000 lines. This causes:
- Context window exhaustion from reading reference files
- Write tool timeout on large content
- Total loss on interruption (no checkpoint recovery)

**Index file example:**
```markdown
# [Feature Name] Plan Index

> **Execution order matters** - complete plans sequentially unless marked [parallel].

| Order | Plan File | Description | Estimated Tasks |
|-------|-----------|-------------|-----------------|
| 1 | 01-scaffold.md | Project setup, config, base classes | 5 |
| 2 | 02-claude.md | Claude Provider implementation | 4 |
| 3 | 03-codex.md | Codex Provider implementation | 4 |
| 4 [parallel] | 04-opencode.md | OpenCode Provider implementation | 4 |
| 5 [parallel] | 05-copilot.md | Copilot Provider implementation | 4 |
| 6 | 06-routes.md | Router + integration tests | 5 |
```

## Bite-Sized Task Granularity

**Each step is one action (2-5 minutes):**
- "Write the failing test" - step
- "Run it to make sure it fails" - step
- "Implement the minimal code to make the test pass" - step
- "Run the tests and make sure they pass" - step
- "Commit" - step

## Plan Document Header

**Every plan MUST start with this header:**

```markdown
# [Feature Name] Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** [One sentence describing what this builds]

**Architecture:** [2-3 sentences about approach]

**Tech Stack:** [Key technologies/libraries]

---
```

## Task Structure

````markdown
### Task N: [Component Name]

**Files:**
- Create: `exact/path/to/file.py`
- Modify: `exact/path/to/existing.py:123-145`
- Test: `tests/exact/path/to/test.py`

**Step 1: Write the failing test**

```python
def test_specific_behavior():
    result = function(input)
    assert result == expected
```

**Step 2: Run test to verify it fails**

Run: `pytest tests/path/test.py::test_name -v`
Expected: FAIL with "function not defined"

**Step 3: Write minimal implementation**

```python
def function(input):
    return expected
```

**Step 4: Run test to verify it passes**

Run: `pytest tests/path/test.py::test_name -v`
Expected: PASS

**Step 5: Commit**

```bash
git add tests/path/test.py src/path/file.py
git commit -m "feat: add specific feature"
```
````

## Writing Strategy (AVOID INTERRUPTIONS)

**Regardless of split or single plan, follow these rules:**

1. **Read references sparingly** - Only read files directly relevant to the current task. Avoid reading 10+ reference files before writing.
2. **Write incrementally** - For a single plan: write task-by-task sections. For split plans: write one file at a time, save before moving on.
3. **Prefer parallel Write calls** - When writing multiple independent split files, use parallel Write calls.
4. **Commit after each file** - After saving each split plan file, commit. This creates checkpoints.

## Remember
- Exact file paths always
- Complete code in plan (not "add validation")
- Exact commands with expected output
- Reference relevant skills with @ syntax
- DRY, YAGNI, TDD, frequent commits
- **If scope is large, split first, write second**

## Execution Handoff

**REQUIRED after saving:** Invoke savedoc skill to backup the plan to Obsidian and open in Chrome for preview.

After saving and syncing, offer execution choice:

**For split plans:**
"Plans split into N files (see index at `docs/plans/YYYY-MM-DD-<feature>-00-index.md`). Three execution options:

1. **Subagent-Driven (this session)** - I dispatch subagents per plan file, with review between each
2. **Parallel Agents (this session)** - I dispatch subagents for plans marked [parallel] simultaneously
3. **Sequential Session (separate)** - Open new session with executing-plans for batch execution

Which approach?"

**For single plans:**
"Plan complete and saved to `docs/plans/<filename>.md`. Two execution options:**

**1. Subagent-Driven (this session)** - I dispatch fresh subagent per task, review between tasks, fast iteration

**2. Parallel Session (separate)** - Open new session with executing-plans, batch execution with checkpoints

**Which approach?"**

**If Subagent-Driven chosen:**
- **REQUIRED SUB-SKILL:** Use superpowers:subagent-driven-development
- Stay in this session
- Fresh subagent per task + code review

**If Parallel Session chosen:**
- Guide them to open new session in worktree
- **REQUIRED SUB-SKILL:** New session uses superpowers:executing-plans
