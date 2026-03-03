---
name: git-commit
description: Auto-generate and apply Conventional Commits when the agent makes commits
metadata:
  workflow: git
  audience: developers
---

## What I do

Generate Conventional Commit messages (feat, fix, docs, style, refactor, perf, test, chore, ci), infer a short scope when possible, assemble header/body/footer, stage the agent's changed files and run `git commit` automatically.

## When to use me

Use for routine commits made by the agent (code changes, fixes, docs, chores). Avoid for sensitive/security or policy-reviewed changes.

## Commit format & examples

Header: `type(scope?): short imperative subject` (<=50 chars)

Examples:
- `feat(auth): allow token refresh`
- `fix(cache): apply TTL to keys`

Add body and footers (`BREAKING CHANGE:` or `Fixes: #123`) when needed.

## Heuristics

- Type selection by diff contents (API/feature → feat, tests only → test, lint/format → style, etc.)
- Scope from top-level dir (e.g., `pkg/auth` -> `auth`); omit if multiple areas changed
- Subject: imperative, present tense, truncated to 50 chars
- Include body when diff is non-trivial

## Workflow (brief)

1. Detect agent-modified files via `git status --porcelain`.
2. Infer `type`, `scope`, `subject`, optional `body`.
3. Validate header against Conventional Commit pattern.
4. Stage intended files (`git add <files>`).
5. Commit: `git commit -m "HEADER" -m "BODY" -m "FOOTER"`.
6. Log commit SHA and the header. Do not push by default.

## Safety checks

- Scan diffs for likely secrets (`.env`, credential filenames, common secret patterns) and abort if found.
- Respect `.gitignore` and pre-commit hooks; do not force-skip hooks.
- If linters exist (configurable), run them before committing; on failure, surface output and abort.

## References

- Conventional Commits: https://www.conventionalcommits.org/
