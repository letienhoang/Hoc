# Học Engineering Workflow v2

Status: **Owner-approved repository policy, merged to main** (2026-11-06).
Applies to the standalone `letienhoang/Hoc` repository. This process does not
change HocTat/HSOne deployment governance.

## Goals and roles

Optimize for completing small, verifiable outcomes with fewer interruptions.
**Owner** defines goals, accepts risk, authorizes irreversible/high-impact
operations, and controls merges during the initial rollout.
**Codex** implements a bounded task, runs focused local validation, and
publishes a reviewable branch/PR.
**Technical Lead (TL)** reviews high-risk changes and signs off on scope,
correctness, evidence and residual risk. A TL review from the PR author's
GitHub account is advisory, not a GitHub-enforced independent approval.

## One authorization per bounded task

A task GO defines: objective, allowed paths/operations, target environment,
test expectations, and explicit exclusions. While within that scope Codex
may create a feature branch, edit files, run non-mutating build/tests,
stage only scoped paths, commit, push, and open/update a Draft PR **without
requesting separate approval for each Git action**. Preserve unrelated work.
Stop and request a revised scope only when work would exceed the authorization.

Do not infer authorization to merge, deploy, execute database migrations,
seed, modify persistent data, rotate credentials, or change production from
authorization to implement code. For now, Owner explicitly decides merges.

## Default delivery path

1. **Scope once.** State outcome, acceptance evidence, risks and exclusions.
2. **Implement.** Use the current `main`, a bounded feature branch, focused
   tests, and synthetic/local data. Never stage secrets or generated outputs.
3. **Publish.** Codex inspects the diff, commits/pushes allowed paths, opens a
   Draft PR, and gives the link and local test evidence.
4. **Automatic CI.** PR runs restore, Release build and non-database .NET tests
   on a fresh runner. A failed or absent required check is not a PASS.
5. **TL review by risk.** Fix blockers in the same PR. Re-run only focused
   local tests for a small fix; CI supplies full validation on the new SHA.
6. **Merge and verify.** Owner approves merge during initial rollout. Check
   final PR head, CI and scope before merge. Perform targeted post-merge
   acceptance; do not repeatedly validate unchanged infrastructure.

Small tasks need not generate a separate planning PR, test-report file or
formal TL comment when CI and diff already provide adequate evidence.

## Risk-based checkpoints

| Change | Review / execution policy |
| --- | --- |
| Docs, straightforward UI, deterministic tests | One task GO; scoped Git actions and PR permitted; lightweight TL check and CI |
| Routine feature or bugfix | One task GO; focused local tests; CI; TL review proportional to impact |
| Authentication, authorization, secrets, cross-tenant access, migration code, sensitive data | TL source review required before Owner merge decision; explicit negative tests |
| Read-only diagnostics on Owner-designated development DB | May be included in the task GO. Verify the effective database and TLS; no repeated project-ownership proof unless contradictory evidence appears |
| Write to persistent development DB | Separate bounded Owner GO for target, change, recovery approach, execution and verification together |
| Production changes, destructive migrations, restores, deployment | Explicit operation-specific Owner GO and rollback/recovery plan |

Disposable test databases may be used only within a previously authorized
disposable target. Never silently substitute an existing local or cloud DB.

## Database playbook

For a database-writing task, perform one consolidated read-only preflight:
effective environment, endpoint/database/user identity, TLS policy, privileges,
schema/history, tenant connections and expected delta. Preflight results and
the Owner's target designation are different types of evidence. Use a
disposable PostgreSQL instance or isolated Neon branch when schema-risk
justifies it; do not create branches/snapshots as rote ceremony.

After a separate bounded write GO, verify target again immediately before
the operation, execute **only** approved migration/seed steps, verify state
and repeatability where the approved plan calls for it, then close connections.
Stop on target drift or unexpected schema. Never infer production equivalence
from SQLite/TestServer tests. For Neon, `pg_stat_ssl` on a compute backend
does not directly measure a client's TLS session to the proxy; evaluate
client-side VerifyFull behavior and the correct connection layer.

For current Học initialization: `DOTNET_ENVIRONMENT=Development`,
`Migration__HostOnly=true`, verified `hoc_dev` target, and explicit Owner
GO are required before executing `Hoc.DbMigrator`.

## CI policy

`.github/workflows/ci.yml` is the baseline: no database service, no Neon
credentials, no migrations, no deployment. It builds the solution and runs
Application, Domain, EF Core SQLite and Web TestServer projects. The initial
workflow must prove green on a PR before any check is marked required.

After confirming reliable results, configure main branch protection/ruleset
to require pull requests, the exact stable CI job status and block force-push.
Do not enable an impossible independent-review requirement for a one-account
repository. GitHub branch protections are configured separately by the Owner
or an authorized administrator.

## Evidence, acceptance and feedback

Local tests are **Codex-reported** unless reproduced independently; GitHub
CI evidence is tied to a specific SHA. Unit, SQLite/TestServer, PostgreSQL,
browser and deployed-runtime proof are not interchangeable. Browser
acceptance remains separate for H1-02; do not label it complete without
actual browser evidence. For browser acceptance, prefer Codex in Chrome
(`@Chrome`) and retain DOM, network and console evidence. If Chrome
connectivity is blocked, report the limitation; do not automatically switch to
custom CDP, HeadlessChrome or desktop automation.

Observe PR cycle time, number of review rounds, Owner interruptions,
CI reliability and post-merge defects. Revise the workflow after real usage;
do not optimize for a fabricated productivity percentage. This document
records policy; it does not itself grant production or persistent-DB write GO.
