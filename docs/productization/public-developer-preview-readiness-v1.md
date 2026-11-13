# Public Developer Preview readiness and onboarding v1

## Preview boundary

This source-first Public Developer Preview is for developers who can work with
an ABP 10 MVC/Razor source checkout and want to either evaluate Học's source
layout or derive an independent Admin consumer from the supported H4 baseline.
It supports source inspection, the H4-03 consumer bootstrap, the H4-02/H4-04
fixed-shape Admin module workflow, and read-only structural diagnosis.

It does **not** offer a Học runtime package, hosted service, automatic
upgrades, binary compatibility, arbitrary module design, database
initialization, migration application, production configuration, deployment,
or production support. A derived consumer owns its source tree, identity,
configuration, schema, migrations, tests, branding, and deployment. See the
[compatibility policy](compatibility-v1.md) for the complete ownership and
upgrade boundary.

## First checkout: use two source trees

Keep these distinct:

| Checkout | Purpose | Required revision |
| --- | --- | --- |
| Học tooling checkout | Holds the H4 scripts, documentation, and repository-scoped Agent Skills. Run commands from here. | Record the exact tooling SHA used for each exercise. |
| Học source checkout | Input to the consumer bootstrap generator. | `6a8605a63d18d35767f520d57d33193f6d15aa29` |
| Derived consumer | A new, independent destination outside either source checkout. | Created only after an approved exact destination. |

The source SHA is intentionally different from the tooling SHA. H4-03 supports
only the pinned source SHA above; a newer `main` is not implicitly a supported
consumer source. The required SDK is `10.0.403` from `global.json`; the
documented baseline uses `net10.0`, ABP `10.7.0`, and LeptonX Lite `5.7.0`.
Use PowerShell 7 (`pwsh`); Node.js `20.11` or later is needed only when Web
client libraries must be restored. PostgreSQL tooling is for a separately
authorized database operation, never this onboarding path.

Before acting, read [AGENTS.md](../../AGENTS.md), the
[engineering workflow](../engineering-workflow-v2.md), the
[automated bootstrap contract](automated-consumer-bootstrap-v1.md), and the
[compatibility policy](compatibility-v1.md). Do not copy ignored secrets,
certificates, local artifacts, restored client libraries, or Git metadata into
a consumer.

## Choose the existing Agent Skill

Use the skills from the Học **tooling** checkout. They are repository-scoped:
a standalone consumer derived from the pinned source does **not** automatically
inherit `.agents/skills/`.

| Starting point | Invoke | What it does |
| --- | --- | --- |
| No consumer exists | `$hoc-create-consumer` | Guides H4-03 bootstrap and its structural verification. |
| An independent consumer needs the supported fixed-shape CRUD module | `$hoc-add-admin-module` | Guides H4-02 generation and H4-04 composition as distinct stages. |
| An existing consumer needs diagnosis | `$hoc-validate-consumer` | Runs read-only structural validation and categorizes the result. |

Read the [Agent Skills routing guide](agent-skills-v1.md) and the selected
skill's `SKILL.md` before commands. Selecting a skill never authorizes
`-Apply`, a migration, `Hoc.DbMigrator`, database access, grants, deployment,
or production work.

## Safe developer sequence

1. Record the tooling checkout path/SHA, source checkout path/SHA, intended
   consumer destination, application identity, and HTTPS root URL. Confirm the
   output directory is new, outside the source tree and existing worktrees.
2. Run the appropriate default dry-run. It is read-only and must complete
   successfully before any mutation. For bootstrap, follow the exact command
   in [automated bootstrap](automated-consumer-bootstrap-v1.md). For module
   work, run generation dry-run and then integration dry-run as separate
   stages.
3. Obtain explicit authorization for the exact filesystem mutation. Only then
   use `-Apply`; bootstrap creation, module generation, and host integration
   are separately bounded writes. Preserve failures and do not force, delete,
   overwrite, or hand-edit generated output to bypass safeguards.
4. Run the read-only structural validator from the tooling checkout:

   ```powershell
   pwsh ./scripts/productization/verify-consumer.ps1 `
     -ConsumerRoot <consumer-root> `
     -ApplicationId <application-id>
   ```

5. In the consumer checkout, use its pinned SDK for serialized restore and
   Release build:

   ```powershell
   dotnet restore <ApplicationId>.slnx -m:1 /nr:false
   dotnet build <ApplicationId>.slnx -c Release --no-restore -m:1 /nr:false
   ```

   For a build-only exercise, two inherited projects may need local empty
   `{}` `appsettings.secrets.json` placeholders, as documented by the
   bootstrap contract. They are not credentials and must not be used as
   Development configuration. Restore ignored Web client libraries only when
   a selected Web/TestServer check requires them.
