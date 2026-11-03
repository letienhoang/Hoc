# H1-02 Security Verification

## Scope and evidence

- **Main baseline:** `24db5df1252b06812d10adac269a6f30b8e2529a`.
- **Verified security-test source revision:** `c9547ef7b2ab6db5da176d7dbca38d3cbe41721a`.
- **Environment:** local Windows TestServer, ABP 10.7.0, .NET SDK 10.0.403, isolated in-memory SQLite supplied by `HocEntityFrameworkCoreTestModule`.
- **Out of scope:** DbMigrator, PostgreSQL connection or writes, production access, real credentials, browser automation, deployment, and merge.
- **Test authorization integrity:** `RolePermissionWebTestBase` replaces the permissive test registrations with ABP's `AbpAuthorizationService`, `MethodInvocationAuthorizationService`, `PermissionChecker`, and `HttpContextCurrentPrincipalAccessor`. Assertions also verify these implementations at runtime.

## Verification matrix

| Control | Evidence | Result |
| --- | --- | --- |
| Anonymous protected-route access | `Anonymous_request_to_protected_route_should_be_challenged` checks the login challenge and return URL. | PASS |
| Login and antiforgery | Invalid credentials remain unauthenticated; a login POST without a token cannot authenticate; a valid token/login reaches the protected probe. | PASS |
| Logout/session access | The authenticated client reaches the probe, then logout returns it to a challenge. | PASS |
| Registration UI and direct API | The Razor page exposes no local-registration inputs; both Razor and `/api/account/register` denial paths assert no user is persisted. | PASS |
| Registration with a valid antiforgery token | The Razor registration POST includes a token and is denied by server policy before a user exists. | PASS |
| Registration override prevention | The setting is `false`, accepts only its default provider, and Global/Tenant override attempts throw. | PASS |
| Admin-managed creation | The existing service-level test verifies administrator-managed identity creation in the isolated fixture. | PASS |
| Reader baseline | Reader is non-default, has no claims or role grants, and its seed is idempotent. | PASS |
| Anonymous and Reader identity-list access | The real `/api/identity/users` endpoint challenges anonymous requests, denies Reader, and returns a valid list to Admin. | PASS |
| Reader user creation | Reader POST to `/api/identity/users` carries an antiforgery token, is authorization-denied, and the proposed user is absent from SQLite. | PASS |
| Reader role escalation | Reader PUT to `/api/identity/users/{id}/roles` carries an antiforgery token, is authorization-denied, and the target user has no roles. | PASS |
| Reader permission-management access | Reader GET to `/api/permission-management/permissions` is authorization-denied; the existing role-grant test separately confirms Reader has no permission grants by default. | PASS |

## Existing and added verification

Existing H1-02A/B/C coverage is retained: protected-route challenge, invalid/valid login, antiforgery login rejection, logout, public-registration Razor/API denial, immutable registration settings, role seed/idempotency/no-grants, and identity-list authorization.

This change adds only three negative TestServer/SQLite tests: Reader user creation, Reader role assignment, and Reader permission-management access. The two mutation requests carry a freshly issued antiforgery token, assert an authorization-denial response rather than 404/500/validation failure, and check that the targeted persisted state did not change.

## Local validation

| Command | Result |
| --- | --- |
| `./.tools/dotnet/dotnet.exe test test/Hoc.Web.Tests/Hoc.Web.Tests.csproj -c Release -m:1 /nr:false --no-build --no-restore --logger "console;verbosity=minimal"` | PASS — 18 passed, 0 failed, 0 skipped; isolated TestServer/SQLite. |
| `./.tools/dotnet/dotnet.exe build Hoc.slnx -c Release -m:1 /nr:false --no-restore` | PASS — 0 warnings, 0 errors. |
| `git diff --check` | PASS before each commit. |

The Web-project build emitted one pre-existing external test-host warning, `CS7022` from `Microsoft.NET.Test.Sdk.Program.cs`; the full solution Release build reported 0 warnings. No tests were skipped or failed. Local validation is not independent CI verification.

## Security findings and remaining risks

- **Medium — fail-open compatibility risk:** `HocAccountSettingDefinitionProvider` returns without enforcing the disabled default if the underlying ABP `IsSelfRegistrationEnabled` definition is absent. Current module dependencies supply it and the current path is covered, but an ABP/module-composition regression could silently restore upstream behavior. No production remediation is included because this H1-02D1 task is verification-only; require an Owner-approved, separately reviewed fail-closed startup policy before changing it.
- **NOT VERIFIED — browser evidence:** TestServer HTTP assertions do not prove browser cookie handling, rendered error UX, CSRF behavior in a real browser, or session invalidation across concurrent tabs.
- **NOT VERIFIED — PostgreSQL/runtime evidence:** SQLite/TestServer does not establish PostgreSQL behavior, deployed configuration, production database state, or production authorization.
- **NOT VERIFIED — H1-01 runtime limitation:** H1-01’s isolated local runtime proof does not establish a deployed, browser-verified, PostgreSQL-backed service. H1-02 inherits that limitation.

## D2 execution checklist (Owner approval required)

1. Approve a disposable, named PostgreSQL target and its ownership; do not use production or an unrelated service.
2. Authorize a dedicated non-administrative Reader test account and an administrator-operated test account; keep credentials in an approved secret mechanism.
3. Start the approved Web host only, with the approved target and configuration, without running DbMigrator unless separately authorized.
4. In a real browser, record anonymous, Reader, and Admin outcomes for the tested identity and permission routes; include logout/session behavior and registration denial with a valid antiforgery submission.
5. Query only the approved disposable target to confirm denied mutations created no users, role memberships, or permission grants; preserve redacted evidence and clean up only under explicit authorization.

## Technical Lead recommendation

**Conditional recommendation: do not declare H1-02 accepted yet.** Local TestServer/SQLite authorization evidence is suitable to advance the Draft PR and request review after validation. D2 browser and approved PostgreSQL runtime evidence, plus an Owner decision on the missing-setting fail-open risk, remain gates for H1-02 acceptance.
