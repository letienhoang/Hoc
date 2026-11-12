# Consumer compatibility policy v1

## Scope and provenance

Học consumers are independent source derivatives. Record the upstream repository and exact Học SHA used to create each consumer. H4-01's reference baseline is Học `6a8605a63d18d35767f520d57d33193f6d15aa29`; this is a source checkpoint, not a claim that every later Học commit is compatible.

Học's source revision is distinct from dependency versions. At this baseline:

| Surface | Baseline | Compatibility claim |
| --- | --- | --- |
| .NET SDK | `10.0.403` from `global.json` | Exact SDK is required for the documented build. |
| Target framework | `net10.0` | Consumer must review its own projects. |
| ABP packages | `10.7.0` | Keep aligned unless a controlled review approves drift. |
| LeptonX Lite | `5.7.0` | External NuGet dependency; no vendoring or redistribution claim. |

## Ownership and supported boundaries

The consumer owns its solution/projects, namespaces, modules, configuration, OpenIddict client/audience identifiers, DbContext/schema, migrations, tests, branding, and deployment. Học's DemoRecords feature is reference material, not consumer infrastructure. A consumer may reuse Học source and conventions but does not receive binary compatibility, a shared `Hoc.*` package contract, automatic upstream fixes, automatic migrations, or seamless upgrades.

Supported claims are narrow: a clean source copy can be structurally validated against a recorded baseline, and a consumer can review selected upstream changes before manually applying them. Unverified claims include runtime authentication, PostgreSQL behavior, migration application, browser behavior, deployed runtime, production readiness, and compatibility with arbitrary consumer modifications.

## Compatibility risks and review classification

| Upstream change | Risk | Required consumer review/validation |
| --- | --- | --- |
| Documentation or isolated build script | Low | Diff review; rerun relevant static/self-tests. |
| Internal implementation with unchanged contracts | Medium | Diff/dependency review; restore, Release build, focused tests. |
| Application contracts, DTOs, permissions, localization, modules | High | Contract impact review, consumer integration tests, authorization checks. |
| Configuration, secrets, TLS, OpenIddict, authentication/authorization | High | Security review; negative tests; real-runtime evidence only when separately authorized. |
| EF mappings, provider, migrations, schema | High | Migration review (`Up`, `Down`, snapshot), target/TLS/recovery plan, separate write GO before APPLY. |
| .NET/ABP/LeptonX dependency upgrade | High | Lock-file/package review, clean restore/build/tests, UI/runtime assessment as applicable. |

Any conflict with a consumer-owned module, configuration, contract, or migration is a stop condition, not an invitation to force a merge.

## Controlled upstream-change procedure

1. Pin and record the current consumer SHA and the candidate upstream Học SHA.
2. Read the upstream diff and classify each changed surface using the table.
3. Identify consumer-owned edits that overlap; preserve consumer identity, credentials boundaries, database ownership, and licensing notices.
4. Apply selected changes in a consumer branch, resolve conflicts intentionally, and update package/SDK versions only after review.
5. Run the static validator, restore, Release build, and relevant focused tests. Separate SQLite/TestServer evidence from PostgreSQL, browser, deployed, and production evidence.
6. For security or persistence changes, obtain the required technical/security review and operation-specific authorization before any persistent action.
7. Retain the pre-upgrade consumer revision as the code rollback point. Do not call an EF `Down()` migration a data-recovery plan.

## Configuration and security compatibility

Consumers must re-evaluate Development secret-overlay behavior, environment selection, PostgreSQL `VerifyFull` TLS, OpenIddict certificates, audience, client IDs, root/redirect URIs, authentication behavior, permissions, tenant boundaries, and configuration precedence. Secrets, certificates, and production endpoints are never copied into tracked source. A static check can flag known artifacts; it cannot validate effective runtime identity or credentials.

## Licensing and redistribution

Retain upstream license/provenance notices. ABP-derived template/source material has its applicable LGPL-3.0-only provenance; apply Apache-2.0 only where the copyright holder authorizes it. LeptonX Lite remains an external dependency. Do not extract its package assets, vendor it, or claim redistribution rights.

## Upgrade and rollback limits

Code can be rolled back to a recorded consumer commit. Database rollback, recovery, package downgrades, external clients, and deployed configuration are separate operational concerns and require their own evidence and approval. There is no automatic merge, migration compatibility guarantee, binary compatibility guarantee, or version matrix beyond validation actually run on a specific consumer revision.
