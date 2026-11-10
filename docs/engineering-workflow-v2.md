# Học Engineering Workflow v2

Status: **Owner-approved repository policy** (2026-11-06), **Owner-approved
risk-proportionate refinement** (2026-11-10). The refinement takes effect as
repository policy after its PR is merged to `main`.
Applies to the standalone `letienhoang/Hoc` repository. This process does not
change HocTat/HSOne deployment governance.

## Goals and roles

Optimize for completing small, verifiable outcomes with fewer interruptions.
Favor working vertical slices and developer/consumer value over creating one
approval, PR or report per architectural layer. Treat Development verification
in proportion to actual data loss/security risk, not production-level ceremony.
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
authorization to implement code. A single **explicit bounded database-task GO**
may authorize the named Development migration, synthetic CRUD, temporary
purpose-specific test permissions, app restart and cleanup together, if each
operation, target, data-disposability assumption and stop condition is stated.
Do not request a new GO after every step already covered by that scope. For now,
Owner explicitly decides merges.

## Default delivery path

1. **Scope once.** State the end-to-end outcome, acceptance evidence, risks,
   target and exclusions. Combine Domain/Application/UI/persistence work into a
   single task when it is a coherent, reviewable vertical slice.
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
Do not repeat passed checkpoints without a source/configuration change, a
specific defect, or a concrete unproven acceptance requirement.

## Risk-based checkpoints

| Change | Review / execution policy |
| --- | --- |
| Docs, straightforward UI, deterministic tests | One task GO; scoped Git actions and PR permitted; lightweight TL check and CI |
| Routine feature or bugfix | One task GO; focused local tests; CI; TL review proportional to impact |
| Authentication, authorization, secrets, cross-tenant access, migration code, sensitive data | TL source review required before Owner merge decision; explicit negative tests |
| Read-only diagnostics on Owner-designated development DB | May be included in the task GO. Verify the effective database and TLS; no repeated project-ownership proof unless contradictory evidence appears |
| Reviewed additive migration on Owner-designated disposable Development DB | One explicit bounded GO can cover schema update, synthetic records, repeatability, restart and cleanup; fresh backup/PITR optional if existing state is reproducible; recheck target/TLS/history/schema immediately before writes |
| Development DB containing important/irreplaceable state | One bounded Owner GO for target and operations plus appropriate verified recovery approach; a `dev` name alone is not proof that its data is disposable |
| Destructive migration, data transformations, Identity/tenant changes | Risk-based deep review, affected-data inventory, explicit Owner approval and suitable recovery strategy |
| Production changes, destructive recovery/restores, deployment | Explicit operation-specific Owner GO and rollback/recovery plan |

Disposable test databases may be used only within an Owner-authorized
disposable target. Never silently substitute an existing local or cloud DB.
For an existing Development database with bootstrap Identity state, explicitly
confirm whether that state can be recreated before treating it as disposable.

## Database playbook

For a database-writing task, use the shortest diagnostic preflight that can
prevent an actual mistake. For a simple, reviewed additive migration on an
Owner-designated disposable Development target, verify the **effective**
environment/database/role, client-side TLS identity, migration history, relevant
schema absence and expected delta immediately before mutation. Reuse prior
reliable evidence instead of rerunning unrelated inventory. Use deeper data,
privilege and tenant checks when affected by the change or when drift suggests
risk. Distinguish observed metadata from Owner's data-disposability decision.

Backup, snapshot and PITR are **not automatic gates** for a reviewed additive
Development migration when existing state can be recreated. They remain
appropriate for irreplaceable state, high-impact data changes and production.
Do not create disposable branches/snapshots as rote ceremony.

Under the exact bounded Owner database-task GO, execute **only** approved
migration/seed/test operations, verify state, repeatability and cleanup as
specified, and close connections.
Stop on target drift or unexpected schema. Never infer production equivalence
from SQLite/TestServer tests. For Neon, `pg_stat_ssl` on a compute backend
does not directly measure a client's TLS session to the proxy; evaluate
client-side VerifyFull behavior and the correct connection layer.

For Học, `Hoc.DbMigrator` is **not schema-only**: even with
`Migration__HostOnly=true` it also seeds data. Use EF Core tooling for the
currently proposed schema-only DemoRecords migration, with
`Hoc.EntityFrameworkCore` as both project and startup project. Before any
DB write require the named Development target, current source/script and
migration history, and an explicit bounded Owner GO. Only use the
`Migration__HostOnly=true` setting when separately authorized to run
`Hoc.DbMigrator`; do not enable its seeding as a side-effect.

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

## Current H1 closeout and next consumer priority (2026-11-10)

The accepted H1-01 through H1-06 checkpoints stay closed. Consolidate remaining
H1 proof into one bounded **H1 Final Integration**: apply the reviewed
`20261106144522_DemoRecordsPersistence` migration to the exact approved
Development target, verify the PostgreSQL schema/history and idempotence,
exercise synthetic DemoRecords CRUD across an actual host restart, perform a
short Admin/Reader/Anonymous browser smoke check, clean up only task-created
data/grants, and record any audit/logging and license gaps succinctly. Do not
claim live PostgreSQL/browser proof from TestServer or CI evidence.

After H1, prioritize an independently composed **Education website consumer**
to test real module reuse, Admin content CRUD and public reading. Education/CMS
is a consumer capability, **not** a required Học platform core. Defer generalized
admin theme frameworks, packaging, portfolio release and a second neutral demo
unless an observed reuse gap makes them necessary. Product phase decisions are
tracked in the accepted H0 Notion record; this section is a near-term workflow
note, not an authorization to change product or production state.

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
