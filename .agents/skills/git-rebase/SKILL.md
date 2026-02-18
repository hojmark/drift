---
name: git-rebase
description: Perform quality-assured git rebase with pre/post validation
metadata:
  workflow: git
  audience: developers
---

## What I do

I perform git rebases while ensuring code quality is maintained throughout the process:

1. **Pre-rebase validation**: Build solution and run all tests using `dotnet nuke test` to establish baseline quality
2. **Capture baseline warnings**: Record any existing warnings to distinguish them from rebase-introduced issues
3. **Execute rebase**: Run the rebase command (typically `git pull --rebase github main`, but can target other branches)
4. **Resolve conflicts**: Help resolve merge conflicts that arise during rebase
5. **Post-rebase validation**: Re-run `dotnet nuke test` to verify quality is maintained
6. **Fix new issues**: Address any NEW warnings or test failures introduced by the rebase
7. **Preserve existing issues**: Leave pre-existing warnings unchanged (they're acceptable)

## When to use me

Use this skill when:
- Rebasing feature branches onto main or other branches
- You need confidence that the rebase doesn't break tests or introduce new problems
- Working in .NET projects that use NUKE build system
- You want to distinguish between old warnings (OK) and new issues (must fix)

## Command patterns

The most common rebase command is:
```bash
git pull --rebase github main
```

But it can be adapted for other branches:
```bash
git pull --rebase origin develop
git rebase main
git rebase -i HEAD~5
```

## Quality gates

**Pre-rebase checks:**
- Solution must build successfully
- All tests must pass
- Document any existing warnings (these are OK to keep)

**Post-rebase requirements:**
- Solution must still build successfully
- All tests must still pass
- No NEW warnings beyond the baseline
- Any new warnings or failures must be fixed

## Workflow

1. Run `dotnet nuke test` and capture output
2. Note any warnings (count and types)
3. Confirm working tree is clean with `git status`
4. Execute the rebase command
5. Resolve conflicts as they appear using Edit tool
6. Continue rebase with `GIT_EDITOR=true git rebase --continue` (non-interactive)
7. Run `dotnet nuke test` again
8. Compare warnings: old ones are OK, new ones need fixing
9. Fix any new issues introduced by the rebase
10. Verify final build succeeds with same or fewer warnings than baseline