6. Run only focused, non-persistent consumer tests selected for the exercise.
   Record commands, exit codes, exact revisions, and evidence category. A
   structural pass, restore, build, or TestServer result does not prove
   PostgreSQL, browser, deployed-runtime, or production behavior.

No step above creates a migration, runs DbMigrator, seeds data, contacts a
persistent target, or deploys. Those operations require distinct scope,
target/TLS/recovery evidence, and Owner authorization.

## Known usability gaps (not H4 contract changes)

- The preview requires two deliberately separate checkouts and manual
  recording of both SHAs; there is no installer or automatic version manager.
- The three skills are available only in the tooling checkout; a consumer must
  retain that checkout or use an explicitly reviewed equivalent skill source.
- `-Apply` is intentionally a manual authorization boundary, not a one-command
  experience.
- Client libraries are ignored and may require the documented Node/ABP restore
  path before Web/TestServer rendering.
- The validator is structural only; developers must choose and retain separate
  runtime evidence where their consumer requires it.

These are onboarding constraints, not defects to solve by broadening the H4
generator, module, compatibility, or runtime contracts.

## Release-readiness matrix

This matrix describes the preview documentation state at its recorded source
revision. It does not make a legal or production-release decision.
No factual `FAIL` was identified in this documentation preflight; an
unperformed exercise is `NOT VERIFIED`, while an unmet required approval or
clearance is `BLOCKED`.

| Area | Classification | Factual evidence / remaining condition |
| --- | --- | --- |
| Source-first audience and boundaries | PASS | README and this guide constrain the preview to source evaluation and independent consumer derivation. |
| Supported tool and dependency versions | PASS | `global.json` pins SDK `10.0.403`; compatibility policy records `net10.0`, ABP `10.7.0`, and LeptonX Lite `5.7.0`. |
| Pinned consumer source | PASS | H4-03 documents `6a8605a63d18d35767f520d57d33193f6d15aa29`; tooling and source revisions are explicitly separate. |
| Skill entry point and consumer non-inheritance | PASS | README, Agent Skills v1, and this guide route all three skills and state the repository-only scope. |
| Dry-run, Apply, validator, restore, build sequence | PASS | Existing H4 contracts define the executable behavior; this guide links and orders them without changing their scope. |
| CI coverage of H4 tool self-tests and clean Release build/tests | PASS | `.github/workflows/ci.yml` runs the four H4 self-test scripts, serialized restore/Release build, and focused test projects. |
| H5-02 clean-checkout reproducibility | NOT VERIFIED | The subsequent exercise has not been run from a fresh tooling/source pair and new disposable consumer destination. |
| Browser, PostgreSQL, deployed runtime, and production | NOT VERIFIED | No such evidence is implied by static validation, build, TestServer, or CI. |
| Public-source licensing/provenance clearance | BLOCKED | Owner/copyright attestation, ABP/Volosoft distribution treatment, and authoritative LeptonX Lite MVC/Razor terms remain required by `NOTICE` and `THIRD-PARTY-NOTICES.md`. |
| Public release or distribution authorization | BLOCKED | Requires Owner/legal decision after the licensing gates; this preview work creates neither a release nor distribution artifact. |

## H5-02 clean-checkout reproducibility evidence

H5-02 should use new, disposable locations and retain a sanitized evidence
record containing:

1. Tooling checkout origin, exact tooling SHA, clean status, and the selected
   skill/contract versions.
2. Separate source checkout origin, proof that pinned SHA
   `6a8605a63d18d35767f520d57d33193f6d15aa29` is available and checked out,
   clean status, and SDK version.
3. Exact new output path, application ID, and HTTPS root URL; proof it is
   outside the source checkout and was nonexistent before Apply.
4. Complete bootstrap dry-run command/output and exit code, followed by the
   separately authorized Apply command/output and exit code.
5. Validator command, all `PASS:`/`FAIL:` lines, final `RESULT:`, and exit
   code. Preserve only paths and diagnostics; never capture secret contents.
6. Consumer revision/status after creation, pinned SDK restore and Release
   build commands with exit codes, plus each focused test command and result.
7. Any required client-library restore command and outcome, clearly separated
   from source/bootstrap evidence.
8. A final classification that structural, compilation, TestServer/SQLite,
   PostgreSQL, browser, deployed-runtime, and production evidence are distinct.
   Mark categories not exercised `NOT VERIFIED`.
9. Confirmation that no shared/persistent database was contacted, no
   migration/DbMigrator/seeding/grants occurred, and no deployment or release
   action occurred.

H5-02 must stop and report an unsupported source revision, unsafe destination,
authorization gap, structural failure, or build/test failure. It must not
repair generated source, relax validation, or continue into H5-03 without a
separate Owner decision.
