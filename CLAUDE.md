# CLAUDE.md

Guidance for Claude Code sessions working in this repo — including KITT, Breeze's governed coding agent (see https://github.com/breeze-com/kitt).

## Repo basics

- Breeze Unity SDK (C#): `sdks/Unity/Breeze/` is the package (with `Plugins/`, docfx docs project), `examples/UnityBreezeDemo/` is a full demo Unity project.
- Tests exist at `sdks/Unity/Breeze/Tests/Runtime/` (Unity Test Framework / NUnit-style, asmdef-scoped). **Caveat: running them properly requires a Unity editor/runtime, which the plain GitHub Actions runner does not have.** Before claiming test evidence, verify what is actually executable in your environment and say so honestly — pure-C# logic may be testable with a standalone compile, Unity-API-dependent code is not.
- `.meta` files are Unity asset metadata: every new file needs Unity to generate its `.meta` (or the convention followed exactly); never delete or hand-edit `.meta` files casually.
- Match existing C# style: namespaces, PascalCase public members, XML doc comments where present.

## KITT agent notes

- Your per-repo memory is fetched into `.kitt/memory.md` before every run. Read it first; it compounds. Write back what you learn by editing `.kitt/memory.md` in place (and, when you open a PR, writing its ledger table row to `.kitt/ledger-row.md`); the workflow syncs both to the hub after your run.
- Branches `kitt/<slug>`, PR titles `[KITT] <summary>`, label `kitt`.
- Every PR body: **What & why** · **Test evidence** (exact commands + output, with honest limits — see the Unity caveat above) · **Self-review** · **Rollback**.
- Never commit `.kitt/` (gitignored).
- You cannot merge PRs, modify `.github/workflows/`, or read env/secret files — enforced at the platform layer; design within.
