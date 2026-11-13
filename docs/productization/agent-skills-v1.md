# Học repository Agent Skills v1

This document is the onboarding and routing guide for the repository-scoped
Codex skills under `.agents/skills/`. It complements the executable H4
productization contracts; it does not create a new generator or runtime
integration.

## Choose the skill by the developer's starting point

| Starting point | Skill | Result |
| --- | --- | --- |
| Học tooling checkout; no consumer exists yet | `hoc-create-consumer` | A dry-run-first plan, then an independently derived consumer only after exact-destination authorization |
| An existing independent consumer needs the supported CRUD module | `hoc-add-admin-module` | H4-02 generation followed by H4-04 composition, with separate dry-run/apply boundaries |
| An existing consumer needs diagnosis | `hoc-validate-consumer` | Read-only structural validation and a categorized handoff; it does not repair findings |

Use `$hoc-create-consumer`, `$hoc-add-admin-module`, or
`$hoc-validate-consumer` for explicit invocation. When working inside this
checkout, the repository-scoped skills are also discoverable by task context.
Explicit invocation and automatic discovery select the same checked-in
`SKILL.md`; neither changes the authorization required for an operation.

The generated consumer is a separate source tree. It does not inherit the
`.agents/skills/` directory from the pinned Học source baseline. Keep the
tooling checkout available when using these workflows, or explicitly provide
an equivalent reviewed skill source.

## Shared operating contract

Every skill invocation must establish the exact tooling checkout, current
revision, requested consumer root (when applicable), application identity,
and ownership before acting. Preserve unrelated worktree changes. Read the
skill's authoritative references and inspect the actual script parameters;
the examples in a skill are not permission to broaden a script contract.

The default sequence is:

1. inspect and report the target and current state;
2. run the supported read-only dry-run or validator;
3. stop on a precondition failure or missing authorization;
4. perform only the explicitly authorized, named filesystem mutation; and
5. verify the result with focused, non-persistent checks.

`-Apply` is never implied by skill selection. Consumer creation requires
authorization for the exact new `OutputRoot`. Module work keeps generation and
consumer integration as separate authorized stages. Validation has no apply
stage and must not repair a finding.

No skill authorizes migration creation or application, `Hoc.DbMigrator`, seed
data, Identity or permission grants, persistent database access or writes,
remote repository creation, deployment, or production operations. Those are
separate tasks with separate target, recovery, and Owner authorization.

## Evidence and handoff

Report the command, exact revision/target identity, exit status, and the
observed result. Keep evidence categories separate: static structure,
compilation, unit tests, SQLite/TestServer, PostgreSQL, browser, and
deployed-runtime/production. A result in one category is not proof of a later
category. Mark unavailable categories `NOT VERIFIED` rather than inferring
them from a build or a dry-run.

The workflows hand off as follows:

- after `hoc-create-consumer` applies successfully, run the generated
  consumer's own structural/build checks before `hoc-add-admin-module`;
- use `hoc-add-admin-module` only for the fixed-shape module contract, not for
  arbitrary domain design or migration application; and
- use `hoc-validate-consumer` to diagnose an existing consumer, then obtain a
  separately scoped correction before editing any finding.

## Maintenance rules

The directory name, frontmatter `name`, and explicit invocation name must
remain identical. Changes to a skill must keep its authoritative references,
authorization boundaries, and evidence limits current. New workflows should
add a distinct skill only when they have a distinct starting point and
outcome; do not turn this routing guide into a generic catch-all.
